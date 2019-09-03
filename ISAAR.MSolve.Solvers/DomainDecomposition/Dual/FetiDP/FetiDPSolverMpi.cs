using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using ISAAR.MSolve.Discretization.Exceptions;
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

//TODO: Add time logging
//TODO: Use a base class for the code that is identical between FETI-1 and FETI-DP.
namespace ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP
{
    public class FetiDPSolverMpi : ISolverMpi
    {
        internal const string name = "FETI-DP Solver"; // for error messages
        private readonly ICornerNodeSelection cornerNodeSelection;
        private readonly ICrosspointStrategy crosspointStrategy = new FullyRedundantConstraints();
        private readonly DofOrdererMpi dofOrderer;
        private readonly FetiDPDofSeparatorMpi dofSeparator;
        private readonly FetiDPMatrixManagerMpi matrixManager;
        private readonly IModelMpi model;
        private readonly bool problemIsHomogeneous;
        private readonly ProcessDistribution procs;
        private readonly IStiffnessDistribution stiffnessDistribution;

        private HashSet<INode> cornerNodesGlobal_master;
        private bool factorizeInPlace = true;
        private bool isStiffnessModified = true;
        private LagrangeMultipliersEnumeratorMpi lagrangeEnumerator;
        //private ISubdomain subdomain;

        public FetiDPSolverMpi(ProcessDistribution processDistribution, IModelMpi model,
            ICornerNodeSelection cornerNodeSelection, IFetiDPMatrixManagerFactory matrixManagerFactory,
            bool problemIsHomogeneous)
        {
            this.procs = processDistribution;
            if (model.NumSubdomains == 1) throw new InvalidSolverException(
                $"Process {processDistribution.OwnRank}: {name} cannot be used if there is only 1 subdomain");
            this.model = model;
            this.Subdomain = model.GetSubdomain(processDistribution.OwnSubdomainID);
            this.cornerNodeSelection = cornerNodeSelection;

            this.dofOrderer = new DofOrdererMpi(processDistribution, new NodeMajorDofOrderingStrategy(), new NullReordering());
            this.dofSeparator = new FetiDPDofSeparatorMpi(processDistribution, model);

            // Matrix managers and linear systems
            this.matrixManager = new FetiDPMatrixManagerMpi(processDistribution, model, this.dofSeparator, matrixManagerFactory);
            //TODO: This will call HandleMatrixWillBeSet() once for each subdomain. For now I will clear the data when 
            //      BuildMatrices() is called. Redesign this.
            //matrixManager.LinearSystem.MatrixObservers.Add(this); 

            // Interface problem
            //this.interfaceProblemSolver = interfaceProblemSolver;

            // Homogeneous/heterogeneous problems
            this.problemIsHomogeneous = problemIsHomogeneous;
            if (problemIsHomogeneous)
            {
                this.stiffnessDistribution = new HomogeneousStiffnessDistributionMpi(processDistribution, model, dofSeparator,  
                    new FetiDPHomogeneousDistributionLoadScaling(dofSeparator));
            }
            else throw new NotImplementedException();
        }

        public HashSet<INode> CornerNodesGlobal
        {
            get
            {
                procs.CheckProcessIsMaster();
                return cornerNodesGlobal_master;
            }
        }

        public HashSet<INode> CornerNodesSubdomain { get; private set; }

        //TODO: I do not like these dependencies. The analyzer should not have to know that it must call ScatterSubdomainData() 
        //      before accessing the linear system or the subdomain.
        public ILinearSystem LinearSystem => matrixManager.GetSubdomainMatrixManager(Subdomain).LinearSystem;

        public SolverLogger Logger { get; } = new SolverLogger(name);
        public string Name => name;
        public INodalLoadDistributor NodalLoadDistributor => stiffnessDistribution;
        public ISubdomain Subdomain { get; }

        private string Header => $"Process {procs.OwnRank}, {this.GetType().Name}: ";

        public IMatrix BuildGlobalMatrix(IElementMatrixProvider elementMatrixProvider)
        {
            HandleMatrixWillBeSet(); //TODO: temporary solution to avoid this getting called once for each linear system/observable

            var watch = new Stopwatch();
            watch.Start();

            IMatrix Kff;
            if (Subdomain.StiffnessModified)
            {
                Debug.WriteLine($"Process {procs.OwnRank}, {this.GetType().Name}:"
                    + $" Assembling the free-free stiffness matrix of subdomain {procs.OwnSubdomainID}");
                Kff = matrixManager.GetSubdomainMatrixManager(Subdomain).BuildFreeDofsMatrix(Subdomain.FreeDofOrdering, 
                    Subdomain.EnumerateElements(), elementMatrixProvider);
                LinearSystem.Matrix = Kff; //TODO: This should be done by the solver not the analyzer. This method should return void.
            }
            else
            {
                Kff = (IMatrix)(LinearSystem.Matrix); //TODO: remove the cast
            }

            watch.Stop();
            if (procs.IsMasterProcess) Logger.LogTaskDuration("Matrix assembly", watch.ElapsedMilliseconds);

            this.Initialize(); //TODO: Should this be called by the analyzer? Probably not, since it must be called before DistributeBoundaryLoads().
            return Kff;
        }

