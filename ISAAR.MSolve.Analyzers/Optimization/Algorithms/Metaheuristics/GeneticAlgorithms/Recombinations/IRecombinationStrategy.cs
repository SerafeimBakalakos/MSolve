using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ISAAR.MSolve.Analyzers.Optimization.Algorithms.Metaheuristics.GeneticAlgorithms.Recombinations
{
    public interface IRecombinationStrategy
    {
        Individual[] Apply(Tuple<Individual, Individual>[] parents, int offspringsCount);
    }
}
