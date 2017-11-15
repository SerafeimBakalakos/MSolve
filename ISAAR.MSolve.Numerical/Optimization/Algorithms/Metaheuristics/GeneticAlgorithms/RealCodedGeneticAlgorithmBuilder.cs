using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ISAAR.MSolve.Numerical.Optimization.Algorithms.Metaheuristics.GeneticAlgorithms.Encodings;
using ISAAR.MSolve.Numerical.Optimization.Algorithms.Metaheuristics.GeneticAlgorithms.Mutations;
using ISAAR.MSolve.Numerical.Optimization.Algorithms.Metaheuristics.GeneticAlgorithms.Mutations.Gaussian;
using ISAAR.MSolve.Numerical.Optimization.Algorithms.Metaheuristics.GeneticAlgorithms.PopulationStrategies;
using ISAAR.MSolve.Numerical.Optimization.Algorithms.Metaheuristics.GeneticAlgorithms.Recombinations;
using ISAAR.MSolve.Numerical.Optimization.Algorithms.Metaheuristics.GeneticAlgorithms.Selections;
using ISAAR.MSolve.Numerical.Optimization.Algorithms.Metaheuristics.GeneticAlgorithms.Selections.FitnessScaling;
using ISAAR.MSolve.Numerical.Optimization.Problem;

namespace ISAAR.MSolve.Numerical.Optimization.Algorithms.Metaheuristics.GeneticAlgorithms
{
    public class RealCodedGeneticAlgorithmBuilder: GeneticAlgorithm<double>.Builder
    {
        private OptimizationProblem problem;

        public RealCodedGeneticAlgorithmBuilder(OptimizationProblem problem) : base(problem)
        {
            this.problem = problem;
        }

        protected override IEncoding<double> DefaultEncoding
        {
            get 
            {
                return new RealCoding();
            }
        }

        protected override IMutationStrategy<double> DefaultMutation
        {
            get // arbitrary
            {
                return new ConstantGaussianMutation(problem);
            }
        }

        protected override int DefaultPopulationSize
        {
            get // Matlab defaults
            {
                return (problem.Dimension <= 5) ? 50 : 200;
            }
        }

        protected override IPopulationStrategy<double> DefaultPopulationStrategy
        {
            get // arbitrary strategy with Matlab's default elitism 
            {
                return new StandardPopulationStrategy<double>(PopulationSize, (int)Math.Round(0.05 * PopulationSize));
            }
        }

        protected override IRecombinationStrategy<double> DefaultRecombination
        {
            get // Matlab defaults
            {
                return new UniformCrossover<double>();
            }
        }

        protected override ISelectionStrategy<double> DefaultSelection
        {
            get // Matlab defaults
            {
                return new RouletteWheelSelection<double>(new InverseRankScaling<double>(0.5));
            }
        }
    }
}