        public void HandleMatrixWillBeSet()
        {
            isStiffnessModified = true;
            throw new NotImplementedException();
        }

        public void Initialize()
        {
            //var watch = new Stopwatch();
            //watch.Start();

            //// Identify corner nodes
            //CornerNodesSubdomain = cornerNodeSelection.GetCornerNodesOfSubdomain(Subdomain); // This may cause a change in connectivity TODO: Query ICornerNodeSelectionMpi and act accordingly

            //// Define the various dof groups
            //dofSeparator.SeparateDofs(cornerNodeSelection, matrixManager);

            ////TODO: B matrices could also be reused in some cases
            //// Define lagrange multipliers and boolean matrices. 
            //this.lagrangeEnumerator = new FetiDPLagrangeMultipliersEnumeratorMpi(procs, model, crosspointStrategy, dofSeparator);
            //lagrangeEnumerator.CalcBooleanMatrices();

            //// Log dof statistics
            //watch.Stop();
            //if (procs.IsMasterProcess)
            //{
            //    Logger.LogTaskDuration("Dof ordering", watch.ElapsedMilliseconds);
            //    Logger.LogNumDofs("Expanded domain dofs", -1); //TODO: There must be MPI communication to fill in this data, which is pretty useless. Remove it.
            //    Logger.LogNumDofs("Lagrange multipliers", lagrangeEnumerator.NumLagrangeMultipliers);
            //    if (procs.IsMasterProcess) Logger.LogNumDofs("Corner dofs", dofSeparator.globalDofs.NumGlobalCornerDofs);
            //}

            //// Use the newly created stiffnesses to determine the stiffness distribution between subdomains.
            ////TODO: Should this be done here or before factorizing by checking that isMatrixModified? 
            //stiffnessDistribution.Update();
            //subdomainGlobalMapping = new FetiDPSubdomainGlobalMappingMpi(model, dofSeparator, stiffnessDistribution);
        }

        public void OrderDofs(bool alsoOrderConstrainedDofs)
        {
            var watch = new Stopwatch();
            watch.Start();

            // Order dofs
            if (Subdomain.ConnectivityModified)
            {
                matrixManager.GetSubdomainMatrixManager(Subdomain).HandleDofOrderingWillBeModified(); //TODO: Not sure about this
            }

            // This should not create subdomain-global mappings which require MPI communication
            //TODO: What about subdomain-global mappings, especially for boundary dofs? Who should create them? 
            dofOrderer.OrderFreeDofs(model);

            if (alsoOrderConstrainedDofs) Subdomain.ConstrainedDofOrdering = dofOrderer.OrderConstrainedDofs(Subdomain);

            // Log dof statistics
            watch.Stop();
            Logger.LogTaskDuration("Dof ordering", watch.ElapsedMilliseconds);
            Logger.LogNumDofs("Global dofs", model.GlobalDofOrdering.NumGlobalFreeDofs);
        }

        public void PreventFromOverwrittingSystemMatrices() => factorizeInPlace = false;

        public void Solve()
        {
            //TODO: This is how you do the static condensation of vectors
            matrixManager.GetSubdomainMatrixManager(Subdomain).ExtractCornerRemainderRhsSubvectors();

            if (isStiffnessModified)
            {
                //TODO: This is how you do the static condensation of matrices
                ISubdomain subdomain = model.GetSubdomain(procs.OwnSubdomainID);
                if (subdomain.StiffnessModified)
                {
                    matrixManager.GetSubdomainMatrixManager(Subdomain).ExtractCornerRemainderSubmatrices();
                }

                // Reorder internal dofs if needed by the preconditioner. TODO: Should I have done this previously in Initialize()?


                // Calculate the preconditioner before factorizing each subdomain's Kff 


                // Factorize each subdomain's Krr


                // Define FETI-DP flexibility matrices

                // Static condensation of remainder dofs (Schur complement)
                //coarseProblemSolver.CalcInverseCoarseProblemMatrix(cornerNodeSelection, dofSeparator);

                isStiffnessModified = false;
            }

            // Static condensation for the force vectors
            matrixManager.CalcCoarseProblemRhs();
        }
    }
}
