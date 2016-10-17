using ISAAR.MSolve.Analyzers.Optimization.Commons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Troschuetz.Random;
using ISAAR.MSolve.Analyzers.Optimization.Algorithms.Metaheuristics.GeneticAlgorithms.Selections.Expectations;

namespace ISAAR.MSolve.Analyzers.Optimization.Algorithms.Metaheuristics.GeneticAlgorithms.Selections
{
    public class RouletteWheelSelection<T> : ISelectionStrategy<T>
    {
        private readonly IExpectationStrategy<T> expectationStrategy;
        private readonly IGenerator rng;

        public RouletteWheelSelection(IExpectationStrategy<T> expectationStrategy):
            this(expectationStrategy, RandomNumberGenerationUtilities.troschuetzRandom)
        {
        }

        public RouletteWheelSelection(IExpectationStrategy<T> expectationStrategy, IGenerator randomNumberGenerator)
        {
            if (expectationStrategy == null) throw new ArgumentException("The expectation strategy must not be null");
            this.expectationStrategy = expectationStrategy;

            if (randomNumberGenerator == null) throw new ArgumentException("The random number generator must not be null");
            this.rng = randomNumberGenerator;
        }

        public Individual<T>[][] Apply(Individual<T>[] population, int parentGroupsCount,
                                       int parentsPerGroup, bool allowIdenticalParents)
        {
            double[] expectations = expectationStrategy.CalculateExpectations(population);
            Roulette roulette = Roulette.CreateFromPositive(expectations, rng);

            var parentGroups = new Individual<T>[parentGroupsCount][];
            for (int group = 0; group < parentGroupsCount; ++group)
            {
                parentGroups[group] = new Individual<T>[parentsPerGroup];
                for (int parent = 0; parent < parentsPerGroup; ++parent)
                {
                    Individual<T> individual = population[roulette.SpinWheelWithBall()];
                    if (!allowIdenticalParents)
                    {
                        while (parentGroups[group].Contains<Individual<T>>(individual))
                        {
                            individual = population[roulette.SpinWheelWithBall()];
                        }
                    }
                    parentGroups[group][parent] = individual;
                }
            }
            return parentGroups;
        }
    }
}
