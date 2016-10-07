using ISAAR.MSolve.Analyzers.Optimization.Commons;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Troschuetz.Random;

namespace ISAAR.MSolve.Analyzers.Optimization.Algorithms.Metaheuristics.GeneticAlgorithms.Recombinations
{
    public class SinglePointCrossover : IRecombinationStrategy
    {
        private readonly IGenerator rng;

        public SinglePointCrossover() : this(RandomNumberGenerationUtilities.troschuetzRandom) { }

        public SinglePointCrossover(IGenerator randomNumberGenerator)
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
                if (2*i + 1 < offspringsCount) offsprings[2 * i + 1] = new Individual(offspring2);
            }
            return offsprings;
        }

        private void Crossover(bool[] parent1, bool[] parent2, 
                                                out bool[] offspring1, out bool[] offspring2)
        {
            int genesCount = parent1.Length; //No checking for the other chromosomes
            offspring1 = new bool[genesCount];
            offspring2 = new bool[genesCount];
            int crossoverPoint = rng.Next(genesCount + 1);
            // These are not necessary. They boost performance for very int arrays, but then the probability that the crossover 
            // point falls on the edges is negligible. The redundant checks for normal usecases might actually cause more harm.
            //if (crossoverPoint == 0)
            //{
            //    Array.Copy(parent1, offspring1, genesCount);
            //    Array.Copy(parent2, offspring2, genesCount);
            //}
            //else if (crossoverPoint == genesCount)
            //{
            //    Array.Copy(parent2, offspring1, genesCount);
            //    Array.Copy(parent1, offspring2, genesCount);
            //}
            Array.Copy(parent1, 0, offspring1, 0, crossoverPoint);
            Array.Copy(parent2, crossoverPoint, offspring1, 0, genesCount-crossoverPoint);
            Array.Copy(parent2, 0, offspring2, 0, crossoverPoint);
            Array.Copy(parent1, crossoverPoint, offspring2, 0, genesCount-crossoverPoint);
        }
    }
}
