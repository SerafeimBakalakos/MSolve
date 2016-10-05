using System;
using System.Collections.Generic;

namespace ISAAR.MSolve.Analyzers.Optimization.Convergence
{
    // Collection of convergence criteria. The criteria are organized into groups. 
    // Convergence is achieved if ANY group is satisfied. For a group to be satisfied ALL its criteria must be satisfied.
    // After the first termination polling request, adding criteria will throw an exception.
    public class ConvergenceChecker
    {
        private LinkedList<IEnumerable<IConvergenceCriterion>> criteriaGroups;
        private bool lockInput; // A builder is overkill for this class

        public ConvergenceChecker()
        {
            this.criteriaGroups = new LinkedList<IEnumerable<IConvergenceCriterion>>();
            this.lockInput = false;
        }

        // TODO: check null input
        public void AddIndependentCriterion(IConvergenceCriterion criterion)
        {
            if (!lockInput)
            {
                criteriaGroups.AddLast(new IConvergenceCriterion[] { criterion });
            }
            else
            {
                throw new InvalidOperationException("Cannot add new criteria after the optimization procedure has began.");
            }
        }

        // TODO: check null input
        public void AddDependentCriteria(IEnumerable<IConvergenceCriterion> criteria)
        {
            if (!lockInput)
            {
                criteriaGroups.AddLast(criteria);
            }
            else
            {
                throw new InvalidOperationException("Cannot add new criteria after the optimization procedure has began.");
            }
        }

        public bool HasConverged(IOptimizationAlgorithm algorithm)
        {
            bool convergence = false;
            foreach (var criteriaGroup in this.criteriaGroups)
            {
                convergence = true;
                foreach (var criterion in criteriaGroup)
                {
                    if (!criterion.HasConverged(algorithm))
                    {
                        convergence = false;
                        break;
                    }
                }
                if (convergence)
                {
                    break;
                }
            }
            return convergence;
        }

        public bool IsEmpty
        {
            get { return this.criteriaGroups.Count == 0; }
        }

        public void Lock()
        {
            this.lockInput = true;
        }
    }
}
