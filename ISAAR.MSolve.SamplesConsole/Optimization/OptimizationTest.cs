using ISAAR.MSolve.Analyzers.Optimization;
using ISAAR.MSolve.Analyzers.Optimization.Algorithms.Metaheuristics.DifferentialEvolution;
using ISAAR.MSolve.Analyzers.Optimization.Algorithms.Metaheuristics.GeneticAlgorithms;
using ISAAR.MSolve.Analyzers.Optimization.Algorithms.Metaheuristics.GeneticAlgorithms.Encodings;
using ISAAR.MSolve.Analyzers.Optimization.Algorithms.Metaheuristics.GeneticAlgorithms.Mutations;
using ISAAR.MSolve.Analyzers.Optimization.Algorithms.Metaheuristics.GeneticAlgorithms.Recombinations;
using ISAAR.MSolve.Analyzers.Optimization.Convergence;
using ISAAR.MSolve.Analyzers.Optimization.Output;
using ISAAR.MSolve.Analyzers.Optimization.Problems;
using ISAAR.MSolve.SamplesConsole.Optimization.BenchmarkFunctions;
using System;
using System.Linq;

namespace ISAAR.MSolve.SamplesConsole.Optimization
{
    public class OptimizationTest
    {
        public static void Main()
        {
            //TestDE();
            TestGA();

            Console.Write("\nEnter any key to exit: ");
            Console.Read();
        }

        private static void TestDE()
        {
            OptimizationProblem optimizationProblem = new Ackley();

            DifferentialEvolutionAlgorithm.Builder builder = new DifferentialEvolutionAlgorithm.Builder(optimizationProblem);
            builder.PopulationSize = 100;
            builder.MutationFactor = 0.4;
            builder.CrossoverProbability = 0.9;
            builder.ConvergenceCriterion = new MaxFunctionEvaluations(100000);

            //DifferentialEvolutionAlgorithm de = new DifferentialEvolutionAlgorithm(optimizationProblem);
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

        private static void TestGA()
        {
            // Define optim problem
            OptimizationProblem problem = new Ackley();

            // Define optim algorithm and parameters
            var optimBuilder = new BinaryGA.Builder(problem);
            optimBuilder.Logger = new BestOfIterationLogger();
            //optimBuilder.Logger = new EmptyLogger();
            optimBuilder.Terminator = new ConvergenceChecker();
            optimBuilder.Terminator.AddIndependentCriterion(new MaxIterations(200));
            optimBuilder.Encoding = new GrayCodes(problem, 16, 8);
            optimBuilder.PopulationSize = 100;
            optimBuilder.Recombination = new SinglePointCrossover();
            optimBuilder.Mutation = new BitFlipMutation(0.05);

            // Start optimization
            const int repetitions = 100;
            var solutions = new double[repetitions];
            for (int rep = 0; rep < repetitions; ++rep)
            {
                var optimAlgorithm = optimBuilder.BuildAlgorithm();
                optimAlgorithm.Solve();
                solutions[rep] = optimAlgorithm.BestFitness;
                Console.WriteLine("Best objective value: " + optimAlgorithm.BestFitness);
            }
            Console.WriteLine("Average objective value: " + solutions.Average());

            //Print results
            //Console.WriteLine("----------- History -----------");
            //optimBuilder.Logger.PrintToConsole();

            //Console.WriteLine("----------- Results -----------");
            //Console.WriteLine("Best objective value: " + optimAlgorithm.BestFitness);
            //Console.WriteLine("For continuous design variables: ");
            //PrintLineArray(optimAlgorithm.BestVariables.Item1);
            //Console.WriteLine("and integer design variables: ");
            //PrintLineArray(optimAlgorithm.BestVariables.Item2);
        }

    }
}
