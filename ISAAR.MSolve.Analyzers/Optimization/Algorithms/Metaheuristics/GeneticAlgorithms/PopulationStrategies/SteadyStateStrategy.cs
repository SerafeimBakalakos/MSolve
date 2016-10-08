﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ISAAR.MSolve.Analyzers.Optimization.Algorithms.Metaheuristics.GeneticAlgorithms.Mutations;
using ISAAR.MSolve.Analyzers.Optimization.Algorithms.Metaheuristics.GeneticAlgorithms.Recombinations;
using ISAAR.MSolve.Analyzers.Optimization.Algorithms.Metaheuristics.GeneticAlgorithms.Selections;

namespace ISAAR.MSolve.Analyzers.Optimization.Algorithms.Metaheuristics.GeneticAlgorithms.PopulationStrategies
{
    class SteadyStateStrategy : IPopulationStrategy
    {
        public Individual[] CreateNextGeneration(Individual[] originalPopulation, ISelectionStrategy selection, IRecombinationStrategy recombination, IMutationStrategy mutation)
        {
            throw new NotImplementedException();
        }
    }
}
