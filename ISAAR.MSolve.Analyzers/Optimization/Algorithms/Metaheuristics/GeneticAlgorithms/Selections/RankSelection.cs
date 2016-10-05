﻿using ISAAR.MSolve.Analyzers.Optimization.Commons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ISAAR.MSolve.Analyzers.Optimization.Algorithms.Metaheuristics.GeneticAlgorithms.Selections
{   
    class RankSelection : SelectionStrategy
    {
        private readonly int rankExponent;
        private readonly IdenticalParentsHandling onCollision;

        public RankSelection(int rankExponent = 1, 
                                         IdenticalParentsHandling onCollision = IdenticalParentsHandling.Reapply)
        {
            if (rankExponent < 1) throw new ArgumentException("The rank exponent must be >= 1, but was " +rankExponent);
            this.rankExponent = rankExponent;
            this.onCollision = onCollision;
        }

        Tuple<Individual, Individual>[] SelectionStrategy.Apply(Individual[] population, int offspringsCount)
        {
            Array.Sort(population); // may already be sorted from elitism. TODO: add a population class to query if it is sorted. 
            double[] probabilities = Probabilities(offspringsCount); // TODO: Have a double[] field and calculate in constructor to gain performance (requires const population size)
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

        private double[] Probabilities(int offspringsCount)
        {
            double[] probabilities = new double[offspringsCount];
            //int sumRanks = offspringsCount*(offspringsCount+1) / 2; // n*(n+1) is even
            if (rankExponent > 1) throw new NotImplementedException();
            double sumRanks = 0.0;
            for (int i = 1; i <= offspringsCount; ++i) sumRanks += Math.Pow(i, rankExponent);
            for (int i = 1; i <= offspringsCount; ++i)
            {
                probabilities[i - 1] = (offspringsCount - i + 1) / sumRanks; // only for exponent = 1
            }
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
                if (rand < probabilities[i]) return i;
            }
            throw new ArgumentException("The provided cummulative probabilities must span the whole interval [0,1]");
        }

        internal enum IdenticalParentsHandling
        {
            Allow, ChooseRandom, Reapply
        };
    }
}