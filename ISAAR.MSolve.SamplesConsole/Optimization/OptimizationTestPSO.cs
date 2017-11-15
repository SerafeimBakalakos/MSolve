using ISAAR.MSolve.Numerical.Optimization;
using ISAAR.MSolve.Numerical.Optimization.Algorithms.Metaheuristics.ParticleSwarmOptimization;
using ISAAR.MSolve.Numerical.Optimization.Convergence;
using ISAAR.MSolve.Numerical.Optimization.Logging;
using ISAAR.MSolve.Numerical.Optimization.Problem;
using System;

namespace ISAAR.MSolve.SamplesConsole.Optimization.BenchmarkFunctions
{
    class OptimizationTestPSO
    {
        public static void Run()
        {
            OptimizationProblem optimizationProblem = new Rosenbrock();

            ParticleSwarmOptimizationAlgorithm.Builder builder = new ParticleSwarmOptimizationAlgorithm.Builder(optimizationProblem);

            builder.SwarmSize = 50;
            builder.PhiP = 2.0;
            builder.PhiG = 2.0;
            builder.ConvergenceCriterion = new MaxFunctionEvaluations((int)10E3);
            builder.Logger = new NoLogger();

            IOptimizationAlgorithm pso = builder.Build();
            IOptimizationAnalyzer analyzer = new OptimizationAnalyzer(pso);
            analyzer.Optimize();

            // Print results
            Console.WriteLine("\n Best Position:");
            for (int i = 0; i < optimizationProblem.Dimension; i++)
            {
                Console.WriteLine(String.Format(@"  x[{0}] = {1} ", i, pso.BestPosition[i]));
            }
            Console.WriteLine(String.Format(@"Best Fitness: {0}", pso.BestFitness));

            Console.Write("\nEnter any key to exit: ");
            Console.Read();
        }
    }
}
