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
    /// GA with fixed parameters
    /// </summary>
    public class BinaryGA: IOptimizationAlgorithm                                                                                        
    {
        // Optim problem fields
        private readonly int continuousVariablesCount;
        private readonly int integerVariablesCount;
        private readonly IObjectiveFunction fitnessFunc;

        // General optim algorithm params
        private readonly IOptimizationLogger logger;
        private readonly ConvergenceChecker terminator;

        // GA params
        private readonly int populationSize;
        private readonly IEncoding encoding; // Must be abstracted behind encoding interface. Certain genetic operators only work for some encodings 
        private readonly ElitismStrategy elitism;
        private readonly SelectionStrategy selection;
        private readonly RecombinationStrategy recombination;
        private readonly MutationStrategy mutation;

        private Individual[] population;

        private BinaryGA(int continuousVariablesCount, int integerVariablesCount, IObjectiveFunction fitnessFunc, int populationSize,
            IOptimizationLogger logger, ConvergenceChecker terminator, IEncoding encoding, ElitismStrategy elitism,
            SelectionStrategy selection, RecombinationStrategy recombination, MutationStrategy mutation)
        {
            this.continuousVariablesCount = continuousVariablesCount;
            this.integerVariablesCount = integerVariablesCount;
            this.fitnessFunc = fitnessFunc;
            this.populationSize = populationSize;
            this.logger = logger;
            this.terminator = terminator;
            this.encoding = encoding;
            this.elitism = elitism;
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

        public void Solve()
        {
            CurrentIteration = 0;
            Initialize();
            logger.Log(this);

            while (!terminator.HasConverged(this))
            {
                ++CurrentIteration;
                Iterate();
                logger.Log(this);
            }
        }

        private void Initialize()
        {
            population = new Individual[populationSize];
            for (int i = 0; i < populationSize; ++i)
            {
                population[i] = Individual.CreateRandom(continuousVariablesCount, integerVariablesCount);
            }

            EvaluateCurrentIndividuals();
        }

        private void Iterate()
        {
            // TODO: Use a PopulationStrategy to handle elitism, steady state, the Matlab approach, etc. It will take as parameters the current population and the recombination, mutation and selection objects
            Individual[] elites = elitism.Apply(population);
            int offspringsCount = populationSize - elites.Length; // Is this correct?
            // What about the Natural Selection/Steady State operator, where some individuals are removed from the gene pool and are replaced by offsprings before mutation? See "Practical Genetic Algorithms"
            // Matlab implementation would be to split the offsprings into 3 parts: [a b c] where a=elitism(parents), b=recombination(parents), c=mutation(parents)
            var parents = selection.Apply(population, offspringsCount);
            Individual[] offsprings = recombination.Apply(parents, offspringsCount); // recombination strategies may require different selection strategies (e.g. 3 parents). It would be better to pass the selection object to recombination.Apply()
            mutation.Apply(offsprings); 
            Array.Copy(elites, population, elites.Length);
            Array.Copy(offsprings, 0, population, elites.Length, offsprings.Length);

            EvaluateCurrentIndividuals();
        }

        private void EvaluateCurrentIndividuals()
        {
            foreach (Individual individual in population)
            {
                if (!individual.IsEvaluated)
                {
                    var phenotype = individual.Phenotype();
                    individual.Fitness = fitnessFunc.Evaluate(individual.Phenotype());
                }
            }
            CurrentFunctionEvaluations += populationSize;
            UpdateBest();
        }

        private void UpdateBest()
        {
            // Update current best
            foreach (Individual individual in population)
            {
                if (individual.Fitness < BestFitness)
                {
                    BestFitness = individual.Fitness;
                    BestPosition = individual.Phenotype();
                }
            }
        }


        public class Builder
        {
            // Optim problem fields. Must be encapsulated in an OptimProblem object
            OptimizationProblem problem;

            public Builder(OptimizationProblem problem)
            {
                ProblemChecker.Check(problem);
                this.problem = problem;
            }

            // General optim algorithm params
            public IOptimizationLogger Logger { get; set; }
            public ConvergenceChecker Terminator { get; set; }

            // GA params
            public IEncoding Encoding { get; set; }
            public int PopulationSize { get; set; }
            public ElitismStrategy Elitism { get; set; }
            public SelectionStrategy Selection { get; set; }
            public RecombinationStrategy Recombination { get; set; }
            public MutationStrategy Mutation { get; set; }

            public BinaryGA BuildAlgorithm()
            {
                
                CheckUserParameters();
                ApplyDefaultParameters();
                Individual.Encoding = this.Encoding; // This should be done elsewhere
                return new BinaryGA(problem.Dimension, 0, problem.ObjectiveFunction, PopulationSize, Logger,
                                    Terminator, Encoding, Elitism, Selection, Recombination, Mutation);
            }

            private void ApplyDefaultParameters()
            {
                if (Logger == null)
                {
                    Logger = new BestOfIterationLogger();
                }

                if (Terminator == null) // arbitrary
                {
                    Terminator = new ConvergenceChecker();
                    Terminator.AddIndependentCriterion(new MaxIterations(1000));
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

                if (Elitism == null) // use Matlab defaults
                {
                    Elitism = new ElitismStrategy((int)(0.05 * PopulationSize) + 1, PopulationSize);
                }

                if (Selection == null) // arbitrary
                {
                    Selection = new RouletteWheelSelection();
                }

                if (Recombination == null) // use Matlab defaults
                {
                    Recombination = new UniformCrossover();
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
                // Adding an empty ConvergenceChecker should not happen, still ...
                if ((Terminator != null))
                {
                    if (Terminator.IsEmpty)
                    {
                        throw new ArgumentException("There must be at least 1 convergence criterion");
                    }
                }

                if ((PopulationSize != 0) && (PopulationSize < 1))
                {
                    throw new ArgumentException("Population size must be at least 1, but was " + PopulationSize);
                }
            }

        }
    }
}
