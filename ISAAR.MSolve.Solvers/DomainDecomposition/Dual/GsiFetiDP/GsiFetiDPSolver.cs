using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.Discretization.Transfer;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Solvers.Commons;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.CornerNodes;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.DofSeparation;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.InterfaceProblem;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.StiffnessMatrices;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.StiffnessDistribution;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.LagrangeMultipliers;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.StiffnessDistribution;
using ISAAR.MSolve.Solvers.LinearSystems;
using ISAAR.MSolve.Solvers.Ordering;
using ISAAR.MSolve.Solvers.Ordering.Reordering;
using MPI;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.Pcg;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.Preconditioning;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.FlexibilityMatrix;
using ISAAR.MSolve.Solvers.Logging;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.Displacements;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP3d.Augmentation;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP3d.StiffnessMatrices;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP3d.FlexibilityMatrix;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using System.IO;
using ISAAR.MSolve.LinearAlgebra.Matrices.Builders;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.GsiFetiDP;
using ISAAR.MSolve.Solvers.Assemblers;
using ISAAR.MSolve.LinearAlgebra.Iterative.PreconditionedConjugateGradient;

//TODO: Add time logging
//TODO: Use a base class for the code that is identical between FETI-1 and FETI-DP.
//TODO: Only works with SuiteSparse for now
//TODO: Generation of the RHS vector is problematic. Analyzer creates one rhs vector per subdomain and stores it in that  
//      subdomain's linear system. Then the globalRhs is assembled in GsiFetiDPSolver.Solve(). Thus boundary loads are 
//      distributed and then summed again. A more important problem is that in linear systems of each subdomain, the residual
//      vector of GSI's PCG is stored, overwriting the force vectors. Instead the analyzer should create a global RHS directly.
namespace ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP3d
{
    public class GsiFetiDPSolver : ISolverMpi
    {
        internal const string name = "GEI-FETI-DP Solver"; // for error messages and logging
        private readonly DofOrderer dofOrderer;
        private readonly FetiDP3dSolverSerial fetiDP;
        //private readonly Dictionary<ISubdomain, SingleSubdomainSystemMpi<DokSymmetric>> linearSystems;
        private readonly IModel model;
        private readonly string msgHeader;
        private readonly GsiFetiDPMatrix gsiMatrix;
        private readonly GsiFetiDPPreconditioner gsiPreconditioner;
        private readonly PcgAlgorithm pcgAlgorithm;

        //private bool factorizeInPlace = true;
        private bool isStiffnessModified = true;

        public GsiFetiDPSolver(IModel model, FetiDP3dSolverSerial fetiDP, PcgAlgorithm pcgAlgorithm)
        {
            this.msgHeader = $"{this.GetType().Name}: ";

            if (model.NumSubdomains == 1) throw new InvalidSolverException(msgHeader 
                + $"This solver cannot be used if there is only 1 subdomain");
            this.model = model;
            this.fetiDP = fetiDP;
            this.pcgAlgorithm = pcgAlgorithm;

            this.Logger = new SolverLoggerSerial(name);
            this.dofOrderer = new DofOrderer(new NodeMajorDofOrderingStrategy(), new NullReordering());
            //this.linearSystems = new Dictionary<ISubdomain, SingleSubdomainSystemMpi<DokSymmetric>>();
            this.gsiMatrix = new GsiFetiDPMatrix(model);
            this.gsiPreconditioner = new GsiFetiDPPreconditioner(model, fetiDP);
        }

        public Vector GlobalDisplacements { get; set; }
        public Vector GlobalForces { get; set; }

        public ISolverLogger Logger { get; }
        public string Name => name;

        public INodalLoadDistributor NodalLoadDistributor => fetiDP.NodalLoadDistributor;

