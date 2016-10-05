using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ISAAR.MSolve.Analyzers.Optimization.Algorithms.Metaheuristics.GeneticAlgorithms.PopulationStrategies
{
    public class ElitismStrategy
    {
        private readonly int elitesCount;

        public ElitismStrategy(int elitesCount, int populationSize)
        {
            if ((elitesCount < 0) || (elitesCount >= populationSize))
            {
                throw new ArgumentException("The number of elites must belong to the interval [0, populationSize-1), but was " 
                    + elitesCount);
            }
            this.elitesCount = elitesCount;
        }

        public Individual[] Apply(Individual[] population)
        {
            Array.Sort(population);
            Individual[] elites = new Individual[elitesCount];
            Array.Copy(population, elites, elitesCount);
            return elites;
        }
    }
}
