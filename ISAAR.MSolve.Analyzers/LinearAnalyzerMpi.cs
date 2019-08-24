﻿using System;
using System.Collections.Generic;
using ISAAR.MSolve.Analyzers.Interfaces;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Logging.Interfaces;
using ISAAR.MSolve.Solvers;
using ISAAR.MSolve.Solvers.LinearSystems;

namespace ISAAR.MSolve.Analyzers
{
    public class LinearAnalyzerMpi : IChildAnalyzer
    {
        private readonly IModel model;
        private readonly IAnalyzerProvider provider;
        private readonly ISolverMpi solver;

        public LinearAnalyzerMpi(IModel model, ISolverMpi solver, IAnalyzerProvider provider)
        {
            this.model = model;
            this.solver = solver;
            this.provider = provider;
        }

        public ILogFactory LogFactory { get; set; }
        public IAnalyzerLog[] Logs { get; set; }

        public IParentAnalyzer ParentAnalyzer { get; set; }

        Dictionary<int, IAnalyzerLog[]> IAnalyzer.Logs
        {
            get
            {
                //TODO: I should probably gather logs from processes here.
                throw new NotImplementedException();
            }
        }

        public void BuildMatrices()
        {
            if (ParentAnalyzer == null) throw new InvalidOperationException("This linear analyzer has no parent.");

            ParentAnalyzer.BuildMatrices();
            //solver.Initialize();
        }

        public void Initialize(bool isFirstAnalysis)
        {
            //InitializeLogs();
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

        private void AddEquivalentNodalLoadsToRHS() //TODO: This must be distributed
        {
            provider.DirichletLoadsAssembler.ApplyEquivalentNodalLoads(
                solver.LinearSystem.Subdomain, solver.LinearSystem.RhsVector);
        }

        private void InitializeLogs()
        {
            Logs = LogFactory.CreateLogs();
        }

        private void StoreLogResults(DateTime start, DateTime end)
        {
            foreach (IAnalyzerLog log in Logs) log.StoreResults(start, end, solver.LinearSystem.Solution);
        }
    }
}
