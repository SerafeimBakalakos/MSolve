﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using ISAAR.MSolve.Logging;
using ISAAR.MSolve.Logging.Interfaces;
using ISAAR.MSolve.Analyzers.Interfaces;
using ISAAR.MSolve.Solvers.Interfaces;
using ISAAR.MSolve.Numerical.LinearAlgebra.Interfaces;
using ISAAR.MSolve.Numerical.LinearAlgebra;
using System.Collections;
using System.Linq;

namespace ISAAR.MSolve.Analyzers
{
    public class NewtonRaphsonNonLinearAnalyzer : IAnalyzer
    {
        private readonly ILinearSystem[] linearSystems;
        private readonly INonLinearSubdomainUpdater[] subdomainUpdaters;
        private readonly ISubdomainGlobalMapping[] mappings;
        private readonly int increments;
        private readonly int totalDOFs;
        private readonly int maxSteps = 120;
        private readonly int stepsForMatrixRebuild = 500;
        private readonly double tolerance = 1e-5;
        private double rhsNorm;
        private INonLinearParentAnalyzer parentAnalyzer = null;
        private readonly ISolver solver;
        private readonly INonLinearProvider provider;
        private readonly Dictionary<int, IVector> rhs = new Dictionary<int, IVector>();
        private readonly Dictionary<int, Vector> u = new Dictionary<int, Vector>();
        private readonly Dictionary<int, Vector> du = new Dictionary<int, Vector>();
        private readonly Dictionary<int, Vector> uPlusdu = new Dictionary<int, Vector>();
        private readonly Vector globalRHS;
        private readonly Dictionary<int, LinearAnalyzerLogFactory> logFactories = new Dictionary<int, LinearAnalyzerLogFactory>();
        private readonly Dictionary<int, IAnalyzerLog[]> logs = new Dictionary<int, IAnalyzerLog[]>();

        public NewtonRaphsonNonLinearAnalyzer(ISolver solver, ILinearSystem[] linearSystems, INonLinearSubdomainUpdater[] subdomainUpdaters, ISubdomainGlobalMapping[] mappings,
            INonLinearProvider provider, int increments, int totalDOFs, int maximumIteration=120, int iterationStepsForMatrixRebuild=500)
        {
            this.solver = solver;
            this.subdomainUpdaters = subdomainUpdaters;
            this.mappings = mappings;
            this.linearSystems = linearSystems;
            this.provider = provider;
            this.increments = increments;
            this.totalDOFs = totalDOFs;
            this.globalRHS = new Vector(totalDOFs);

	        maxSteps = maximumIteration;
	        stepsForMatrixRebuild=iterationStepsForMatrixRebuild;
	        
			InitializeInternalVectors();
        }

        private void InitializeLogs()
        {
            logs.Clear();
            foreach (int id in logFactories.Keys) logs.Add(id, logFactories[id].CreateLogs());
        }

        private void StoreLogResults(DateTime start, DateTime end)
        {
            foreach (int id in logs.Keys)
                foreach (var l in logs[id])
                    l.StoreResults(start, end, linearSystems[id].Solution);
        }

        public Dictionary<int, LinearAnalyzerLogFactory> LogFactories { get { return logFactories; } }

        #region IAnalyzer Members

        public Dictionary<int, IAnalyzerLog[]> Logs { get { return logs; } }
        public IAnalyzer ParentAnalyzer
        {
            get { return (IAnalyzer)parentAnalyzer; }
            set { parentAnalyzer = (INonLinearParentAnalyzer)value; }
        }

        public IAnalyzer ChildAnalyzer
        {
            get { return null; }
            set { throw new InvalidOperationException("Newton-Raphson analyzer cannot contain an embedded analyzer."); }
        }

        public void InitializeInternalVectors()
        {
            globalRHS.Clear();
            rhs.Clear();
            u.Clear();
            du.Clear();
            uPlusdu.Clear();

            foreach (ILinearSystem subdomain in linearSystems)
            {
                Vector r = new Vector(subdomain.RHS.Length);
                ((Vector)subdomain.RHS).CopyTo(r.Data, 0);
                r.Multiply(1 / (double)increments);
                rhs.Add(subdomain.ID, r);
                u.Add(subdomain.ID, new Vector(subdomain.RHS.Length));
                du.Add(subdomain.ID, new Vector(subdomain.RHS.Length));
                uPlusdu.Add(subdomain.ID, new Vector(subdomain.RHS.Length));
                mappings[linearSystems.Select((v, i) => new { System = v, Index = i }).First(x => x.System.ID == subdomain.ID).Index].SubdomainToGlobalVector(((Vector)subdomain.RHS).Data, globalRHS.Data);
            }
            rhsNorm = provider.RHSNorm(globalRHS.Data);
        }

        private void UpdateInternalVectors()
        {
            globalRHS.Clear();
            foreach (ILinearSystem subdomain in linearSystems)
            {
                Vector r = new Vector(subdomain.RHS.Length);
                ((Vector)subdomain.RHS).CopyTo(r.Data, 0);
                r.Multiply(1 / (double)increments);
                rhs[subdomain.ID] = r;
                mappings[linearSystems.Select((v, i) => new { System = v, Index = i }).First(x => x.System.ID == subdomain.ID).Index].SubdomainToGlobalVector(((Vector)subdomain.RHS).Data, globalRHS.Data);
            }
            rhsNorm = provider.RHSNorm(globalRHS.Data);
        }

        public void Initialize()
        {
            solver.Initialize();
        }

        private void UpdateRHS(int step)
        {
            foreach (ILinearSystem subdomain in linearSystems)
            {
                Vector subdomainRHS = ((Vector)subdomain.RHS);
                rhs[subdomain.ID].CopyTo(subdomainRHS.Data, 0);
                subdomainRHS.Multiply(step + 1);
            }
        }

