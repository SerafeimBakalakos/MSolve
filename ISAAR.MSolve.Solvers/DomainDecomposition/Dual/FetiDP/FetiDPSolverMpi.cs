//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.Linq;
//using System.Text;
//using ISAAR.MSolve.Discretization.Exceptions;
//using ISAAR.MSolve.Discretization.Interfaces;
//using ISAAR.MSolve.Discretization.Transfer;
//using ISAAR.MSolve.LinearAlgebra.Matrices;
//using ISAAR.MSolve.LinearAlgebra.Vectors;
//using ISAAR.MSolve.Solvers.Commons;
//using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.CornerNodes;
//using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.DofSeparation;
//using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.InterfaceProblem;
//using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.StiffnessMatrices;
//using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.StiffnessDistribution;
//using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.LagrangeMultipliers;
//using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.StiffnessDistribution;
//using ISAAR.MSolve.Solvers.LinearSystems;
//using ISAAR.MSolve.Solvers.Ordering;
//using ISAAR.MSolve.Solvers.Ordering.Reordering;
//using MPI;
//using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.Pcg;
//using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.Preconditioning;
//using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.FlexibilityMatrix;
//using ISAAR.MSolve.Solvers.Logging;

////TODO: Add time logging
////TODO: Use a base class for the code that is identical between FETI-1 and FETI-DP.
//namespace ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP
//{
//    public class FetiDPSolverMpi : ISolverMpi
//    {
//        internal const string name = "FETI-DP Solver"; // for error messages and logging
//        private readonly ICrosspointStrategy crosspointStrategy = new FullyRedundantConstraints();
//        private readonly DofOrdererMpi dofOrderer;
//        private readonly FetiDPDofSeparatorMpi dofSeparator;
//        private readonly FetiDPInterfaceProblemSolverMpi interfaceProblemSolver;
//        private readonly LagrangeMultipliersEnumeratorMpi lagrangesEnumerator;
//        private readonly FetiDPMatrixManagerMpi matrixManager;
//        private readonly IModelMpi model;
//        private readonly string msgHeader;
//        private readonly bool problemIsHomogeneous;
//        private readonly IFetiPreconditionerFactory precondFactory;
//        private readonly IFetiPreconditioningOperations preconditioning; //TODO: perhaps this should be hidden inside IFetiPreconditionerFactory
//        private readonly ProcessDistribution procs;
//        private readonly IStiffnessDistribution stiffnessDistribution;
//        private readonly FetiDPSubdomainGlobalMappingMpi subdomainGlobalMapping;

//        private bool factorizeInPlace = true;
//        private FetiDPFlexibilityMatrixMpi flexibility;
//        private bool isStiffnessModified = true;
//        private IFetiPreconditioner preconditioner;

//        public FetiDPSolverMpi(ProcessDistribution processDistribution, IModelMpi model, ICornerNodeSelection cornerNodeSelection,
//            IFetiDPMatrixManagerFactory matrixManagerFactory, IFetiPreconditioningOperations preconditioning,  
//            PcgSettings pcgSettings, bool problemIsHomogeneous)
//        {
//            this.procs = processDistribution;
//            if (model.NumSubdomains == 1) throw new InvalidSolverException(
//                $"Process {processDistribution.OwnRank}: {name} cannot be used if there is only 1 subdomain");
//            this.model = model;
//            this.Subdomain = model.GetSubdomain(processDistribution.OwnSubdomainID);

//            this.Logger = new SolverLoggerMpi(procs, name);
//            this.msgHeader = $"Process {procs.OwnRank}, {this.GetType().Name}: ";

//            // Connectivity
//            this.CornerNodes = cornerNodeSelection;
//            this.dofOrderer = new DofOrdererMpi(processDistribution, new NodeMajorDofOrderingStrategy(), new NullReordering());
//            this.dofSeparator = new FetiDPDofSeparatorMpi(processDistribution, model);
//            this.lagrangesEnumerator = new LagrangeMultipliersEnumeratorMpi(procs, model, crosspointStrategy, dofSeparator);

//            // Matrix managers and linear systems
//            this.matrixManager = new FetiDPMatrixManagerMpi(procs, model, this.dofSeparator, matrixManagerFactory);
//            //TODO: This will call HandleMatrixWillBeSet() once for each subdomain. For now I will clear the data when 
//            //      BuildMatrices() is called. Redesign this.
//            //matrixManager.LinearSystem.MatrixObservers.Add(this); 