        /// <summary>
        ///  builds Kff of each subdomain
        /// </summary>
        /// <param name="elementMatrixProvider"></param>
        public void BuildGlobalMatrix(IElementMatrixProvider elementMatrixProvider)
        {
            HandleMatrixWillBeSet(); //TODO: temporary solution to avoid this getting called once for each linear system/observable
            fetiDP.HandleMatrixWillBeSet();

            Logger.StartMeasuringTime();

            var assembler = new SymmetricDokAssembler();
            gsiMatrix.MatricesKff = new Dictionary<ISubdomain, CsrMatrix>();
            foreach (ISubdomain subdomain in model.EnumerateSubdomains())
            {
                Debug.WriteLine(msgHeader
                        + $" Assembling the free-free stiffness matrix of subdomain {subdomain.ID}");
                DokSymmetric dokKff = assembler.BuildGlobalMatrix(
                    subdomain.FreeDofOrdering, subdomain.EnumerateElements(), elementMatrixProvider);
                fetiDP.GetLinearSystem(subdomain).Matrix = dokKff; //TODO: Therefore only SuiteSparse FETI-DP is allowed
                gsiMatrix.MatricesKff[subdomain] = dokKff.BuildCsrMatrix();
            }
            Logger.LogCurrentTaskDuration("Matrix assembly");

            //TODO: When should this be called?
            fetiDP.Initialize(); 
        }

        public ILinearSystemMpi GetLinearSystem(ISubdomain subdomain) => fetiDP.GetLinearSystem(subdomain);
        
        public void HandleMatrixWillBeSet()
        {
            //Do nothing
            //isStiffnessModified = true;
            //foreach (ISubdomain subdomain in model.EnumerateSubdomains())
            //{
            //    if (subdomain.StiffnessModified)
            //    {
            //        Debug.WriteLine(msgHeader + $"Clearing saved matrices of subdomain {subdomain.ID}.");
            //        linearSystems[subdomain].Matrix = null;
            //    }
            //}
        }

        public void Initialize()
        {
        }

        public void OrderDofs(bool alsoOrderConstrainedDofs)
        {
            fetiDP.OrderDofs(alsoOrderConstrainedDofs);
        }

        public void PreventFromOverwrittingSystemMatrices()
        {
            // Do nothing
            /*factorizeInPlace = false;*/
        }

        public void Solve()
        {
            // Set up FETI-DP as preconditioner
            if (isStiffnessModified)
            {
                Logger.StartMeasuringTime();
                gsiPreconditioner.Update();
                Logger.LogCurrentTaskDuration("Calculating preconditioner");

                isStiffnessModified = false;
            }

            // Create global RHS vector for PCG
            Logger.StartMeasuringTime();
            int numGlobalDofs = model.GlobalDofOrdering.NumGlobalFreeDofs;
            GlobalForces = Vector.CreateZero(numGlobalDofs);
            foreach (ISubdomain subdomain in model.EnumerateSubdomains())
            {
                IVectorView subdomainRhs = fetiDP.GetLinearSystem(subdomain).RhsVector;
                model.GlobalDofOrdering.AddVectorSubdomainToGlobal(subdomain, subdomainRhs, GlobalForces);
            }
            Logger.LogCurrentTaskDuration("Calculating GSI rhs");

            // Solve interface problem
            Logger.StartMeasuringTime();
            GlobalDisplacements = Vector.CreateZero(numGlobalDofs); //TODO: reuse previous one
            bool zeroInitialGuess = true;
            pcgAlgorithm.Solve(gsiMatrix, gsiPreconditioner, GlobalForces, GlobalDisplacements, zeroInitialGuess, 
                () => Vector.CreateZero(numGlobalDofs));
            Logger.LogCurrentTaskDuration("Solving GSI problem");

            // Calculate the displacements of each subdomain
            Logger.StartMeasuringTime();
            foreach (ISubdomain subdomain in model.EnumerateSubdomains())
            {
                var subdomainSolution = (Vector)(fetiDP.GetLinearSystem(subdomain).Solution);
                model.GlobalDofOrdering.ExtractVectorSubdomainFromGlobal(subdomain, GlobalDisplacements, subdomainSolution);
            }
            Logger.LogCurrentTaskDuration("Extract subdomain displacements");

            Logger.IncrementAnalysisStep();
        }

        public class Builder
        {
            private readonly FetiDP3dSolverSerial fetiDP;

            /// <summary>
            /// Only SuiteSparse implementations of FETI-DP operations are allowed for now.
            /// </summary>
            /// <param name="fetiDP"></param>
            public Builder(FetiDP3dSolverSerial fetiDP)
            {
                this.fetiDP = fetiDP;
            }

            public PcgAlgorithm PcgAlgorithm { get; set; } = (new PcgAlgorithm.Builder()).Build();

            public GsiFetiDPSolver Build(IModel model)
            {
                return new GsiFetiDPSolver(model, fetiDP, PcgAlgorithm);
            }
        }
    }
}
