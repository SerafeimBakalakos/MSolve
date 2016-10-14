using ISAAR.MSolve.Analyzers.Optimization.Commons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Troschuetz.Random;

namespace ISAAR.MSolve.Analyzers.Optimization.Algorithms.Metaheuristics.GeneticAlgorithms.Selections
{   
    class RankSelection<T> : ISelectionStrategy<T>
    {
        private readonly int rankExponent;
        private readonly IGenerator rng;

        public RankSelection(): this(RandomNumberGenerationUtilities.troschuetzRandom)
        {
        }

        public RankSelection(IGenerator randomNumberGenerator, int rankExponent = 1)
        {
            if (randomNumberGenerator == null) throw new ArgumentException("The random number generator must not be null");
            this.rng = randomNumberGenerator;

            if (rankExponent < 1) throw new ArgumentException("The rank exponent must be >= 1, but was " +rankExponent);
            this.rankExponent = rankExponent;
        }

        public Individual<T>[][] Apply(Individual<T>[] population, int parentGroupsCount, 
                                       int parentsPerGroup, bool allowIdenticalParents)
        {
            Array.Sort(population); // may already be sorted from elitism. TODO: add a population class to query if it is sorted. 
            double[] probabilities = Probabilities(population.Length);  // TODO: Have a double[] field and calculate in constructor to gain performance (requires const population size)

            var parentGroups = new Individual<T>[parentGroupsCount][];
            for (int group = 0; group < parentGroupsCount; ++group)
            {
                parentGroups[group] = new Individual<T>[parentsPerGroup];
                for (int parent = 0; parent < parentsPerGroup; ++parent)
                {
                    Individual<T> individual = population[RollWheel(probabilities)];
                    if (!allowIdenticalParents)
                    {
                        while (parentGroups[group].Contains<Individual<T>>(individual))
                        {
                            individual = population[RollWheel(probabilities)];
                        }
                    }
                    parentGroups[group][parent] = individual;
                }
            }
            return parentGroups;
        }

        private double[] Probabilities(int populationSize)
        {
            // General case
            if (rankExponent > 1) throw new NotImplementedException();
            //double sumRanks = 0.0;
            //for (int i = 1; i <= populationSize; ++i) sumRanks += Math.Pow(i, rankExponent);

            // Only for exponent = 1
            double[] probabilities = new double[populationSize];
            int sumRanks = (populationSize * (populationSize + 1)) / 2; // n*(n+1) is even
            for (int i = 1; i <= populationSize; ++i)
            {
                probabilities[i - 1] = (populationSize - i + 1) / sumRanks; // only for exponent = 1
            }
            for (int i = 1; i < populationSize; ++i) probabilities[i] += probabilities[i - 1];
            // Due to error accumulation the last addition will be 0.999... Can I get away by just setting it to 1.0?
            probabilities[populationSize - 1] = 1.0; 
            return probabilities;
        }

        private int RollWheel(double[] probabilities)
        {
            double rand = rng.NextDouble();
            for (int i = 0; i < probabilities.Length; ++i)
            {
                if (rand < probabilities[i]) return i;
            }
            throw new ArgumentException("The provided cummulative probabilities must span the whole interval [0,1]");
        }
    }
}