//            // Preconditioning
//            this.preconditioning = preconditioning;
//            this.precondFactory = new FetiPreconditionerMpi.Factory(procs);

//            // Interface problem
//            this.interfaceProblemSolver = new FetiDPInterfaceProblemSolverMpi(procs, model, pcgSettings);

//            // Homogeneous/heterogeneous problems
//            this.problemIsHomogeneous = problemIsHomogeneous;
//            if (problemIsHomogeneous)
//            {
//                this.stiffnessDistribution = new HomogeneousStiffnessDistributionMpi(procs, model, dofSeparator,  
//                    new FetiDPHomogeneousDistributionLoadScaling(dofSeparator));
//            }
//            else throw new NotImplementedException();

//            this.subdomainGlobalMapping = new FetiDPSubdomainGlobalMappingMpi(procs, model, dofSeparator, stiffnessDistribution);
//        }

//        public ICornerNodeSelection CornerNodes { get; }
//        public ISolverLogger Logger { get; }
//        public string Name => name;
//        public INodalLoadDistributor NodalLoadDistributor => stiffnessDistribution;
//        public ISubdomain Subdomain { get; }

//        public void BuildGlobalMatrix(IElementMatrixProvider elementMatrixProvider)
//        {
//            HandleMatrixWillBeSet(); //TODO: temporary solution to avoid this getting called once for each linear system/observable

//            Logger.StartMeasuringTime();

//            IFetiDPSubdomainMatrixManager subdomainMatrices = matrixManager.GetFetiDPSubdomainMatrixManager(Subdomain);
//            if (Subdomain.StiffnessModified)
//            {
//                Debug.WriteLine($"Process {procs.OwnRank}, {this.GetType().Name}:"
//                    + $" Assembling the free-free stiffness matrix of subdomain {procs.OwnSubdomainID}");
//                subdomainMatrices.BuildFreeDofsMatrix(Subdomain.FreeDofOrdering, elementMatrixProvider);
//            }

//            Logger.LogCurrentTaskDuration("Matrix assembly");

//            this.Initialize(); //TODO: Should this be called by the analyzer? Probably not, since it must be called before DistributeBoundaryLoads().
//        }

//        //TODO: I do not like these dependencies. The analyzer should not have to know that it must call ScatterSubdomainData() 
//        //      before accessing the linear system or the subdomain.
//        public ILinearSystem GetLinearSystem(ISubdomain subdomain)
//        {
//            procs.CheckProcessMatchesSubdomain(subdomain.ID);
//            return matrixManager.GetFetiDPSubdomainMatrixManager(subdomain).LinearSystem;
//        } 

//        public void HandleMatrixWillBeSet()
//        {
//            isStiffnessModified = true;
//            throw new NotImplementedException();
//        }

//        public void Initialize()
//        {
//            Logger.StartMeasuringTime();

//            // Define the various dof groups
//            dofSeparator.SeparateDofs(CornerNodes, matrixManager);

//            //TODO: B matrices could also be reused in some cases
//            // Define lagrange multipliers and boolean matrices. 
//            lagrangesEnumerator.CalcBooleanMatrices(dofSeparator.GetRemainderDofOrdering);

//            // Log dof statistics
//            Logger.LogCurrentTaskDuration("Dof ordering");
//            Logger.LogNumDofs("Lagrange multipliers", lagrangesEnumerator.NumLagrangeMultipliers);
//            Logger.LogNumDofs("Corner dofs", dofSeparator.NumGlobalCornerDofs);

//            // Use the newly created stiffnesses to determine the stiffness distribution between subdomains.
//            //TODO: Should this be done here or before factorizing by checking that isMatrixModified? 
//            stiffnessDistribution.Update();
//        }

//        public void OrderDofs(bool alsoOrderConstrainedDofs)
//        {
//            Logger.StartMeasuringTime();

//            // Order dofs
//            if (Subdomain.ConnectivityModified)
//            {
//                matrixManager.GetFetiDPSubdomainMatrixManager(Subdomain).HandleDofOrderingWillBeModified(); //TODO: Not sure about this
//            }

//            // This should not create subdomain-global mappings which require MPI communication
//            //TODO: What about subdomain-global mappings, especially for boundary dofs? Who should create them? 
//            dofOrderer.OrderFreeDofs(model);

//            if (alsoOrderConstrainedDofs) Subdomain.ConstrainedDofOrdering = dofOrderer.OrderConstrainedDofs(Subdomain);

