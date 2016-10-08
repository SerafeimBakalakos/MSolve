using ISAAR.MSolve.Analyzers.Optimization.Algorithms.Metaheuristics.GeneticAlgorithms.PopulationStrategies;
using ISAAR.MSolve.Analyzers.Optimization.Algorithms.Metaheuristics.GeneticAlgorithms.Encodings;
using ISAAR.MSolve.Analyzers.Optimization.Algorithms.Metaheuristics.GeneticAlgorithms.Mutations;
using ISAAR.MSolve.Analyzers.Optimization.Algorithms.Metaheuristics.GeneticAlgorithms.Recombinations;
using ISAAR.MSolve.Analyzers.Optimization.Algorithms.Metaheuristics.GeneticAlgorithms.Selections;
using ISAAR.MSolve.Analyzers.Optimization.Convergence;
using ISAAR.MSolve.Analyzers.Optimization.Output;
using ISAAR.MSolve.Analyzers.Optimization.Problems;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ISAAR.MSolve.Analyzers.Optimization.Algorithms.Metaheuristics.GeneticAlgorithms
{
    /// <summary>
    /// GA with non adaptive parameters
    /// </summary>
    public class BinaryGeneticAlgorithm: IOptimizationAlgorithm                                                                                        
    {
        #region fields, properties, constructor
        // Optim problem fields
        private readonly int continuousVariablesCount;
        private readonly int integerVariablesCount;
        private readonly IObjectiveFunction fitnessFunc;

        // General optim algorithm params
        private readonly IOptimizationLogger logger;
        private readonly IConvergenceCriterion convergenceCriterion;

        // GA params
        private readonly int populationSize;
        private readonly IEncoding<bool> encoding; 
        private readonly IPopulationStrategy<bool> populationStrategy;
        private readonly ISelectionStrategy<bool> selection;
        private readonly IRecombinationStrategy<bool> recombination;
        private readonly IMutationStrategy<bool> mutation;

        private Individual<bool>[] population;

        private BinaryGeneticAlgorithm(int continuousVariablesCount, int integerVariablesCount, IObjectiveFunction fitnessFunc, 
            int populationSize, IOptimizationLogger logger, IConvergenceCriterion convergenceCriterion, IEncoding<bool> encoding, 
            IPopulationStrategy<bool> populationStrategy, ISelectionStrategy<bool> selection, IRecombinationStrategy<bool> recombination, 
            IMutationStrategy<bool> mutation)
        {
            this.continuousVariablesCount = continuousVariablesCount;
            this.integerVariablesCount = integerVariablesCount;
            this.fitnessFunc = fitnessFunc;
            this.populationSize = populationSize;
            this.logger = logger;
            this.convergenceCriterion = convergenceCriterion;
            this.encoding = encoding;
            this.populationStrategy = populationStrategy;
            this.selection = selection;
            this.recombination = recombination;
            this.mutation = mutation;

            this.CurrentIteration = -1; // Initialization phase is not counted towards iterations
            this.BestPosition = null;
            this.BestFitness = double.MaxValue;
        }

        public double BestFitness { get; private set; }
        public double[] BestPosition { get; private set; }
        public int CurrentIteration { get; private set; }
        public double CurrentFunctionEvaluations { get; private set; }
        #endregion

        #region methods
        public void Solve()
        {
            CurrentIteration = 0;
            Initialize();
            logger.Log(this);

            while (!convergenceCriterion.HasConverged(this))
            {
                ++CurrentIteration;
                Iterate();
                logger.Log(this);
            }
        }

        // This should use an Initializer object
        private void Initialize()
        {
            population = new Individual<bool>[populationSize];
            for (int i = 0; i < populationSize; ++i)
            {
                bool[] chromosome = encoding.CreateRandomGenotype();
                population[i] = new Individual<bool>(chromosome);
            }

            EvaluateCurrentIndividuals();
        }

        private void Iterate()
        {
            population = populationStrategy.CreateNextGeneration(population, selection, recombination, mutation);

            EvaluateCurrentIndividuals();
        }

        private void EvaluateCurrentIndividuals()
        {
            foreach (Individual<bool> individual in population)
            {
                if (!individual.IsEvaluated)
                {
                    double[] phenotype = encoding.ComputePhenotype(individual.Chromosome);
                    individual.Fitness = fitnessFunc.Evaluate(phenotype);
                }
            }
            CurrentFunctionEvaluations += populationSize;
            UpdateBest();
        }

        private void UpdateBest()
        {
            foreach (Individual<bool> individual in population)
            {
                if (individual.Fitness < BestFitness)
                {
                    BestFitness = individual.Fitness;
                    BestPosition = encoding.ComputePhenotype(individual.Chromosome); // Redundant conversion; must be removed 
                }
            }
        }
        #endregion

        public class Builder
        {
            #region fields, properties, constructor
            // Optim problem fields.
            OptimizationProblem problem;

            public Builder(OptimizationProblem problem)
            {
                ProblemChecker.Check(problem);
                this.problem = problem;
            }

            // General optim algorithm params
            public IOptimizationLogger Logger { get; set; }
            public IConvergenceCriterion ConvergenceCriterion { get; set; }

            // GA params
            public IEncoding<bool> Encoding { get; set; }
            public int PopulationSize { get; set; }
            public IPopulationStrategy<bool> PopulationStrategy { get; set; }
            public ISelectionStrategy<bool> Selection { get; set; }
            public IRecombinationStrategy<bool> Recombination { get; set; }
            public IMutationStrategy<bool> Mutation { get; set; }
            #endregion

            #region methods
            public BinaryGeneticAlgorithm BuildAlgorithm()
            {
                
                CheckUserParameters();
                ApplyDefaultParameters();
                return new BinaryGeneticAlgorithm(problem.Dimension, 0, problem.ObjectiveFunction, PopulationSize, Logger,
                                    ConvergenceCriterion, Encoding, PopulationStrategy, Selection, Recombination, Mutation);
            }

            private void ApplyDefaultParameters()
            {
                if (Logger == null)
                {
                    Logger = new BestOfIterationLogger();
                }

                if (ConvergenceCriterion == null) // arbitrary
                {
                    ConvergenceCriterion = new MaxIterations(100 * problem.Dimension);
                }

                if (PopulationSize == 0) // use Matlab defaults
                {
                    PopulationSize = (problem.Dimension <= 5) ? 50 : 200;

                    //if (integerVariablesCount == 0) // continuous problem
                    //{
                    //    PopulationSize = (continuousVariablesCount <= 5) ? 50 : 200;
                    //}
                    //else // Mixed integer problem
                    //{
                    //    PopulationSize = Math.Min(100, Math.Max(40, 10 * (continuousVariablesCount + integerVariablesCount)));
                    //}
                }

                if (Encoding == null) // arbitrary
                {
                    Encoding = new GrayCodeEncoding(problem, 32, 8); // sizes of float, char 
                } 

                if (PopulationStrategy == null) // arbitrary strategy with Matlab's default elitism 
                {
                    PopulationStrategy = new StandardPopulationStrategy<bool>(PopulationSize, (int)Math.Round(0.05 * PopulationSize));
                }

                if (Selection == null) // arbitrary
                {
                    Selection = new RouletteWheelSelection<bool>();
                }

                if (Recombination == null) // use Matlab defaults
                {
                    Recombination = new UniformCrossover<bool>();
                }

                if (Mutation == null) // arbitrary
                {
                    Mutation = new BitFlipMutation(0.2);
                }
            }

            // Crash when user provides incompatible parameters. Warn him when the parameters, although legal, may result in poor performance 
            // Will ignore the default values. TODO find a better way to handle this.
            private void CheckUserParameters()
            {
                if ((PopulationSize != 0) && (PopulationSize < 1))
                {
                    throw new ArgumentException("Population size must be at least 1, but was " + PopulationSize);
                }
            }
            #endregion
        }
    }
}
