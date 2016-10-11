using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

    namespace ISAAR.MSolve.Analyzers.Optimization.Logging
{
    public class BestOfIterationLogger: IOptimizationLogger
    {
        private List<double[]> bestContinuousVariables;
        private List<int[]> bestIntegerVariables;
        private List<double> bestObjectives;

        public BestOfIterationLogger()
        {
            bestContinuousVariables = new List<double[]>();
            bestIntegerVariables = new List<int[]>();
            bestObjectives = new List<double>();
        }

        public void Log(IOptimizationAlgorithm algorithm)
        {
            bestContinuousVariables.Add(algorithm.BestPosition);
            //bestIntegerVariables.Add();
            bestObjectives.Add(algorithm.BestFitness);
        }

        public void PrintToConsole()
        {
            Console.Write("Initialization: ");
            PrintEntryToConsole(0);
            for (int iteration = 0; iteration < bestObjectives.Count-1; ++iteration)
            {
                Console.Write("Iteration " + iteration + ": ");
                PrintEntryToConsole(iteration + 1);
            }
        }

        private void PrintEntryToConsole(int index)
        {
            Console.Write("continuous variables = " + ArrayToString<double>(bestContinuousVariables[index]));
            Console.Write(" , integer variables = " + ArrayToString<int>(bestIntegerVariables[index]));
            Console.WriteLine(" , objective value = " + bestObjectives[index]);
        }

        private static string ArrayToString<T>(T[] array)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("{ ");
            foreach (var entry in array)
            {
                builder.Append(entry);
                builder.Append(' ');
            }
            builder.Append("}");
            return builder.ToString();
        }
        
    }
}
