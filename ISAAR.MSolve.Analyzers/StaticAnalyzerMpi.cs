using System;
using System.Collections.Generic;
using ISAAR.MSolve.Analyzers.Interfaces;
using ISAAR.MSolve.Analyzers.NonLinear;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.Discretization.Transfer;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Logging.Interfaces;
using ISAAR.MSolve.Solvers;
using ISAAR.MSolve.Solvers.LinearSystems;
using MPI;

namespace ISAAR.MSolve.Analyzers
{
    public class StaticAnalyzerMpi : INonLinearParentAnalyzer
    {
        private readonly Intracommunicator comm;
        private readonly int master;
        private readonly IModelMpi model;
        //private readonly IStaticProvider provider; //TODO: Use this instead
        private readonly IElementMatrixProvider provider;
        private readonly ISolverMpi solver;

        public StaticAnalyzerMpi(IModelMpi model, ISolverMpi solver, IElementMatrixProvider provider,
            IChildAnalyzer childAnalyzer, int masterProcess) 
        {
            this.model = model;
            this.solver = solver;
            this.provider = provider;
            this.ChildAnalyzer = childAnalyzer;
            this.ChildAnalyzer.ParentAnalyzer = this;

            this.comm = Communicator.world;
            this.master = masterProcess;
        }

        public Dictionary<int, IAnalyzerLog[]> Logs { get; } = new Dictionary<int, IAnalyzerLog[]>();

        public IChildAnalyzer ChildAnalyzer { get; }

        public void BuildMatrices()
        {
            solver.LinearSystem.Matrix = solver.BuildGlobalMatrix(provider);
        }

        public IVector GetOtherRhsComponents(ILinearSystem linearSystem, IVector currentSolution)
        {
            //TODO: use a ZeroVector class that avoid doing useless operations or refactor this method. E.g. let this method 
            // alter the child analyzer's rhs vector, instead of the opposite (which is currently done).
            return linearSystem.CreateZeroVector();
        }

        public void Initialize(bool isFirstAnalysis = true)
        {
            int rank = comm.Rank;
            if (isFirstAnalysis)
            {
                model.ConnectDataStructures();
                model.ScatterSubdomains(); 

                // Order dofs
                solver.OrderDofs(false);

                // Initialize linear system
                solver.LinearSystem.Reset(); // Necessary to define the linear system's size 
                solver.LinearSystem.Subdomain.Forces = Vector.CreateZero(solver.LinearSystem.Size);
            }
            else
            {
                //TODO: Perhaps these shouldn't be done if an analysis has already been executed. The model will not be 
                //      modified. Why should the linear system be?
                solver.LinearSystem.Reset();
                solver.LinearSystem.Subdomain.Forces = Vector.CreateZero(solver.LinearSystem.Size);
            }

            //TODO: Perhaps this should be called by the child analyzer
            BuildMatrices();

            // Loads must be created after building the matrices.
            //TODO: Some loads may not have to be recalculated each time the stiffness changes.
            //model.AssignLoads(solver.DistributeNodalLoads);
            //linearSystem.RhsVector = linearSystem.Subdomain.Forces;

            if (ChildAnalyzer == null) throw new InvalidOperationException("Static analyzer must contain an embedded analyzer.");
            ChildAnalyzer.Initialize(isFirstAnalysis);
        }

        public void Solve()
        {
            if (ChildAnalyzer == null) throw new InvalidOperationException("Static analyzer must contain an embedded analyzer.");
            ChildAnalyzer.Solve();
            throw new NotImplementedException("Must take care of load assignment & distribution");
        }
    }
}
