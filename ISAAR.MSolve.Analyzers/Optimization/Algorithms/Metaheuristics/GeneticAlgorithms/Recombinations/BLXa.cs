using ISAAR.MSolve.Analyzers.Optimization.Commons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Troschuetz.Random;

namespace ISAAR.MSolve.Analyzers.Optimization.Algorithms.Metaheuristics.GeneticAlgorithms.Recombinations
{
    
    public class BLXa: IRecombinationStrategy<double>
    {
        private readonly IGenerator rng;
        private readonly double alpha;

        public BLXa(double alpha = 0.5) : 
            this(alpha, RandomNumberGenerationUtilities.troschuetzRandom) { }

        public BLXa(double alpha, IGenerator randomNumberGenerator)
        {
            // TODO: find min, max alpha
            //if (true)
            //{
            //    throw new ArgumentException("The alpha parameter must belong to the interval [], but was " + alpha);
            //}
            this.alpha = alpha;

            if (randomNumberGenerator == null) throw new ArgumentException("The random number generator must not be null");
            this.rng = randomNumberGenerator;
        }

        public Individual<double>[] Apply(Tuple<Individual<double>, Individual<double>>[] parents, int offspringsCount)
        {
            // TODO: BLX-a does not necessarily create 2 offsprings, like e.g. single point crossover. 
            // There needs to be a way for the recombination strategy to request as many parent pairs/triads/etc it needs 
            // from the selection strategy
            var offsprings = new Individual<double>[offspringsCount];
            double[] offspring1, offspring2;
            for (int i = 0; i < parents.Length; ++i)
            {
                Blend(parents[i].Item1.Chromosome, parents[i].Item2.Chromosome, out offspring1, out offspring2);
                offsprings[2 * i] = new Individual<double>(offspring1);
                if (2 * i + 1 < offspringsCount) offsprings[2 * i + 1] = new Individual<double>(offspring2);
            }
            return offsprings;
        }

        private void Blend(double[] parent1, double[] parent2, out double[] offspring1, out double[] offspring2)
        {
            int genesCount = parent1.Length; //No checking for the other chromosomes
            offspring1 = new double[genesCount];
            offspring2 = new double[genesCount];

            for (int gene = 0; gene < genesCount; ++gene)
            {
                double range = Math.Abs(parent1[gene] - parent2[gene]);
                double min = Math.Min(parent1[gene], parent2[gene]) - alpha * range;
                double max = Math.Max(parent1[gene], parent2[gene]) + alpha * range;
                offspring1[gene] = rng.NextDouble(min, max);
                offspring2[gene] = rng.NextDouble(min, max);
            }
        }
    }
}
