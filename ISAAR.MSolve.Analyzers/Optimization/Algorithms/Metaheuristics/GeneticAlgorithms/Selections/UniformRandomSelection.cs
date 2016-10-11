using ISAAR.MSolve.Analyzers.Optimization.Commons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Troschuetz.Random;

namespace ISAAR.MSolve.Analyzers.Optimization.Algorithms.Metaheuristics.GeneticAlgorithms.Selections
{
    class UniformRandomSelection<T>: ISelectionStrategy<T>
    {
        private readonly bool allowIdenticalParents;
        private readonly IGenerator rng;

        public UniformRandomSelection(): this(RandomNumberGenerationUtilities.troschuetzRandom)
        {
        }

        public UniformRandomSelection(IGenerator randomNumberGenerator, bool allowIdenticalParents = false)
        {
            if (randomNumberGenerator == null) throw new ArgumentException("The random number generator must not be null");
            this.rng = randomNumberGenerator;
            this.allowIdenticalParents = allowIdenticalParents;
        }

        public Tuple<Individual<T>, Individual<T>>[] Apply(Individual<T>[] population, int offspringsCount)
        {
            int pairsCount = (offspringsCount - 1) / 2 + 1;
            var pairs = new Tuple<Individual<T>, Individual<T>>[pairsCount];
            for (int i = 0; i < pairsCount; ++i)
            {
                int parent1 = rng.Next(population.Length);
                int parent2 = rng.Next(population.Length);
                if (!allowIdenticalParents)
                {
                    while (parent1 == parent2) parent2 = rng.Next(population.Length);
                }
                pairs[i] = new Tuple<Individual<T>, Individual<T>>(population[parent1], population[parent2]);
            }
            return pairs;
        }
    }
}
