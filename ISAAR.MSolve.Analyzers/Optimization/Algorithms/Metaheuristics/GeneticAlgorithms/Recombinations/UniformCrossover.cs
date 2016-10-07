using ISAAR.MSolve.Analyzers.Optimization.Commons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Troschuetz.Random;

namespace ISAAR.MSolve.Analyzers.Optimization.Algorithms.Metaheuristics.GeneticAlgorithms.Recombinations
{
    public class UniformCrossover : IRecombinationStrategy
    {
        private readonly IGenerator rng;

        public UniformCrossover() : this(RandomNumberGenerationUtilities.troschuetzRandom) { }

        public UniformCrossover(IGenerator randomNumberGenerator)
        {
            if (randomNumberGenerator == null) throw new ArgumentException("The random number generator must not be null");
            this.rng = randomNumberGenerator;
        }

        Individual[] IRecombinationStrategy.Apply(Tuple<Individual, Individual>[] parents, int offspringsCount)
        {
            var offsprings = new Individual[offspringsCount];
            bool[] offspring1, offspring2;
            for (int i = 0; i < parents.Length; ++i)
            {
                Crossover(parents[i].Item1.Chromosome, parents[i].Item2.Chromosome, out offspring1, out offspring2);
                offsprings[2 * i] = new Individual(offspring1);
                if (2 * i + 1 < offspringsCount) offsprings[2 * i + 1] = new Individual(offspring2);
            }
            return offsprings;
        }

        private void Crossover(bool[] parent1, bool[] parent2,
                                                out bool[] offspring1, out bool[] offspring2)
        {
            int genesCount = parent1.Length; //No checking for the other chromosomes
            offspring1 = new bool[genesCount];
            offspring2 = new bool[genesCount];
            for (int gene = 0; gene < genesCount; ++gene)
            {
                if (rng.NextBoolean()) // true => crossover
                {
                    offspring1[gene] = parent2[gene];
                    offspring2[gene] = parent1[gene];
                }
                else // false => inherit from corresponding parent
                {
                    offspring1[gene] = parent1[gene];
                    offspring2[gene] = parent2[gene];
                }
            }
        }
    }
}
