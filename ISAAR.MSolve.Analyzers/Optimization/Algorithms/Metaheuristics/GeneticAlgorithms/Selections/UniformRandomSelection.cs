﻿using ISAAR.MSolve.Analyzers.Optimization.Commons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ISAAR.MSolve.Analyzers.Optimization.Algorithms.Metaheuristics.GeneticAlgorithms.Selections
{
    class UniformRandomSelection : SelectionStrategy
    {
        private readonly bool allowIdenticalParents;

        public UniformRandomSelection(bool allowIdenticalParents = false)
        {
            this.allowIdenticalParents = allowIdenticalParents;
        }

        Tuple<Individual, Individual>[] SelectionStrategy.Apply(Individual[] population, int offspringsCount)
        {
            Random rng = RandomNumberGenerationUtilities.sysRandom;
            int pairsCount = (offspringsCount - 1) / 2 + 1;
            var pairs = new Tuple<Individual, Individual>[pairsCount];
            for (int i = 0; i < pairsCount; ++i)
            {
                int parent1 = rng.Next(population.Length);
                int parent2 = rng.Next(population.Length);
                if (!allowIdenticalParents)
                {
                    while (parent1 == parent2) parent2 = rng.Next(population.Length);
                }
                pairs[i] = new Tuple<Individual, Individual>(population[parent1], population[parent2]);
            }
            return pairs;
        }
    }
}