using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ISAAR.MSolve.Analyzers.Optimization.Algorithms.Metaheuristics.GeneticAlgorithms.Recombinations
{
    public interface IRecombinationStrategy<T>
    {
        Individual<T>[] Apply(Tuple<Individual<T>, Individual<T>>[] parents, int offspringsCount);
    }
}