//            // Log dof statistics
//            Logger.LogCurrentTaskDuration("Dof ordering");
//            if (procs.IsMasterProcess) Logger.LogNumDofs("Global dofs", model.GlobalDofOrdering.NumGlobalFreeDofs);
//        }

//        public void PreventFromOverwrittingSystemMatrices() => factorizeInPlace = false;

//        public void Solve()
//        {
//            IFetiDPSubdomainMatrixManager subdomainMatrices = matrixManager.GetFetiDPSubdomainMatrixManager(Subdomain);
//            ISingleSubdomainLinearSystem linearSystem = subdomainMatrices.LinearSystem;
//            linearSystem.SolutionConcrete = linearSystem.CreateZeroVectorConcrete();

//            if (isStiffnessModified)
//            {
//                // Separate the stiffness matrix
//                Logger.StartMeasuringTime();
//                if (Subdomain.StiffnessModified)
//                {
//                    subdomainMatrices.ExtractCornerRemainderSubmatrices();
//                }
//                Logger.LogCurrentTaskDuration("Calculating coarse problem matrix");

//                // Calculate the preconditioner before factorizing each subdomain's Krr.
//                // The inter-subdomain stiffness distribution may have changed even if a subdomain's stiffness is the same.
//                Logger.StartMeasuringTime();
//                if (preconditioning.ReorderInternalDofsForFactorization) dofSeparator.ReorderInternalDofs(matrixManager);
//                preconditioner = precondFactory.CreatePreconditioner(preconditioning, model, dofSeparator, lagrangesEnumerator,
//                    matrixManager, stiffnessDistribution);
//                Logger.LogCurrentTaskDuration("Calculating preconditioner");

//                Logger.StartMeasuringTime();
//                // Factorize each subdomain's Krr
//                if (Subdomain.StiffnessModified)
//                {
//                    //TODO: If I can reuse Krr, I can also reuse its factorization. Therefore this must be inPlace. In contrast, FETI-1 needs Kff intact for Stiffness distribution, in the current design).
//                    Debug.WriteLine(msgHeader 
//                        + $"Inverting the remainder-remainder stiffness matrix of subdomain {Subdomain.ID} in place.");
//                    matrixManager.GetFetiDPSubdomainMatrixManager(Subdomain).InvertKrr(true);
//                }

//                // Calculate FETI-DP coarse problem matrix
//                matrixManager.CalcInverseCoarseProblemMatrix(CornerNodes);
//                flexibility = new FetiDPFlexibilityMatrixMpi(procs, model, dofSeparator, lagrangesEnumerator, matrixManager);
//                Logger.LogCurrentTaskDuration("Calculating coarse problem matrix");

//                isStiffnessModified = false;
//            }

//            // Calculate FETI-DP coarse problem rhs 
//            Logger.StartMeasuringTime();
//            subdomainMatrices.ExtractCornerRemainderRhsSubvectors();
//            matrixManager.CalcCoarseProblemRhs();
//            Logger.LogCurrentTaskDuration("Calculating coarse problem rhs");

//            Logger.StartMeasuringTime();
//            // Calculate the norm of the forces vector |f| = |K*u|. It is needed to check the convergence of PCG.
//            double globalForcesNorm = double.NaN; 
//            if (procs.IsMasterProcess)
//            {
//                globalForcesNorm = subdomainGlobalMapping.CalcGlobalForcesNorm(
//                    sub => matrixManager.GetFetiDPSubdomainMatrixManager(sub).LinearSystem.RhsConcrete);
//            }

//            // Solve interface problem
//            (Vector lagranges, Vector uc) = interfaceProblemSolver.SolveInterfaceProblem(matrixManager, lagrangesEnumerator,
//                flexibility, preconditioner, globalForcesNorm, Logger);
//            Logger.LogCurrentTaskDuration("Solving interface problem");

//            // Calculate the displacements of each subdomain
//            Logger.StartMeasuringTime();
//            //Dictionary<int, Vector> actualDisplacements = CalcActualDisplacements(lagranges, uc, fr);
//            //foreach (var idSystem in linearSystems) idSystem.Value.SolutionConcrete = actualDisplacements[idSystem.Key];
//            Logger.LogCurrentTaskDuration("Calculate displacements from lagrange multipliers");

//            Logger.IncrementAnalysisStep();
//        }
//    }
//}
