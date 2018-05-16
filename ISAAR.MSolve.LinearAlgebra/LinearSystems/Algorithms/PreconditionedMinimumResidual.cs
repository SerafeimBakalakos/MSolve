﻿using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Exceptions;
using ISAAR.MSolve.LinearAlgebra.LinearSystems.Preconditioning;
using ISAAR.MSolve.LinearAlgebra.LinearSystems.Statistics;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Vectors;

//TODO: perhaps some vectors and operations are not needed or can be simplified (e.g. norm2(r) instead of sqrt(r*r)) 
//      in the version without preconditioning
namespace ISAAR.MSolve.LinearAlgebra.LinearSystems.Algorithms
{
    /// <summary>
    /// MINRES algorithm for solving an n-by-n system of linear equations: A*x = b, where A is symmetric and b is a given vector 
    /// of length n. A may be indefinite. It can also be singular, in which case the least squares problem is solved instead.
    /// The preconditioner M must be symmetric positive definite. The MINRES method is presented by Paige, Saunders in 
    /// https://www.researchgate.net/publication/243578401_Solution_of_Sparse_Indefinite_Systems_of_Linear_Equations
    /// </summary>
    public class PreconditionedMinimumResidual
    {
        private const double eps = double.Epsilon; // TODO: replace it in the code, when all else has been successfully ported

        private readonly bool checkMatrices;
        private readonly int maxIterations;
        private readonly bool printIterations;
        private readonly double residualTolerance;

        public PreconditionedMinimumResidual(int maxIterations, double residualTolerance, bool checkMatrices, 
            bool printIterations)
        {
            this.checkMatrices = checkMatrices;
            this.maxIterations = maxIterations;
            this.printIterations = printIterations;
            this.residualTolerance = residualTolerance;
        }

