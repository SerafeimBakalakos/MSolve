using ISAAR.MSolve.Analyzers.Optimization.Commons;
using ISAAR.MSolve.Analyzers.Optimization.Problems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Troschuetz.Random;

namespace ISAAR.MSolve.Analyzers.Optimization.Algorithms.Metaheuristics.GeneticAlgorithms.Encodings
{
    public class GrayCodeEncoding : AbstractBinaryEncoding
    {
        public GrayCodeEncoding(OptimizationProblem problem, int bitsPerContinuousVariable, int bitsPerIntegerVariable):
                        base(problem, bitsPerContinuousVariable, bitsPerIntegerVariable)
        {
        }

        public override sealed double[] ComputePhenotype(bool[] genotype)
        {
            // Continuous variables
            double[] continuousVariables = new double[continuousVariablesCount];
            for (int i = 0; i < continuousVariablesCount; ++i)
            {
                int start = i * bitsPerContinuousVariable;
                int deci = BinaryUtilities.GrayCodeToDecimal(genotype, start, bitsPerContinuousVariable);
                double normalized = deci / (Math.Round(Math.Pow(2, bitsPerContinuousVariable)) - 1);
                continuousVariables[i] = continuousLowerBounds[i] +
                                         normalized * (continuousUpperBounds[i] - continuousLowerBounds[i]);
            }
            return continuousVariables;
        }

        public int[] IntegerPhenotype(bool[] genotype)
        {
            // Integer Variables
            int[] integerVariables = new int[integerVariablesCount];
            int offset = continuousVariablesCount * bitsPerContinuousVariable;
            for (int i = 0; i < integerVariablesCount; ++i)
            {
                int start = offset + i * bitsPerIntegerVariable;
                integerVariables[i] = BinaryUtilities.GrayCodeToDecimal(genotype, start, bitsPerIntegerVariable);
            }
            return integerVariables;
        }
    }
}
