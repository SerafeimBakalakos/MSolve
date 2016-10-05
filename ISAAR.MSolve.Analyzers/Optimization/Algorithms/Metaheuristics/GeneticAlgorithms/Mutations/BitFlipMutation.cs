using ISAAR.MSolve.Analyzers.Optimization.Commons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ISAAR.MSolve.Analyzers.Optimization.Algorithms.Metaheuristics.GeneticAlgorithms.Mutations
{
    public class BitFlipMutation : MutationStrategy
    {
        private readonly double mutationProbability;
        private readonly Random rng;

        public BitFlipMutation(double mutationProbability) : this(mutationProbability, RandomNumberGenerationUtilities.sysRandom) { }

        public BitFlipMutation(double mutationProbability, Random randomNumberGenerator)
        {
            if (mutationProbability < 0 || mutationProbability > 1)
            {
                throw new ArgumentException("The mutation probability of each gene must belong to the intrval [0,1], but was "
                                             + mutationProbability);
            }
            this.mutationProbability = mutationProbability;
            if (randomNumberGenerator == null)
            {
                throw new ArgumentException("The random number generator must not be null");
            }
            this.rng = randomNumberGenerator;
        }

        public void Apply(Individual[] population)
        {
            //CanonicalVersion(population);
            FastVersion(population);
        }

        // Running time = O(populationSize * chromosomeSize). Particularly slow for binary encoding, where chromosomeSize is large.
        private void CanonicalVersion(Individual[] population)
        {
            long genesCount = population[0].Chromosome.LongLength; //No checking for the other chromosomes
            foreach (var individual in population)
            {
                bool[] chromosome = individual.Chromosome;
                for (int gene = 0; gene < genesCount; ++gene)
                {
                    if (rng.NextDouble() < mutationProbability) chromosome[gene] = !chromosome[gene];
                }
            }
        }

        // Faster than canonical version, but I am not sure if it is valid statistically. 
        // The canonical version performs (populationSize * chromosomeSize * mutationProbability) mutations on average. 
        // In contrast this version always performs exactly as many mutations. 
        // Also it is possible for 2 or more mutations to occur on the same gene of the same chromosome, thus negating each other.
        private void FastVersion(Individual[] population)
        {
            long genesCount = population[0].Chromosome.LongLength; //No checking for the other chromosomes
            long totalMutations = (long)(population.LongLength * (mutationProbability * genesCount));
            for (long i = 0; i < totalMutations; ++i)
            {
                int individual = rng.Next(population.Length);
                long gene = rng.NextLong(genesCount);
                population[individual].Chromosome[gene] = !population[individual].Chromosome[gene];
            }
        }
    }
}
