using ISAAR.MSolve.Analyzers.Optimization.Commons;
using ISAAR.MSolve.Analyzers.Optimization.Problems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Troschuetz.Random;

namespace ISAAR.MSolve.Analyzers.Optimization.Algorithms.Metaheuristics.GeneticAlgorithms.Encodings
{
    public abstract class AbstractBinaryEncoding: IEncoding<bool>
    {
        #region fields and properties
        // Random number generation
        protected readonly IGenerator rng = RandomNumberGenerationUtilities.troschuetzRandom;

        // Continuous variables
        protected readonly int continuousVariablesCount;
        protected readonly double[] continuousLowerBounds;
        protected readonly double[] continuousUpperBounds;
        protected readonly int bitsPerContinuousVariable;

        // Integer variables
        protected readonly int integerVariablesCount;
        //private readonly int[] integerLowerBounds; They are 0
        protected readonly int[] integerUpperBounds;
        protected readonly int bitsPerIntegerVariable;
        #endregion

        #region constructor
        protected AbstractBinaryEncoding(OptimizationProblem problem, int bitsPerContinuousVariable, int bitsPerIntegerVariable)
        {
            this.continuousVariablesCount = problem.Dimension;
            this.continuousLowerBounds = problem.LowerBound;
            this.continuousUpperBounds = problem.UpperBound;
            this.bitsPerContinuousVariable = bitsPerContinuousVariable;
            this.integerVariablesCount = 0;
            this.integerUpperBounds = null;
            this.bitsPerIntegerVariable = 0;
        }
        #endregion

        #region IEncoding implementations
        public virtual bool[] CreateRandomGenotype()
        {
            bool[] chromosome = new bool[continuousVariablesCount * bitsPerContinuousVariable +
                                           integerVariablesCount * bitsPerIntegerVariable];
            for (int i = 0; i < chromosome.LongLength; ++i)
            {
                if (rng.NextBoolean()) chromosome[i] = true;
            }
            return chromosome;
        }

        public abstract double[] ComputePhenotype(bool[] genotype);
        #endregion
    }
}
