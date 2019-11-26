using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.LinearAlgebra.Distributed;
using ISAAR.MSolve.LinearAlgebra.Reordering;
using ISAAR.MSolve.Logging.DomainDecomposition;
using ISAAR.MSolve.Solvers;
using ISAAR.MSolve.Solvers.Direct;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.Feti1;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.Feti1.InterfaceProblem;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.Feti1.Matrices;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.CornerNodes;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.InterfaceProblem;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.StiffnessMatrices;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.Pcg;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.Preconditioning;
using ISAAR.MSolve.Solvers.Ordering.Reordering;
using ISAAR.MSolve.XFEM.Analyzers;
using ISAAR.MSolve.XFEM.Entities;
using ISAAR.MSolve.XFEM.Solvers;
using ISAAR.MSolve.XFEM.Tests;
using MPI;

namespace ISAAR.MSolve.XFEM.Tests.Paper1
{
    public class DoubleCantileverBeamMpi
    {
        private const int numElementsY = 15;
        private const double tipEnrichementRadius = 0.0;
        //private const string crackPlotDirectory = @"C:\Users\Serafeim\Desktop\COMPDYN2019\DCB\Plots\LSM";
        //private const string subdomainPlotDirectory = @"C:\Users\Serafeim\Desktop\COMPDYN2019\DCB\Plots\Subdomains";
        private const string solverLogPath = @"C:\Users\Serafeim\Desktop\COMPDYN2019\DCB\solver_log.txt";

        public static void Run(string[] args)
        {
            using (new MPI.Environment(ref args))
            {
                int numSubdomainsY = 3;
                int numSubdomainsX = 3 * numSubdomainsY;

                int master = 0;
                //int[] processesToSubdomains = Enumerable.Range(0, numSubdomainsY * numSubdomainsX).ToArray();
                int[][] processesToSubdomains = new int[numSubdomainsY * numSubdomainsX][];
                for (int p = 0; p < numSubdomainsY * numSubdomainsX; ++p) processesToSubdomains[p] = new int[] { p };
                var procs = new ProcessDistribution(Communicator.world, master, processesToSubdomains);

                DcbBenchmarkBelytschkoMpi benchmark = CreateBenchmark(procs, numElementsY, numSubdomainsX, numSubdomainsY,
                    tipEnrichementRadius);
                ISolverMpi solver = DefineSolver(procs, benchmark);
                RunCrackPropagationAnalysis(procs, benchmark, solver);
            }
        }

        private static DcbBenchmarkBelytschkoMpi CreateBenchmark(ProcessDistribution procs, int numElementsY, int numSubdomainsX,
            int numSubdomainsY, double tipEnrichmentRadius)
        {
            var builder = new DcbBenchmarkBelytschkoMpi.Builder(procs, numElementsY, numSubdomainsX, numSubdomainsY);
            //builder.LsmPlotDirectory = crackPlotDirectory;
            //builder.SubdomainPlotDirectory = subdomainPlotDirectory;
            builder.HeavisideEnrichmentTolerance = 0.001;
            builder.MaxIterations = 8;
            builder.TipEnrichmentRadius = tipEnrichmentRadius;
            builder.JintegralRadiusOverElementSize = 2.0;

            DcbBenchmarkBelytschkoMpi benchmark = builder.BuildBenchmark();
            benchmark.InitializeModel();
            return benchmark;
        }

        private static ISolverMpi DefineSolver(ProcessDistribution procs, DcbBenchmarkBelytschkoMpi benchmark)
        {
            benchmark.Partitioner = new TipAdaptivePartitioner(benchmark.Crack);
            Func<ISubdomain, HashSet<INode>> getInitialCorners = sub => new HashSet<INode>(
                    CornerNodeUtilities.FindCornersOfRectangle2D(sub).Where(node => node.Constraints.Count == 0));
            var cornerNodeSelection = new CrackedFetiDPCornerNodesMpi(procs, benchmark.Model, benchmark.Crack, getInitialCorners);
            //var reordering = new OrderingAmdCSparseNet();  // This is slower than natural ordering
            IReorderingAlgorithm reordering = null;
            var fetiMatrices = new FetiDPMatrixManagerFactorySkyline(reordering);
            var builder = new FetiDPSolverMpi.Builder(procs, fetiMatrices);
            //builder.Preconditioning = new LumpedPreconditioning();
            //builder.Preconditioning = new DiagonalDirichletPreconditioning();
            builder.Preconditioning = new DirichletPreconditioning();
            builder.ProblemIsHomogeneous = true;
            builder.PcgSettings = new PcgSettings() { ConvergenceTolerance = 1E-7 };
            return builder.Build(benchmark.Model, cornerNodeSelection);
        }

        private static void RunCrackPropagationAnalysis(ProcessDistribution procs, DcbBenchmarkBelytschkoMpi benchmark,
            ISolverMpi solver)
        {
            TipAdaptivePartitioner partitioner = null;
            partitioner = new TipAdaptivePartitioner(benchmark.Crack);
            var analyzer = new QuasiStaticCrackPropagationAnalyzerMpi(procs, benchmark.Model, solver, benchmark.Crack,
                benchmark.FractureToughness, benchmark.MaxIterations, partitioner);

            analyzer.Initialize();
            analyzer.Analyze();

            if (procs.IsMasterProcess)
            {
                // Write crack path
                Console.WriteLine("Crack path:");
                foreach (var point in benchmark.Crack.CrackPath)
                {
                    Console.WriteLine($"{point.X} {point.Y}");
                }
                Console.WriteLine();

                solver.Logger.WriteToFile(solverLogPath, $"{solver.Name}_log", true);
                solver.Logger.WriteAggregatesToFile(solverLogPath, $"{solver.Name}_log", true);
            }
        }
    }
}
