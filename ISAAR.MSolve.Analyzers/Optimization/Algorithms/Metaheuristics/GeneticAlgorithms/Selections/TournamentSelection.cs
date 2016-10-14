using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ISAAR.MSolve.Analyzers.Optimization.Algorithms.Metaheuristics.GeneticAlgorithms.Selections
{
    class TournamentSelection<T> : ISelectionStrategy<T>
    {
        public Individual<T>[][] Apply(Individual<T>[] population, int parentGroupsCount, 
                                       int parentsPerGroup, bool allowIdenticalParents)
        {
            throw new NotImplementedException();
        }
    }
}
