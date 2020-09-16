using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.Geometry.Triangulation;
using ISAAR.MSolve.XFEM_OLD.Multiphase.Elements;
using ISAAR.MSolve.XFEM_OLD.Multiphase.Geometry;

namespace ISAAR.MSolve.XFEM_OLD.Multiphase.Entities
{
    public class GeometricModel
    {
        public GeometricModel()
        {
        }

        private Dictionary<IXFiniteElement, IReadOnlyList<ElementSubtriangle>> ConformingMesh { get; set; }

        public List<PhaseJunction> Junctions { get; } = new List<PhaseJunction>();

        public IMeshTolerance MeshTolerance { get; set; } = new ArbitrarySideMeshTolerance();

        public List<IPhase> Phases { get; } = new List<IPhase>();

        public void AssociatePhasesElements(XModel physicalModel)
        {
            IPhase defaultPhase = Phases[0];
            for (int i = 1; i < Phases.Count; ++i)
            {
                Phases[i].InteractWithElements(physicalModel.Elements, MeshTolerance);
            }
            defaultPhase.InteractWithElements(physicalModel.Elements, MeshTolerance);
        }

        public void AssossiatePhasesNodes(XModel physicalModel)
        {
            IPhase defaultPhase = Phases[0];
            for (int i = 1; i < Phases.Count; ++i)
            {
                Phases[i].InteractWithNodes(physicalModel.Nodes);
            }
            defaultPhase.InteractWithNodes(physicalModel.Nodes);
        }

        public void FindConformingMesh(XModel physicalModel) //TODO: Perhaps I need a dedicated class for this
        {
            ConformingMesh = new Dictionary<IXFiniteElement, IReadOnlyList<ElementSubtriangle>>();
            var triangulator = new ConformingTriangulator();
            foreach (IXFiniteElement element in physicalModel.Elements)
            {
                if (element.PhaseIntersections.Count == 0) continue;
                ConformingMesh[element] = 
                    triangulator.FindConformingMesh(element, element.PhaseIntersections.Values, MeshTolerance);
            }
        }

        public void FindJunctions(XModel physicalModel)
        {
            int id = 0;
            foreach (IXFiniteElement element in physicalModel.Elements)
            {
                #region debug
                //double x = element.Nodes[0].X;
                //double y = element.Nodes[0].Y;
                //double xTarget = -0.98, yTarget = 0.88;
                //double tol = 1E-3;
                //if ((Math.Abs(x - xTarget) < tol) && (Math.Abs(y - yTarget) < tol))
                //{
                //    Console.WriteLine();
                //}
                #endregion

                if (element.Phases.Count < 3) continue;
                Dictionary<IPhase, SortedSet<IPhase>> phaseInteractions = FindPhaseInteractions(element);
                RemoveNonJunctionPhases(element, phaseInteractions);
                if (phaseInteractions.Count < 3) continue;

                foreach (IPhase[] junctionCandidates in FindCandidatePhasesForJunction(phaseInteractions))
                {
                    List<IPhase> phaseChain = PhasesFormJunction(junctionCandidates, phaseInteractions);
                    if (phaseChain != null)
                    {
                        var junction = new PhaseJunction();
                        junction.ID = id++;
                        junction.Element = element;
                        junction.Phases = phaseChain;
                        Junctions.Add(junction);
                    }
                }
            }
        }

        public static IPhase FindPhaseAt(CartesianPoint point, IXFiniteElement element)
        {
            IPhase defaultPhase = null;
            foreach (IPhase phase in element.Phases)
            {
                // Avoid searching for the point in the default phase, since its shape is hihly irregular.
                if (phase is DefaultPhase)
                {
                    defaultPhase = phase;
                    continue;
                }
                else if (phase.Contains(point)) return phase;
            }

            // If the point is not contained in any other phases, it must be in the default phase 
            Debug.Assert(defaultPhase != null, "The point does not belong to any phases");
            return defaultPhase;
        }

        public IReadOnlyList<ElementSubtriangle> GetConformingTriangulationOf(IXFiniteElement element)
        {
            Debug.Assert(ConformingMesh != null);
            return ConformingMesh[element];
        }

        private IEnumerable<IPhase[]> FindCandidatePhasesForJunction(Dictionary<IPhase, SortedSet<IPhase>> phaseInteractions)
        {
            var phases = new Dictionary<int, IPhase>();
            foreach (IPhase phase in phaseInteractions.Keys) phases[phase.ID] = phase;
            int[] allOptions = phases.Keys.ToArray();
            Array.Sort(allOptions);
            int minJunctionSize = 3;
            IEnumerable<int[]> idCombos = Utilities.Combinations.FindAllCombos(allOptions, minJunctionSize);
            var phaseCombos = new List<IPhase[]>();
            foreach (int[] idCombo in idCombos)
            {
                var phaseCombo = new IPhase[idCombo.Length];
                for (int i = 0; i < idCombo.Length; ++i)
                {
                    phaseCombo[i] = phases[idCombo[i]];
                }
                phaseCombos.Add(phaseCombo);
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
                if (phaseInteractions[phase].Count == 1)
                {
                    IPhase neighbor = phaseInteractions[phase].First();
                    phaseInteractions.Remove(phase);
                    phaseInteractions[neighbor].Remove(phase);
                }
            }
        }


    }
}
