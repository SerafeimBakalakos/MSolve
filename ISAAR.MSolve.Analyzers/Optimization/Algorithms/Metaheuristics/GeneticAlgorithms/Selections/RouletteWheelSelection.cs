using ISAAR.MSolve.Analyzers.Optimization.Commons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ISAAR.MSolve.Analyzers.Optimization.Algorithms.Metaheuristics.GeneticAlgorithms.Selections
{
    // TODO: There is a variant that subtracts the max fitness of this generation from all fitnesses before calculating the probabbilities
    class RouletteWheelSelection: SelectionStrategy
    {
        private readonly IdenticalParentsHandling onCollision;

        public RouletteWheelSelection(IdenticalParentsHandling onCollision = IdenticalParentsHandling.Reapply)
        {
            this.onCollision = onCollision;
        }

        Tuple<Individual, Individual>[] SelectionStrategy.Apply(Individual[] population, int offspringsCount)
        {
            Array.Sort(population); // may already be sorted from elitism. TODO: add a population class to query if it is sorted. 
            double[] probabilities = Probabilities(population, offspringsCount); // TODO: Have a double[] field and calculate in constructor to gain performance (requires const population size)
            int pairsCount = (offspringsCount - 1) / 2 + 1;
            var pairs = new Tuple<Individual, Individual>[pairsCount];
            for (int i = 0; i < pairsCount; ++i)
            {
                int parent1 = RollWheel(probabilities);
                int parent2 = RollWheel(probabilities);
                switch (onCollision)
                {
                    case IdenticalParentsHandling.Allow:
                        break;
                    case IdenticalParentsHandling.ChooseRandom:
                        while (parent1 == parent2) parent2 = RandomNumberGenerationUtilities.sysRandom.Next(offspringsCount);
                        break;
                    case IdenticalParentsHandling.Reapply:
                        while (parent1 == parent2) parent2 = RollWheel(probabilities);
                        break;
                }
                pairs[i] = new Tuple<Individual, Individual>(population[parent1], population[parent2]);
            }
            return pairs;
        }

        private double[] Probabilities(Individual[] population, int offspringsCount)
        {
            double[] probabilities = new double[offspringsCount];
            double sumFitness = 0.0;
            for (int i = 0; i < offspringsCount; ++i) sumFitness += population[i].Fitness;
            for (int i = 0; i < offspringsCount; ++i) probabilities[i] = population[i].Fitness / sumFitness;
            for (int i = 1; i < offspringsCount; ++i) probabilities[i] += probabilities[i - 1];
            // Due to error accumulation the last addition will be 0.999... Can I get away by just setting it to 1.0?
            probabilities[offspringsCount - 1] = 1.0;
            return probabilities;
        }

        private static int RollWheel(double[] probabilities)
        {
            double rand = RandomNumberGenerationUtilities.sysRandom.NextDouble();
            for (int i = 0; i < probabilities.Length; ++i)
            {
                if (rand < probabilities[i]) return i ;
            }
            throw new ArgumentException("The provided cummulative probabilities must span the whole interval [0,1]");
        }

        internal enum IdenticalParentsHandling
        {
            Allow, ChooseRandom, Reapply
        };
    }
}