        public void Solve()
        {
            InitializeLogs();

            DateTime start = DateTime.Now;
            UpdateInternalVectors();
            for (int increment = 0; increment < increments; increment++)
            {
                double errorNorm = 0;
                ClearIncrementalSolutionVector();
                UpdateRHS(increment);

                double firstError = 0;
                int step = 0;
                for (step = 0; step < maxSteps; step++)
                {
                    solver.Solve();
                    errorNorm = rhsNorm != 0 ? CalculateInternalRHS(increment, step) / rhsNorm : 0;
                    if (step == 0) firstError = errorNorm;
                    if (errorNorm < tolerance) break;

                    SplitResidualForcesToSubdomains();
                    if ((step + 1) % stepsForMatrixRebuild == 0)
                    {
                        provider.Reset();
                        BuildMatrices();
                        solver.Initialize();
                    }
                }
                Debug.WriteLine("NR {0}, first error: {1}, exit error: {2}", step, firstError, errorNorm);
                SaveMaterialStateAndUpdateSolution();
            }
            CopySolutionToSubdomains();
//            ClearMaterialStresses();
            DateTime end = DateTime.Now;

            StoreLogResults(start, end);
        }

        private double CalculateInternalRHS(int currentIncrement, int step)
        {
            globalRHS.Clear();
            foreach (ILinearSystem subdomain in linearSystems)
            {
                if (currentIncrement == 0 && step == 0)
                {
                    Array.Clear(du[subdomain.ID].Data, 0, du[subdomain.ID].Length);
                    Array.Clear(uPlusdu[subdomain.ID].Data, 0, uPlusdu[subdomain.ID].Length);
                    du[subdomain.ID].Add(((Vector)subdomain.Solution));
                    uPlusdu[subdomain.ID].Add(((Vector)subdomain.Solution));
                    du[subdomain.ID].Subtract(u[subdomain.ID]);
                }
                else
                {
                    du[subdomain.ID].Add(((Vector)subdomain.Solution));
                    Array.Clear(uPlusdu[subdomain.ID].Data, 0, uPlusdu[subdomain.ID].Length);
                    uPlusdu[subdomain.ID].Add(u[subdomain.ID]);
                    uPlusdu[subdomain.ID].Add(du[subdomain.ID]);
                }
                //Vector<double> internalRHS = (Vector<double>)subdomain.GetRHSFromSolution(u[subdomain.ID], du[subdomain.ID]);
                Vector internalRHS = (Vector)subdomainUpdaters[linearSystems.Select((v, i) => new { System = v, Index = i }).First(x => x.System.ID == subdomain.ID).Index].GetRHSFromSolution(uPlusdu[subdomain.ID], du[subdomain.ID]);
                provider.ProcessInternalRHS(subdomain, internalRHS.Data, uPlusdu[subdomain.ID].Data);
                    //(new Vector<double>(u[subdomain.ID] + du[subdomain.ID])).Data);

                if (parentAnalyzer != null)
                    internalRHS.Add(new Vector(parentAnalyzer.GetOtherRHSComponents(subdomain,
                        uPlusdu[subdomain.ID])));
                //new Vector<double>(u[subdomain.ID] + du[subdomain.ID]))));
                
                Vector subdomainRHS = ((Vector)subdomain.RHS);
                subdomainRHS.Clear();
                for (int j = 0; j <= currentIncrement; j++) subdomainRHS.Add((Vector)rhs[subdomain.ID]);
                subdomainRHS.Subtract(internalRHS);
                mappings[linearSystems.Select((v, i) => new { System = v, Index = i }).First(x => x.System.ID == subdomain.ID).Index].SubdomainToGlobalVector(subdomainRHS.Data, globalRHS.Data);
            }
            double providerRHSNorm = provider.RHSNorm(globalRHS.Data);
            return providerRHSNorm;
        }

        private void ClearIncrementalSolutionVector()
        {
            foreach (ILinearSystem subdomain in linearSystems)
                du[subdomain.ID].Clear();
        }

        private void SplitResidualForcesToSubdomains()
        {
            foreach (ILinearSystem subdomain in linearSystems)
            {
                Vector subdomainRHS = ((Vector)subdomain.RHS);
                subdomainRHS.Clear();
                mappings[linearSystems.Select((v, i) => new { System = v, Index = i }).First(x => x.System.ID == subdomain.ID).Index].SplitGlobalVectorToSubdomain(globalRHS.Data, subdomainRHS.Data);
            }
        }

        private void SaveMaterialStateAndUpdateSolution()
        {
            foreach (ILinearSystem subdomain in linearSystems)
            {
                subdomainUpdaters[linearSystems.Select((v, i) => new { System = v, Index = i }).First(x => x.System.ID == subdomain.ID).Index].UpdateState();
                u[subdomain.ID].Add(du[subdomain.ID]);
            }
        }

        private void CopySolutionToSubdomains()
        {
            foreach (ILinearSystem subdomain in linearSystems)
                u[subdomain.ID].CopyTo(((Vector)subdomain.Solution).Data, 0);
        }

        private void ClearMaterialStresses()
        {
            foreach (ILinearSystem subdomain in linearSystems)
                subdomainUpdaters[linearSystems.Select((v, i) => new { System = v, Index = i }).First(x => x.System.ID == subdomain.ID).Index].ResetState();
        }

        public void BuildMatrices()
        {
            if (parentAnalyzer == null) 
                throw new InvalidOperationException("This Newton-Raphson non-linear analyzer has no parent.");

            parentAnalyzer.BuildMatrices();
            //solver.Initialize();
        }

        #endregion
    }
}
