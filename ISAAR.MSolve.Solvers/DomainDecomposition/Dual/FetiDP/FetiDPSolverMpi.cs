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
        private readonly IDofOrderer dofOrderer = new DofOrderer(new NodeMajorDofOrderingStrategy(), new NullReordering());
        private readonly Intracommunicator comm;
        private readonly int master;
        private readonly IFetiDPSubdomainMatrixManagerFactory matrixManagerFactory;
        private readonly IStructuralModel model;
        private readonly MpiTransfer transfer;

        private bool factorizeInPlace = true;
        private ISingleSubdomainLinearSystem linearSystem;
        private IFetiDPSubdomainMatrixManager matrixManager;
        private ISubdomain subdomain;

        public FetiDPSolverMpi(IStructuralModel model, IFetiDPSubdomainMatrixManagerFactory matrixManagerFactory, 
            int masterProcess, ISubdomainSerializer serializer)
        {
            this.model = model;
            this.matrixManagerFactory = matrixManagerFactory;

            this.comm = Communicator.world;
            this.master = masterProcess;
            this.transfer = new MpiTransfer(serializer); //TODO: the serializer should be accessed by the model.
        }

        //TODO: I do not like these dependencies. The analyzer should not have to know that it must call ScatterSubdomainData() 
        //      before accessing the linear system or the subdomain.
        public ILinearSystem LinearSystem
        {
            get
            {
                if (linearSystem == null) throw new InvalidOperationException("The subdomain data must be scattered first.");
                return linearSystem;
            }
        }

        public ISubdomain Subdomain
        {
            get
            {
                if (subdomain == null) throw new InvalidOperationException("The subdomain data must be scattered first.");
                return subdomain;
            }
        }

        public SolverLogger Logger { get; } = new SolverLogger(name);
        public string Name => name;

        public IMatrix BuildGlobalMatrices(IElementMatrixProvider elementMatrixProvider)
        {
            IMatrix Kff = matrixManager.BuildGlobalMatrix(subdomain.FreeDofOrdering, subdomain.Elements, elementMatrixProvider);
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
        { //TODO: What about subdomain-global mapping? Especially for boundary dofs.
            subdomain.FreeDofOrdering = dofOrderer.OrderFreeDofs(subdomain);
            if (alsoOrderConstrainedDofs) subdomain.ConstrainedDofOrdering = dofOrderer.OrderConstrainedDofs(subdomain);
        }

        public void PreventFromOverwrittingSystemMatrices() => factorizeInPlace = false;

        public void ScatterSubdomainData()
        {
            subdomain = transfer.ScatterSubdomains(model, master);
            matrixManager = matrixManagerFactory.CreateMatricesManager(subdomain);
            linearSystem = matrixManager.LinearSystem;
        }

        public void Solve()
        {
            // Print the trace of each stiffness matrix
            int rank = comm.Rank;
            double trace = Trace(linearSystem.Matrix);
            Console.WriteLine($"(process {rank}) Subdomain {subdomain.ID}: trace(stiffnessMatrix) = {trace}");
        }

        public static double Trace(IMatrixView matrix)
        {
            double trace = 0.0;
            for (int i = 0; i < matrix.NumRows; ++i) trace += matrix[i, i];
            return trace;
        }
    }
}
