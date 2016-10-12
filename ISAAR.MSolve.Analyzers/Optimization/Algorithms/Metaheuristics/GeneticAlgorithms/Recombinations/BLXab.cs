using ISAAR.MSolve.Analyzers.Optimization.Commons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Troschuetz.Random;

namespace ISAAR.MSolve.Analyzers.Optimization.Algorithms.Metaheuristics.GeneticAlgorithms.Recombinations
{
    public class BLXab : IRecombinationStrategy<double>
    {
        private readonly IGenerator rng;
        private readonly double alpha;
        private readonly double beta;

        public BLXab(double alpha = 0.75, double beta = 0.25) :
            this(alpha, beta, RandomNumberGenerationUtilities.troschuetzRandom)
        { }

        public BLXab(double alpha, double beta, IGenerator randomNumberGenerator)
        {
            // TODO: find min, max for alpha and beta
            if (alpha <= beta)
            {
                throw new ArgumentException("Parameter alpha = " + alpha + " must be greater than parameter beta = " + beta);
            }
            this.alpha = alpha;
            this.beta = beta;

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
                if (parents[i].Item1.Fitness <= parents[i].Item2.Fitness)
                {
                    Blend(parents[i].Item1.Chromosome, parents[i].Item2.Chromosome, out offspring1, out offspring2);
                }
                else
                {
                    Blend(parents[i].Item2.Chromosome, parents[i].Item1.Chromosome, out offspring1, out offspring2);
                }
                
                offsprings[2 * i] = new Individual<double>(offspring1);
                if (2 * i + 1 < offspringsCount) offsprings[2 * i + 1] = new Individual<double>(offspring2);
            }
            return offsprings;
        }

        private void Blend(double[] bestParent, double[] worstParent, out double[] offspring1, out double[] offspring2)
        {
            int genesCount = bestParent.Length; //No checking for the other chromosomes
            offspring1 = new double[genesCount];
            offspring2 = new double[genesCount];

            for (int gene = 0; gene < genesCount; ++gene)
            {
                double range = Math.Abs(bestParent[gene] - worstParent[gene]);
                if (bestParent[gene] <= worstParent[gene])
                {
                    double min = bestParent[gene] - alpha * range;
                    double max = worstParent[gene] + beta * range;
                    offspring1[gene] = rng.NextDouble(min, max);
                    offspring2[gene] = rng.NextDouble(min, max);
                }
                else
                {
                    double min = worstParent[gene] - beta * range;
                    double max = bestParent[gene] + alpha * range;
                    offspring1[gene] = rng.NextDouble(min, max);
                    offspring2[gene] = rng.NextDouble(min, max);
                }
            }
        }
    }
}
