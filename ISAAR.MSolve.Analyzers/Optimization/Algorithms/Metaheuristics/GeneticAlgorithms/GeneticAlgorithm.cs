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
    /// Genetic Algorithm with non adaptive parameters
    /// </summary>
    public partial class GeneticAlgorithm<T> : IOptimizationAlgorithm
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
        private readonly IEncoding<T> encoding;
        private readonly IPopulationStrategy<T> populationStrategy;
        private readonly ISelectionStrategy<T> selection;
        private readonly IRecombinationStrategy<T> recombination;
        private readonly IMutationStrategy<T> mutation;

        private Individual<T>[] population;

        private GeneticAlgorithm(int continuousVariablesCount, int integerVariablesCount, IObjectiveFunction fitnessFunc,
            int populationSize, IOptimizationLogger logger, IConvergenceCriterion convergenceCriterion, IEncoding<T> encoding,
            IPopulationStrategy<T> populationStrategy, ISelectionStrategy<T> selection, IRecombinationStrategy<T> recombination,
            IMutationStrategy<T> mutation)
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

        #region public methods
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
        #endregion

        #region private methods
        // This should use an Initializer object
        private void Initialize()
        {
            population = new Individual<T>[populationSize];
            for (int i = 0; i < populationSize; ++i)
            {
                T[] chromosome = encoding.CreateRandomGenotype();
                population[i] = new Individual<T>(chromosome);
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
            foreach (Individual<T> individual in population)
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
            foreach (Individual<T> individual in population)
            {
                if (individual.Fitness < BestFitness)
                {
                    BestFitness = individual.Fitness;
                    BestPosition = encoding.ComputePhenotype(individual.Chromosome); // Redundant conversion; must be removed 
                }
            }
        }
        #endregion
    }
}