        public (Vector, MinresStatistics) Solve(IMatrixView A, Vector b, IPreconditioner M, double shift = 0.0)
        {
            //TODO: use invalid values as initial
            //TODO: remove Matlab/Fortran notation from comments

            /// Initialize.

            int n = b.Length;
            int istop = 0; // TODO: not sure if needed
            int itn = 0;
            double Anorm = 0.0;
            double Acond = 0.0;
            double rnorm = 0.0;
            double ynorm = 0.0;
            var x = Vector.CreateZero(n);

            /// -------------------------------------------------
            /// Set up y and v for the first Lanczos vector v1.
            /// y = beta1 P' v1,  where  P = C**(-1).
            /// v is really P' v1.
            /// -------------------------------------------------

            Vector r1 = b.Copy();
            Vector y = M.SolveLinearSystem(b);
            double beta1 = b * y;

            /// Test for an indefinite preconditioner.
            /// If b = 0 exactly, stop with x = 0.            

            if (beta1 < 0.0) throw new IndefiniteMatrixException("The preconditioner M is not positive definite.");
            else if (beta1 == 0.0)
            {
                var stats = new MinresStatistics { TerminationCause = 0 };
                return (x, stats);
            }
            beta1 = Math.Sqrt(beta1); //Normalize y to get v1 later.
            if (checkMatrices)
            {
                CheckSymmetricPreconditioner(M, y, r1);

                // WARNING: originally this had y instead of b. However, some precision is lost when multiplying inv(M)*b.
                // Coupled with the absurdely small tolerance used by IsMatrixSymmetric(), the matrix is flagged as non-symmetric
                // even though it is. By using b instead of y, the check is identical to the one done in the unpreconditioned
                // MINRES version.
                MinimumResidual.CheckSymmetricMatrix(A, b, r1);
            }

            /// ------------------------------------------------- 
            /// Initialize other quantities.
            /// -------------------------------------------------

            double oldb = 0.0, beta = beta1, dbar = 0.0, epsln = 0.0;
            double qrnorm = beta1, phibar = beta1, rhs1 = beta1, rhs2 = 0.0;
            double tnorm2 = 0.0, gmax = 0.0, gmin = double.MaxValue;
            double cs = -1.0, sn = 0.0;
            var w = Vector.CreateZero(n);
            var w2 = Vector.CreateZero(n);
            Vector r2 = r1.Copy();


            /// ------------------------------------------------- 
            /// Main iteration loop.
            /// -------------------------------------------------
            while (itn < maxIterations) // k = itn = 1 first time through
            {
                ++itn;

                /// -----------------------------------------------------------------
                /// Obtain quantities for the next Lanczos vector vk + 1, k = 1, 2,...
                ///The general iteration is similar to the case k = 1 with v0 = 0:
                ///
                /// p1 = Operator * v1 - beta1 * v0,
                /// alpha1 = v1'p1,
                /// q2 = p2 - alpha1 * v1,
                /// beta2 ^ 2 = q2'q2,
                /// v2 = (1 / beta2) q2.
                ///
                /// Again, y = betak P vk, where  P = C * *(-1).
                /// ....more description needed.
                /// -----------------------------------------------------------------

                double s = 1.0 / beta;    // Normalize previous vector(in y).
                Vector v = y; // No need to copy: y will point to another vector shortly after this
                v.ScaleIntoThis(s); // v = vk if P = I

                y = MinimumResidual.ShiftedMatrixVectorMult(A, v, shift);
                if (itn >= 2) y.AxpyIntoThis(-beta / oldb, r1); //WARNING: this works if itn is initialized and updated as in the matlab script

                double alfa = v * y;              // alphak
                y.AxpyIntoThis(-alfa / beta, r2);
                r1 = r2; // No need to copy: r2 will point to another vector after this
                r2 = y; // In the preconditioned version I can get away without copying it. 
                y = M.SolveLinearSystem(r2);
                oldb = beta; // oldb = betak
                beta = r2 * y;  // beta = betak+1^2
                if (beta < 0) throw new IndefiniteMatrixException("The preconditioner M is not positive definite.");
                beta = Math.Sqrt(beta);
                tnorm2 += alfa * alfa + oldb * oldb + beta * beta;

                if (itn == 1) //Initialize a few things.
                {
                    if (beta / beta1 <= 10.0 * eps) //beta2 = 0 or ~0
                    {
                        istop = -1; // Terminate later.
                    }
                }

                /// Apply previous rotation Qk-1 to get
                ///   [deltak epslnk + 1] = [cs  sn][dbark    0]
                ///   [gbar k dbar k + 1]   [sn - cs][alfak betak + 1].

                double oldeps = epsln;
                double delta = cs * dbar + sn * alfa; // delta1 = 0         deltak
                double gbar = sn * dbar - cs * alfa;  // gbar 1 = alfa1     gbar k
                double gbarSquared = gbar * gbar;
                epsln = sn * beta;                    // epsln2 = 0         epslnk + 1
                dbar = -cs * beta;                    // dbar 2 = beta2     dbar k+1
                double root = Math.Sqrt(gbarSquared + dbar * dbar);
                double Arnorm = phibar * root;        // || Ar{ k - 1}||

                /// Compute the next plane rotation Qk

                double gamma = Math.Sqrt(gbarSquared + beta * beta); // gammak
                if (gamma < eps) gamma = eps;
                cs = gbar / gamma;                                   // ck
                sn = beta / gamma;                                   // sk
                double phi = cs * phibar;                                   // phik
                phibar = sn * phibar;                                // phibark + 1

                /// Update  x.

                double denom = 1.0 / gamma;
                Vector w1 = w2.Copy();
                w2 = w;
                // Do efficiently: w = (v - oldeps * w1 - delta * w2) * denom 
                w = v.Axpy(-oldeps, w1);
                w.AxpyIntoThis(-delta, w2);
                w.ScaleIntoThis(denom);
                x.AxpyIntoThis(phi, w);

                /// Go round again.

                if (gmax < gamma) gmax = gamma;
                if (gmin > gamma) gmin = gamma;
                double z = rhs1 / gamma;
                rhs1 = rhs2 - delta * z;
                rhs2 = -epsln * z;

                /// Estimate various norms.

                Anorm = Math.Sqrt(tnorm2);
                ynorm = x.Norm2();
                double epsa = Anorm * eps;
                double epsx = Anorm * ynorm * eps;
                double epsr = Anorm * ynorm * residualTolerance;
                double diag = gbar;
                if (diag == 0) diag = epsa;

                qrnorm = phibar;
                rnorm = qrnorm;
                double test1 = rnorm / (Anorm * ynorm);    //  || r || / (|| A || || x ||)
                double test2 = root / Anorm;               // || Ar{ k - 1}|| / (|| A || || r_{ k - 1}||)


                /// Estimate  cond(A).
                /// In this version we look at the diagonals of R  in the
                /// factorization of the lower Hessenberg matrix,  Q* H = R,
                /// where H is the tridiagonal matrix from Lanczos with one
                /// extra row, beta(k + 1) e_k ^ T.

                Acond = gmax / gmin;

                /// See if any of the stopping criteria are satisfied.
                /// In rare cases, istop is already - 1 from above (Abar = const* I).

                if (istop == 0)
                {
                    double t1 = 1.0 + test1;      // These tests work if residualTolerance < double.Epsilon. 
                    double t2 = 1.0 + test2;      //TODO: then test that directly, like the last ones. Geez
                    if (t2 <= 1.0) istop = 2;
                    if (t1 <= 1.0) istop = 1;

                    if (itn >= maxIterations) istop = 6;
                    if (Acond >= 0.1 / eps) istop = 4; // TODO: shouldn't this be case istop=5?
                    if (epsx >= beta1) istop = 3;
                    //%if rnorm <= epsx   , istop = 2; end //They were commented out in the original code
                    //%if rnorm <= epsr   , istop = 1; end //They were commented out in the original code
                    if (test2 <= residualTolerance) istop = 2;
                    if (test1 <= residualTolerance) istop = 1;
                }

                if (printIterations) MinimumResidual.WriteIterationData(A, b, shift, x, itn, maxIterations, test1, test2,
                    Anorm, Acond, gbar, qrnorm, istop, epsx, epsr);

                if (istop != 0)
                {
                    var stats = new MinresStatistics
                    {
                        IterationsRequired = itn, //WARNING: take care if you change itn to be a for loop variable or sth else
                        TerminationCause = istop,
                        MatrixCondition = Acond,
                        MatrixNorm = Anorm,
                        MatrixTimesResidualNorm = Arnorm,
                        ResidualNorm = rnorm,
                        YNorm = ynorm
                    };
                    return (x, stats);
                }
            }
            throw new Exception("Should not have reached here");
        }

        private static void CheckSymmetricPreconditioner(IPreconditioner M, Vector y, Vector r1)
        {
            Vector r2 = M.SolveLinearSystem(y);
            double s = y * y;
            double t = r1 * r2;
            double z = Math.Abs(s - t);
            double epsa = (s + eps) * Math.Pow(eps, 1.0 / 3.0);
            if (z > epsa) throw new AsymmetricMatrixException("The preconditioner M is not symmetric.");
        }
    }
}