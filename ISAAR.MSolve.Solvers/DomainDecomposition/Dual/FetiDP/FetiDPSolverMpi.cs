using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.Discretization.Transfer;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.Matrices;
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
        private readonly DofOrdererMpi dofOrderer;
        private readonly IFetiDPSubdomainMatrixManagerFactory matrixManagerFactory;
        private readonly IModelMpi model;
        private readonly ProcessDistribution procs;
        private readonly int rank;

        private bool factorizeInPlace = true;
        private ISingleSubdomainLinearSystem linearSystem;
        private IFetiDPSubdomainMatrixManager matrixManager;
        //private ISubdomain subdomain;

        public FetiDPSolverMpi(ProcessDistribution processDistribution, IModelMpi model, 
            IFetiDPSubdomainMatrixManagerFactory matrixManagerFactory)
        {
            this.procs = processDistribution;
            this.model = model;
            this.matrixManagerFactory = matrixManagerFactory;
            this.dofOrderer = new DofOrdererMpi(processDistribution, new NodeMajorDofOrderingStrategy(), new NullReordering());
        }

        //TODO: I do not like these dependencies. The analyzer should not have to know that it must call ScatterSubdomainData() 
        //      before accessing the linear system or the subdomain.
        public ILinearSystem LinearSystem
        {
            get
            {
                try
                {
                    return linearSystem;
                }
                catch (Exception)
                {
                    throw new InvalidOperationException("The subdomain data must be scattered first.");
                }
            }
        }

        public ISubdomain Subdomain
        {
            get
            {
                try
                {
                    return model.GetSubdomain(procs.OwnSubdomainID);
                }
                catch (Exception)
                {
                    throw new InvalidOperationException("The subdomain data must be scattered first.");
                }
            }
        }

        public SolverLogger Logger { get; } = new SolverLogger(name);
        public string Name => name;

        public IMatrix BuildGlobalMatrices(IElementMatrixProvider elementMatrixProvider)
        {
            ISubdomain subdomain = model.GetSubdomain(procs.OwnSubdomainID);
            IMatrix Kff = matrixManager.BuildGlobalMatrix(subdomain.FreeDofOrdering, subdomain.EnumerateElements(), 
                elementMatrixProvider);
            linearSystem.Matrix = Kff;
            return Kff;
        }

        public void HandleMatrixWillBeSet()
        {
            throw new NotImplementedException();
        }

        public void Initialize()
        {
            throw new NotImplementedException();
        }

        public void OrderDofs(bool alsoOrderConstrainedDofs)
        {
            // This should not create subdomain-global mappings which require MPI communication
            //TODO: What about subdomain-global mappings, especially for boundary dofs? Who should create them? 
            dofOrderer.OrderFreeDofs(model); 

            ISubdomain subdomain = model.GetSubdomain(procs.OwnSubdomainID);
            if (alsoOrderConstrainedDofs) subdomain.ConstrainedDofOrdering = dofOrderer.OrderConstrainedDofs(subdomain);
        }

        public void PreventFromOverwrittingSystemMatrices() => factorizeInPlace = false;

        public void ScatterSubdomainData()
        {
            model.ScatterSubdomains();
            matrixManager = matrixManagerFactory.CreateMatricesManager(model.GetSubdomain(procs.OwnSubdomainID));
            linearSystem = matrixManager.LinearSystem;
        }

        public void Solve()
        {
            // Print the trace of each stiffness matrix
            double trace = Trace(linearSystem.Matrix);
            Console.WriteLine($"(process {procs.OwnRank}) Subdomain {procs.OwnSubdomainID}: trace(stiffnessMatrix) = {trace}");
        }

        private static double Trace(IMatrixView matrix)
        {
            double trace = 0.0;
            for (int i = 0; i < matrix.NumRows; ++i) trace += matrix[i, i];
            return trace;
        }
    }
}
