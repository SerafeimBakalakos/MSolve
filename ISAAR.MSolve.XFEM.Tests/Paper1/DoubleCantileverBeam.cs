using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;
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
using static ISAAR.MSolve.XFEM.Tests.COMPDYN2019.Utilities;

namespace ISAAR.MSolve.XFEM.Tests.Paper1
{
    public class DoubleCantileverBeam
    {
        private const int numElementsY = 15;
        private const double tipEnrichementRadius = 0.0;
        private const string crackPlotDirectory = @"C:\Users\Serafeim\Desktop\COMPDYN2019\DCB\Plots\LSM";
        private const string subdomainPlotDirectory = @"C:\Users\Serafeim\Desktop\COMPDYN2019\DCB\Plots\Subdomains";
        private const string solverLogPath = @"C:\Users\Serafeim\Desktop\COMPDYN2019\DCB\solver_log.txt";

        public static void Run()
        {
            int numSubdomainsY = 3;
            int numSubdomainsX = numSubdomainsY;
            var solverType = SolverType.FetiDP;

            DcbBenchmarkBelytschko benchmark = CreateBenchmark(numElementsY, numSubdomainsX, numSubdomainsY, tipEnrichementRadius);
            ISolverMpi solver = DefineSolver(benchmark, solverType);
            RunCrackPropagationAnalysis(benchmark, solver);

            Console.Write("\nEnd");
        }

        private static DcbBenchmarkBelytschko CreateBenchmark(int numElementsY, int numSubdomainsX, int numSubdomainsY, double tipEnrichmentRadius)
        {
            var builder = new DcbBenchmarkBelytschko.Builder(numElementsY, numSubdomainsX, numSubdomainsY);
            builder.LsmPlotDirectory = crackPlotDirectory;
            builder.SubdomainPlotDirectory = subdomainPlotDirectory;
            builder.HeavisideEnrichmentTolerance = 0.001;
            builder.MaxIterations = 8;
            builder.TipEnrichmentRadius = tipEnrichmentRadius;

            // Usually should be in [1.5, 2.5). The J-integral radius must be large enough to at least include elements around
            // the element that contains the crack tip. However it must not be so large that an element intersected by the 
            // J-integral contour is containes the previous crack tip. Thus the J-integral radius must be sufficiently smaller
            // than the crack growth length.
            builder.JintegralRadiusOverElementSize = 2.0;

            DcbBenchmarkBelytschko benchmark = builder.BuildBenchmark();
            benchmark.InitializeModel();
            return benchmark;
        }

        private static ISolverMpi DefineSolver(DcbBenchmarkBelytschko benchmark, SolverType solverType)
        {
            if (solverType == SolverType.FetiDP)
            {
                //benchmark.Partitioner = new TipAdaptivePartitioner(benchmark.Crack);
                benchmark.Model.ConnectDataStructures();

                //Dictionary<ISubdomain, HashSet<INode>> initialCorners = FindCornerNodesFromRectangleCorners(benchmark.Model);
                Func<ISubdomain, HashSet<INode>> getInitialCorners = sub => new HashSet<INode>(
                    CornerNodeUtilities.FindCornersOfRectangle2D(sub).Where(node => node.Constraints.Count == 0));
                var cornerNodeSelection = new CrackedFetiDPCornerNodesSerial(benchmark.Model, benchmark.Crack, getInitialCorners);
                //var reordering = new OrderingAmdCSparseNet();  // This is slower than natural ordering
                IReorderingAlgorithm reordering = null;
                var fetiMatrices = new FetiDPMatrixManagerFactorySkyline(reordering);
                var builder = new FetiDPSolverSerial.Builder(fetiMatrices);
                //builder.Preconditioning = new LumpedPreconditioning();
                //builder.Preconditioning = new DiagonalDirichletPreconditioning();
                builder.Preconditioning = new DirichletPreconditioning();
                builder.ProblemIsHomogeneous = true;
                builder.PcgSettings = new PcgSettings() { ConvergenceTolerance = 1E-7 };
                return builder.Build(benchmark.Model, cornerNodeSelection);
            }
            else throw new ArgumentException("Invalid solver choice.");
        }

        private static void RunCrackPropagationAnalysis(DcbBenchmarkBelytschko benchmark, ISolverMpi solver)
        {
            var analyzer = new QuasiStaticCrackPropagationAnalyzerSerial(benchmark.Model, solver, benchmark.Crack, 
                benchmark.FractureToughness, benchmark.MaxIterations, benchmark.Partitioner);

            // Subdomain plots
            if (subdomainPlotDirectory != null)
            {
                if (solver is FetiDPSolverSerial fetiDP)
                {
                    analyzer.DDLogger = new DomainDecompositionLoggerFetiDP(fetiDP.CornerNodes, subdomainPlotDirectory, true);
                }
                else analyzer.DDLogger = new DomainDecompositionLogger(subdomainPlotDirectory);
            }

            analyzer.Initialize();
            analyzer.Analyze();

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
