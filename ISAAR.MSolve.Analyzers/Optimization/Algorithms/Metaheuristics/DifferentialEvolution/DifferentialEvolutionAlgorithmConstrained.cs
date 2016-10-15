using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ISAAR.MSolve.Analyzers.Optimization.Constraints.Penalties;
using ISAAR.MSolve.Analyzers.Optimization.Convergence;
using ISAAR.MSolve.Analyzers.Optimization.Problem;

namespace ISAAR.MSolve.Analyzers.Optimization.Algorithms.Metaheuristics.DifferentialEvolution
{
    public class DifferentialEvolutionAlgorithmConstrained: DifferentialEvolutionAlgorithm
    {
        private IConstraintFunction[] inequalityConstraints;
        private IPenaltyStatic penalty;

        protected DifferentialEvolutionAlgorithmConstrained(OptimizationProblem optimizationProblem, int populationSize,
                                double mutationFactor, double crossoverProbability, IConvergenceCriterion convergenceCriterion,
                                IPenaltyStatic penalty):
            base (optimizationProblem, populationSize, mutationFactor, crossoverProbability, convergenceCriterion)
        {
            this.inequalityConstraints = optimizationProblem.InequalityConstraints;
            this.penalty = penalty;
        }

        protected override void Evaluation(Individual[] individuals)
        {
            //base.Evaluation(individuals);

            for (int i = 0; i < populationSize; i++)
            {
                double fitness = objectiveFunction.Evaluate(individuals[i].Position);
                individuals[i].ObjectiveValue = penalty.Evaluate(fitness, individuals[i].Position);
            }
            CurrentFunctionEvaluations += this.populationSize;
        }

        public new class Builder : DifferentialEvolutionAlgorithm.Builder
        {
            private OptimizationProblem optimizationProblem;

            public IPenaltyStatic Penalty { get; set; }

            public Builder(OptimizationProblem optimizationProblem) : base(optimizationProblem)
            {
                this.optimizationProblem = optimizationProblem;
            }

            public new IOptimizationAlgorithm Build()
            {
                ProblemChecker.Check(optimizationProblem);
                return new DifferentialEvolutionAlgorithmConstrained(optimizationProblem, PopulationSize,
                    MutationFactor, CrossoverProbability, ConvergenceCriterion, Penalty);
            }
        }

    }
}
