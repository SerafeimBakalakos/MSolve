using ISAAR.MSolve.Analyzers.Optimization.Commons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Troschuetz.Random;

namespace ISAAR.MSolve.Analyzers.Optimization.Algorithms.Metaheuristics.GeneticAlgorithms.Selections
{
    // TODO: There is a variant that subtracts the max fitness of this generation from all fitnesses before calculating the probabbilities
    class RouletteWheelSelection<T> : ISelectionStrategy<T>
    {
        private readonly IGenerator rng;

        public RouletteWheelSelection(): this(RandomNumberGenerationUtilities.troschuetzRandom)
        {
        }

        public RouletteWheelSelection(IGenerator randomNumberGenerator)
        {
            if (randomNumberGenerator == null) throw new ArgumentException("The random number generator must not be null");
            this.rng = randomNumberGenerator;
        }

        public Individual<T>[][] Apply(Individual<T>[] population, int parentGroupsCount,
                                       int parentsPerGroup, bool allowIdenticalParents)
        {
            Array.Sort(population); // may already be sorted from elitism. TODO: add a population class to query if it is sorted. 
            double[] probabilities = Probabilities(population, population.Length);

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

        private double[] Probabilities(Individual<T>[] population, int populationSize)
        {
            double[] probabilities = new double[populationSize];
            double sumFitness = 0.0;
            for (int i = 0; i < populationSize; ++i) sumFitness += population[i].Fitness;
            for (int i = 0; i < populationSize; ++i) probabilities[i] = population[i].Fitness / sumFitness;
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
                if (rand < probabilities[i]) return i ;
            }
            throw new ArgumentException("The provided cummulative probabilities must span the whole interval [0,1]");
        }
    }
}
