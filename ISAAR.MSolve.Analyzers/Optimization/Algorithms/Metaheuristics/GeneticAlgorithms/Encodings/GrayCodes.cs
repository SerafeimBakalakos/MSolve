using ISAAR.MSolve.Analyzers.Optimization.Commons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Troschuetz.Random;

namespace ISAAR.MSolve.Analyzers.Optimization.Algorithms.Metaheuristics.GeneticAlgorithms.Encodings
{
    public class GrayCodes : IEncoding
    {
        private static readonly IGenerator rng = RandomNumberGenerationUtilities.troschuetzRandom;
        private readonly int continuousVariablesCount;
        private readonly int bitsPerContinuousVariable;
        private readonly double[] continuousLowerBounds;
        private readonly double[] continuousUpperBounds;
        private readonly int integerVariablesCount;
        private readonly int bitsPerIntegerVariable;
        //private readonly int[] integerLowerBounds; They are 0
        private readonly int[] integerUpperBounds;

        // TODO: most of these should be contained in an OptimProblem DTO
        public GrayCodes(int continuousVariablesCount, double[] continuousLowerBounds, double[] continuousUpperBounds,
                              int integerVariablesCount, int[] integerUpperBounds,
                              int bitsPerContinuousVariable, int bitsPerIntegerVariable)
        {
            this.continuousVariablesCount = continuousVariablesCount;
            this.continuousLowerBounds = continuousLowerBounds;
            this.continuousUpperBounds = continuousUpperBounds;
            this.integerVariablesCount = integerVariablesCount;
            this.integerUpperBounds = integerUpperBounds;
            this.bitsPerContinuousVariable = bitsPerContinuousVariable;
            this.bitsPerIntegerVariable = bitsPerIntegerVariable;
        }

        public bool[] CreateRandomGenotype()
        {
            bool[] chromosome = new bool[continuousVariablesCount * bitsPerContinuousVariable +
                                           integerVariablesCount * bitsPerIntegerVariable];
            for (int i = 0; i < chromosome.LongLength; ++i)
            {
                if (rng.NextBoolean()) chromosome[i] = true;
            }
            return chromosome;
        }

        public double[] Phenotype(bool[] genotype)
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
