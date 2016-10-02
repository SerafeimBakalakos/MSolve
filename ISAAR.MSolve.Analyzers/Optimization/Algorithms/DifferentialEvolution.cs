using static ISAAR.MSolve.Analyzers.Optimization.Commons.VectorOperations;
using System;


namespace ISAAR.MSolve.Analyzers.Optimization.Algorithms
{
    /// <summary>
    ///     Class implementing the Differential Evolution metaheuristic search algorithm
    ///     <see href = " https://doi.org/10.1023/A:1008202821328"> 
    ///     R. Storn and K. Price (1997). "Differential evolution - a simple and efficient heuristic 
    ///     for global optimization over continuous spaces", Journal of Global Optimization, 11, pp.341–359.
    ///     </see>
    /// </summary>
    public class DifferentialEvolution : IOptimizationAlgorithm
    {
        private readonly int populationSize = 40;
        private readonly int dimension; 
        private double mutationFactor
            = 0.6;
        private double crossoverProbability 
            = 0.9;
        private int maximumFunctionEvaluations;
        private readonly int maxIterations = 1000;
        private int currentIteration;

        private IObjectiveFunction function;
        //private ObjectiveProblem objectiveProblem;
        private double[] lowerBound;
        private double[] upperBound;

        private Individual[] individuals;
        private Individual[] offsprings;

        private readonly Random randomNumberGenerator = new Random();

        //public DifferentialEvolution(ObjectiveProblem objectiveProblem)
        public DifferentialEvolution(int dimension, double[] lowerBound, double[] upperBound, IObjectiveFunction function)
        {
            this.dimension = dimension;
            this.lowerBound = lowerBound;
            this.upperBound = upperBound;
            this.function = function;
        }

        public double BestFitness
        {
            get; private set;
        } = double.MaxValue;

        public double[] BestPosition
        {
            get; private set;
        }

        private void Initialize()
        {
            individuals = new Individual[populationSize];

            for (int i = 0; i < populationSize; i++)
            {
                double[] position = new double[dimension];

                for (int v = 0; v < dimension; v++)
                {
                    position[v] = lowerBound[v] + randomNumberGenerator.NextDouble() * (upperBound[v] - lowerBound[v]);
                }

                individuals[i] = new Individual(position, double.MaxValue);
            }

            // Evaluate the initial population
            Evaluation(individuals);

            // Store the best fitness and position
            for (int i = 0; i < populationSize; i++)
            {
                if (individuals[i].ObjectiveValue < BestFitness)
                {
                    BestFitness = individuals[i].ObjectiveValue;
                    BestPosition = individuals[i].Position;
                }
            }
        }

        private void Iterate()
        {
            Mutation();
            CheckBounds();
            Recombination();
            Evaluation(offsprings);
            Selection();
        }

        /// <summary>
        ///     Performs crossover according to binary operation.
        /// </summary>
        private void Recombination()
        {
            int jRand = randomNumberGenerator.Next(dimension);

            for (int i = 0; i < populationSize; i++)
            {
                double[] donorVector = offsprings[i].Position;
                double[] trialVector = new double[dimension];

                for (int d = 0; d < dimension; d++)
                {
                    if (randomNumberGenerator.NextDouble() < crossoverProbability || d == jRand)
                        trialVector[d] = donorVector[d];
                    else
                        trialVector[d] = individuals[i].Position[d];

               }
                offsprings[i].Position = trialVector;
            }

        }
        /// <summary>
        ///     Performs selection
        /// </summary>

        private void Selection()
        {
            for (int i = 0; i < populationSize; i++)
            {
                if (offsprings[i].ObjectiveValue < individuals[i].ObjectiveValue)
                {
                    individuals[i] = offsprings[i];
                }

                if (individuals[i].ObjectiveValue < BestFitness)
                {
                    BestFitness = individuals[i].ObjectiveValue;
                    BestPosition = individuals[i].Position;
                }
            }
        }

        private void Evaluation(Individual[] individuals)
        {
            for (int i = 0; i < populationSize; i++)
            {
                double fitness = function.Fitness(individuals[i].Position);

                individuals[i].ObjectiveValue = fitness;

            }
        }

        /// <summary>
        ///     Performs mutation of the population according to de/rand/1/bin scheme
        /// </summary>
        private void Mutation()
        {
            offsprings = new Individual[populationSize];

            for (int i = 0; i < populationSize; i++)
            {
                int r1 = randomNumberGenerator.Next(populationSize);
                int r2 = randomNumberGenerator.Next(populationSize);
                int r3 = randomNumberGenerator.Next(populationSize);

                
                double[] donorVector = Add(individuals[r1].Position, Scale(mutationFactor, Subtract(individuals[r2].Position, individuals[r3].Position)));

                offsprings[i] = new Individual(donorVector, double.MaxValue);
            }

        }

        /// <summary>
        ///     Verify upper and lower bounds for the donor vector
        /// </summary>
        private void CheckBounds()
        {

            for (int i = 0; i < populationSize; i++)
            {
                double[] donorVector = offsprings[i].Position;

                for (int j = 0; j < dimension; j++)
                {
                    if (donorVector[j] > upperBound[j]) donorVector[j] = upperBound[j];
                    else if (donorVector[j] < lowerBound[j]) donorVector[j] = lowerBound[j];
                }
            }
        }

        public void Solve()
        {
            Initialize();
            currentIteration = 0;
            while (currentIteration < maxIterations)
            {
                currentIteration++;
                Iterate();

                // Write best fitness and position
                Console.WriteLine(String.Format(@"Iter: {0} | {1} ", currentIteration, BestFitness));
                // Array.ForEach(BestPosition, x => Console.WriteLine(x));
            }
        }

    }
}