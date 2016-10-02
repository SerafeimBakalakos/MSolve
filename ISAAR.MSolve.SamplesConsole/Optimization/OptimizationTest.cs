using ISAAR.MSolve.Analyzers.Optimization;
using ISAAR.MSolve.Analyzers.Optimization.Algorithms;
using ISAAR.MSolve.SamplesConsole.Optimization.BenchmarkFunctions;
using System;

namespace ISAAR.MSolve.SamplesConsole.Optimization
{
    public class OptimizationTest
    {
        public static void Main()
        {
            //IObjectiveFunction objective = new Ackley();
            //double[] lowerBounds = { -5, -5 };
            //double[] upperBounds = {  5,  5 };

            //IObjectiveFunction objective = new Beale();
            //double[] lowerBounds = { -4.5, -4.5 };
            //double[] upperBounds = { 4.5, 4.5 };

            //IObjectiveFunction objective = new GoldsteinPrice();
            //double[] lowerBounds = {-2, -2 };
            //double[] upperBounds = { 2,  2 };

            IObjectiveFunction objective = new McCormick();
            double[] lowerBounds = { -1.5, -3.0 };
            double[] upperBounds = {  4.0,  4.0 };

            DifferentialEvolution de = new DifferentialEvolution(lowerBounds.Length, lowerBounds, upperBounds, objective);
            IOptimizationAnalyzer analyzer = new OptimizationAnalyzer(de);
            analyzer.Optimize();

            // Print results
            Console.WriteLine("\n Best Position:");
            for (int i = 0; i < lowerBounds.Length; i++)
            {
                Console.WriteLine(String.Format(@"  x[{0}] = {1} ", i, de.BestPosition[i]));
            }
            Console.WriteLine(String.Format(@"Best Fitness: {0}", de.BestFitness));
        }
    }
}
