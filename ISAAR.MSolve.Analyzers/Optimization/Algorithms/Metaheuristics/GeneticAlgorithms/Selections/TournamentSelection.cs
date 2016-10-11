using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ISAAR.MSolve.Analyzers.Optimization.Algorithms.Metaheuristics.GeneticAlgorithms.Selections
{
    class TournamentSelection<T> : ISelectionStrategy<T>
    {
        public Tuple<Individual<T>, Individual<T>>[] Apply(Individual<T>[] population, int offspringsCount)
        {
            throw new NotImplementedException();
        }
    }
}
