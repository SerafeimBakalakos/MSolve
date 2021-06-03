using System;
using System.Collections.Generic;
using MGroup.Analyzers.Interfaces;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Logging.Interfaces;
using MGroup.Solvers;
using MGroup.Solvers.LinearSystems;
using MGroup.Environments;

namespace MGroup.Analyzers
{
    public class LinearAnalyzer : IChildAnalyzer
    {
        private readonly IReadOnlyDictionary<int, ILinearSystem> linearSystems;
        private readonly IComputeEnvironment environment;
        private readonly IStructuralModel model;
        private readonly IAnalyzerProvider provider;
        private readonly ISolver solver;

        public LinearAnalyzer(IComputeEnvironment environment, IStructuralModel model, ISolver solver, 
            IAnalyzerProvider provider)
        {
            this.environment = environment;
            this.model = model;
            this.solver = solver;
            this.linearSystems = solver.LinearSystems;
            this.provider = provider;
        }

        public IParentAnalyzer ParentAnalyzer { get; set; }

        public void BuildMatrices()
        {
            if (ParentAnalyzer == null) throw new InvalidOperationException("This linear analyzer has no parent.");

            ParentAnalyzer.BuildMatrices();
        }

        public void Initialize(bool isFirstAnalysis)
        {
            InitializeLogs();
            //solver.Initialize(); //TODO: Using this needs refactoring
        }

        public void Solve()
        {
            DateTime start = DateTime.Now;
            AddEquivalentNodalLoadsToRHS(); //TODO: The initial rhs (from other loads) should also be built by the analyzer instead of the model.
            solver.Solve();
            DateTime end = DateTime.Now;
            StoreLogResults(start, end);
        }

        private void AddEquivalentNodalLoadsToRHS()
        {
            Action<int> processSubdomainRhs = subdomainID =>
            {
                ILinearSystem linearSystem = linearSystems[subdomainID];
                try
                {
                    // Make sure there is at least one non zero prescribed displacement.
                    (INode node, IDofType dof, double displacement) = linearSystem.Subdomain.Constraints.Find(du => du != 0.0);

                    //TODO: the following 2 lines are meaningless outside diplacement control (and even then, they are not so clear).
                    double scalingFactor = 1;
                    IVector initialFreeSolution = linearSystem.CreateZeroVector();

                    IVector equivalentNodalLoads = provider.DirichletLoadsAssembler.GetEquivalentNodalLoads(
                        linearSystem.Subdomain, initialFreeSolution, scalingFactor);
                    linearSystem.RhsVector.SubtractIntoThis(equivalentNodalLoads);
                }
                catch (KeyNotFoundException)
                {
                    // There aren't any non zero prescribed displacements, therefore we do not have to calculate the equivalent 
                    // nodal loads, which is an expensive operation (all elements are accessed, their stiffness is built, etc..)
                }
            };
            environment.DoPerNode(processSubdomainRhs);
        }

        private void InitializeLogs()
        {
        }

        private void StoreLogResults(DateTime start, DateTime end)
        {
        }
    }
}
