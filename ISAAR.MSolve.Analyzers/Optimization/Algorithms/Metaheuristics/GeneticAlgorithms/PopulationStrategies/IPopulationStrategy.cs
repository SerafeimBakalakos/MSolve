using ISAAR.MSolve.Analyzers.Optimization.Algorithms.Metaheuristics.GeneticAlgorithms.Mutations;
using ISAAR.MSolve.Analyzers.Optimization.Algorithms.Metaheuristics.GeneticAlgorithms.Recombinations;
using ISAAR.MSolve.Analyzers.Optimization.Algorithms.Metaheuristics.GeneticAlgorithms.Selections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ISAAR.MSolve.Analyzers.Optimization.Algorithms.Metaheuristics.GeneticAlgorithms.PopulationStrategies
{
    public interface IPopulationStrategy
    {
        Individual[] CreateNextGeneration(Individual[] originalPopulation, ISelectionStrategy selection, 
                                      IRecombinationStrategy recombination, IMutationStrategy mutation); 
    }
}
