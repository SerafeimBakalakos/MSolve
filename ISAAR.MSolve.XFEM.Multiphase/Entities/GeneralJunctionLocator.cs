using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ISAAR.MSolve.XFEM_OLD.Multiphase.Elements;

namespace ISAAR.MSolve.XFEM_OLD.Multiphase.Entities
{
    public class GeneralJunctionLocator : IJunctionLocator
    {
        public List<PhaseJunction> FindJunctions(XModel physicalModel)
        {
            var allJunctions = new List<PhaseJunction>();
            int id = 0;
            foreach (IXFiniteElement element in physicalModel.Elements)
            {
                #region debug
                //double x = element.Nodes[0].X;
                //double y = element.Nodes[0].Y;
                //double xTarget = 800, yTarget = 680;
                //double tol = 1E-3;
                //if ((Math.Abs(x - xTarget) < tol) && (Math.Abs(y - yTarget) < tol))
                //{
                //    Console.WriteLine();
                //}
                //if (element.ID == 41)
                //{
                //    Console.WriteLine();
                //}
                #endregion

                if (element.Phases.Count < 3) continue;
                Dictionary<IPhase, SortedSet<IPhase>> phaseInteractions = FindPhaseInteractions(element);
                RemoveNonJunctionPhases(element, phaseInteractions);
                if (phaseInteractions.Count < 3) continue;

                SortedDictionary<int, List<IPhase[]>> junctionCandidates = FindJunctionPhaseCombinations(phaseInteractions);
                foreach (var pair in junctionCandidates)
                {
                    int numPhases = pair.Key;
                    List<IPhase[]> combinations = pair.Value;
                    foreach (IPhase[] combo in combinations)
                    {
                        // If this combination is a superset of another one that formed a junction, then it should be ignored
                        bool ignoreCombo = IsSupersetOfExistingJunction(combo, allJunctions);
                        if (ignoreCombo) continue;

                        // Examine current combination
                        List<IPhase> phaseChain = PhasesFormJunction(combo, phaseInteractions);
                        if (phaseChain != null)
                        {
                            var junction = new PhaseJunction();
                            junction.ID = id++;
                            junction.Element = element;
                            junction.Phases = phaseChain;
                            allJunctions.Add(junction);
                        }
                    }
                }
            }
            return allJunctions;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="phaseInteractions"></param>
        /// <returns>
        /// Values are list of combos with the same number of phases. 
        /// Keys are the number of phases in each combo belonging to the corresponding value/list. 
        /// </returns>
        private SortedDictionary<int, List<IPhase[]>> FindJunctionPhaseCombinations(
            Dictionary<IPhase, SortedSet<IPhase>> phaseInteractions)
        {
            var phases = new Dictionary<int, IPhase>();
            foreach (IPhase phase in phaseInteractions.Keys) phases[phase.ID] = phase;
            int[] allOptions = phases.Keys.ToArray();
            Array.Sort(allOptions);
            int minJunctionSize = 3;
            var phaseCombos = new SortedDictionary<int, List<IPhase[]>>();
            for (int i = minJunctionSize; i <= allOptions.Length; ++i)
            {
                phaseCombos[i] = new List<IPhase[]>();
            }
            IEnumerable<int[]> idCombos = Utilities.Combinations.FindAllCombos(allOptions, minJunctionSize);
            foreach (int[] idCombo in idCombos)
            {
                var phaseCombo = new IPhase[idCombo.Length];
                for (int i = 0; i < idCombo.Length; ++i)
                {
                    phaseCombo[i] = phases[idCombo[i]];
                }
                phaseCombos[idCombo.Length].Add(phaseCombo);
            }
            return phaseCombos;
        }

        private Dictionary<IPhase, SortedSet<IPhase>> FindPhaseInteractions(IXFiniteElement element)
        {
            var phaseInteractions = new Dictionary<IPhase, SortedSet<IPhase>>();
            foreach (PhaseBoundary boundary in element.PhaseIntersections.Keys)
            {
                bool existsPos = phaseInteractions.TryGetValue(boundary.PositivePhase, out SortedSet<IPhase> neighborsPos);
                if (!existsPos)
                {
                    neighborsPos = new SortedSet<IPhase>();
                    phaseInteractions[boundary.PositivePhase] = neighborsPos;
                }
                neighborsPos.Add(boundary.NegativePhase);

                bool existsNeg = phaseInteractions.TryGetValue(boundary.NegativePhase, out SortedSet<IPhase> neighborsNeg);
                if (!existsNeg)
                {
                    neighborsNeg = new SortedSet<IPhase>();
                    phaseInteractions[boundary.NegativePhase] = neighborsNeg;
                }
                neighborsNeg.Add(boundary.PositivePhase);
            }

            return phaseInteractions;
        }

        private static bool IsSupersetOfExistingJunction(IPhase[] phaseCombo, List<PhaseJunction> existingJunctions)
        {
            var currentSet = new HashSet<IPhase>(phaseCombo);
            foreach (PhaseJunction junction in existingJunctions)
            {
                if (currentSet.IsSupersetOf(junction.Phases) && (currentSet.Count > junction.Phases.Count)) return true;
            }
            return false;
        }

        //TODO: If phases == 4 check that each triplet does not form a junction on its own. Similarly for 5 or more phases
        private List<IPhase> PhasesFormJunction(IEnumerable<IPhase> candidatePhases,
            Dictionary<IPhase, SortedSet<IPhase>> phaseInteractions)
        {
            // Phases of the junction will be arranged in a chain: each phase will be be between its 2 neighbors
            var phasesChain = new List<IPhase>();
            var remainderPhases = new HashSet<IPhase>(candidatePhases);
            phasesChain.Add(remainderPhases.First());
            remainderPhases.Remove(phasesChain[0]);
            while (remainderPhases.Count > 0)
            {
                IPhase current = phasesChain[phasesChain.Count - 1];
                bool foundNext = false;

                // Find the next phase in the chain
                foreach (IPhase phase in remainderPhases)
                {
                    if (phaseInteractions[current].Contains(phase))
                    {
                        foundNext = true;
                        phasesChain.Add(phase);
                        remainderPhases.Remove(phase);
                        break;
                    }
                }

                // Broken chain => No junction
                if (!foundNext) return null;
            }

            // Make sure the first and last phases are neighbors
            IPhase first = phasesChain[0];
            IPhase last = phasesChain[phasesChain.Count - 1];
            if (phaseInteractions[last].Contains(first)) return phasesChain;
            else return null; // Broken chain => No junction
        }

        private void RemoveNonJunctionPhases(IXFiniteElement element, Dictionary<IPhase, SortedSet<IPhase>> phaseInteractions)
        {
            foreach (IPhase phase in element.Phases)
            {
                //Debug.Assert(phaseInteractions[phase].Count >= 1);
                try
                {
                    if (phaseInteractions[phase].Count == 1)
                    {
                        IPhase neighbor = phaseInteractions[phase].First();
                        phaseInteractions.Remove(phase);
                        phaseInteractions[neighbor].Remove(phase);
                    }
                }
                catch (Exception)
                {

                }
            }
        }
    }
}
