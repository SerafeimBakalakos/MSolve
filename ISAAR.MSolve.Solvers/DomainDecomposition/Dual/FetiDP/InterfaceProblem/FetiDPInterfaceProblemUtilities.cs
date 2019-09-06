﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.LinearAlgebra.Iterative;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Matrices.Operators;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Solvers.Commons;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.FlexibilityMatrix;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.StiffnessMatrices;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.LagrangeMultipliers;

//TODO: Should the matrix manager and lgarange enumerator be injected into the constructor?
namespace ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.InterfaceProblem
{
    
    public class FetiDPInterfaceProblemUtilities
    {
        public static Vector CalcCornerDisplacements(IFetiDPMatrixManager matrixManager, IFetiDPFlexibilityMatrix flexibility,
            Vector lagranges)
        {
            // uc = inv(KccStar) * (fcStar + FIrc^T * lagranges)
            Vector temp = flexibility.MultiplyGlobalFIrcTransposed(lagranges);
            temp.AddIntoThis(matrixManager.CoarseProblemRhs);
            return matrixManager.MultiplyInverseCoarseProblemMatrix(temp);
        }

        public static Vector CalcInterfaceProblemRhs(IFetiDPMatrixManager matrixManager, IFetiDPFlexibilityMatrix flexibility,
            Vector globalDr)
        {
            // rhs = dr - FIrc * inv(KccStar) * fcStar
            Vector temp = matrixManager.MultiplyInverseCoarseProblemMatrix(matrixManager.CoarseProblemRhs);
            temp = flexibility.MultiplyGlobalFIrc(temp);
            return globalDr - temp;
        }

        public static Vector CalcSubdomainDr(ISubdomain subdomain, IFetiDPMatrixManager matrixManager,
            ILagrangeMultipliersEnumerator lagrangesEnumerator)
        {
            // dr = sum_over_s( Br[s] * inv(Krr[s]) * fr[s])
            // This class only calculates dr[s] = Br[s] * inv(Krr[s]) * fr[s];
            // The summation is delegated to another class.

            SignedBooleanMatrixColMajor Br = lagrangesEnumerator.GetBooleanMatrix(subdomain);
            IFetiDPSubdomainMatrixManager subdomainMatrices = matrixManager.GetFetiDPSubdomainMatrixManager(subdomain);
            Vector temp = subdomainMatrices.MultiplyInverseKrrTimes(subdomainMatrices.Fr);
            return Br.Multiply(temp);
        }

        public static void CheckConvergence(IterativeStatistics stats)
        {
            if (!stats.HasConverged)
            {
                throw new IterativeSolverNotConvergedException("FETI-DP did not converge to a solution. PCG"
                    + $" algorithm run for {stats.NumIterationsRequired} iterations and the residual norm ratio was"
                    + $" {stats.ResidualNormRatioEstimation}");
            }
        }
    }
}