using System;
using System.Collections.Generic;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Logging.Interfaces;
using MGroup.Analyzers.Interfaces;
using MGroup.Environments;
using MGroup.Solvers;
using MGroup.Solvers.LinearSystems;

namespace MGroup.Analyzers
{
    public class StaticAnalyzer : INonLinearParentAnalyzer
    {
        private readonly IReadOnlyDictionary<int, ILinearSystem> linearSystems;
        private readonly IComputeEnvironment environment;
        private readonly IStructuralModel model;
        private readonly IStaticProvider provider;
        private readonly ISolver solver;

        public StaticAnalyzer(IComputeEnvironment environment, IStructuralModel model, ISolver solver, IStaticProvider provider, 
            IChildAnalyzer childAnalyzer)
        {
            this.environment = environment;
            this.model = model;
            this.linearSystems = solver.LinearSystems;
            this.solver = solver;
            this.provider = provider;

            this.ChildAnalyzer = childAnalyzer;
            this.ChildAnalyzer.ParentAnalyzer = this;
        }

        public IChildAnalyzer ChildAnalyzer { get; }

        public void BuildMatrices()
        {
            provider.BuildMatrices();
        }

        public IVector GetOtherRhsComponents(ILinearSystem linearSystem, IVector currentSolution)
        {
            //TODO: use a ZeroVector class that avoid doing useless operations or refactor this method. E.g. let this method 
            // alter the child analyzer's rhs vector, instead of the opposite (which is currently done).
            return linearSystem.CreateZeroVector();
        }

        public void Initialize(bool isFirstAnalysis = true)
        {
            if (isFirstAnalysis)
            {
                // The order in which the next initializations happen is very important.
                //model.ConnectDataStructures(); //TODOMPI: This is done by the client, because it is necessary for the partition.
                solver.OrderDofs(false);
                solver.Initialize();
            }

            Action<int> initializeRhs = subdomainID =>
            {
                ILinearSystem linearSystem = linearSystems[subdomainID];
                linearSystem.Reset(); // Necessary to define the linear system's size 
                linearSystem.Subdomain.Forces = Vector.CreateZero(linearSystem.Size);
                linearSystem.RhsVector = linearSystem.Subdomain.Forces;
            };
            environment.DoPerNode(initializeRhs);

            //TODO: Perhaps this should be called by the child analyzer
            BuildMatrices(); 

            // Loads must be created after building the matrices.
            //TODO: Some loads may not have to be recalculated each time the stiffness changes.
            model.AssignLoads(solver.DistributeNodalLoads);

            ChildAnalyzer.Initialize(isFirstAnalysis);
        }

        public void Solve()
        {
            ChildAnalyzer.Solve();
        }
    }
}
