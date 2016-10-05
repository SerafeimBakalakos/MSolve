﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ISAAR.MSolve.Analyzers.Optimization.Algorithms.Metaheuristics.GeneticAlgorithms.Selections
{
    public interface SelectionStrategy
    {
        Tuple<Individual, Individual>[] Apply(Individual[] population, int offspringsCount);
    }
}