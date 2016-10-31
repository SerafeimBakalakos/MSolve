using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ISAAR.MSolve.Analyzers.Optimization;
using ISAAR.MSolve.Analyzers.Optimization.Algorithms.Metaheuristics.DifferentialEvolution;
using ISAAR.MSolve.Analyzers.Optimization.Constraints.Penalties;
using ISAAR.MSolve.Analyzers.Optimization.Convergence;
using ISAAR.MSolve.Analyzers.Optimization.Problem;
using ISAAR.MSolve.SamplesConsole.Optimization.BenchmarkFunctions.Constrained;

namespace ISAAR.MSolve.SamplesConsole.Optimization
{
    public class OptimizationTestDEConstrained
    {
        public static void Run()
        {
            OptimizationProblem optimizationProblem = new S_CRES();

            DifferentialEvolutionAlgorithmConstrained.Builder builder = 
                new DifferentialEvolutionAlgorithmConstrained.Builder(optimizationProblem);
            builder.PopulationSize = 20;
            builder.MutationFactor = 0.6;
            builder.CrossoverProbability = 0.9;
            builder.ConvergenceCriterion = new MaxFunctionEvaluations(100000);
            builder.Penalty = new DeathPenalty(optimizationProblem.InequalityConstraints);
            IOptimizationAlgorithm de = builder.Build();

            IOptimizationAnalyzer analyzer = new OptimizationAnalyzer(de);
            analyzer.Optimize();

            // Print results
            Console.WriteLine("\n Best Position:");
            for (int i = 0; i < optimizationProblem.Dimension; i++)
            {
                Console.WriteLine(String.Format(@"  x[{0}] = {1} ", i, de.BestPosition[i]));
            }
            Console.WriteLine(String.Format(@"Best Fitness: {0}", de.BestFitness));
        }
    }
}
