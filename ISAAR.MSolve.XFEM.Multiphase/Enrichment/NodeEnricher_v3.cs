using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using ISAAR.MSolve.XFEM_OLD.Multiphase.Elements;
using ISAAR.MSolve.XFEM_OLD.Multiphase.Enrichment.SingularityResolution;
using ISAAR.MSolve.XFEM_OLD.Multiphase.Entities;

//TODO: Add heaviside singularity resolver
//TODO: Remove casts
namespace ISAAR.MSolve.XFEM_OLD.Multiphase.Enrichment
{
    public class NodeEnricher_v3
    {
        private readonly GeometricModel geometricModel;
        private readonly XModel physicalModel;
        private readonly ISingularityResolver singularityResolver;

        public NodeEnricher_v3(XModel physicalModel, GeometricModel geometricModel, ISingularityResolver singularityResolver)
        {
            this.physicalModel = physicalModel;
            this.geometricModel = geometricModel;
            this.singularityResolver = singularityResolver;
            this.JunctionEnrichments = new Dictionary<PhaseJunction, HashSet<JunctionEnrichment_v2>>();
        }

        //public Dictionary<IXFiniteElement, HashSet<IJunctionEnrichment>> JunctionElements { get; }

        public Dictionary<PhaseJunction, HashSet<JunctionEnrichment_v2>> JunctionEnrichments { get; }

        public void ApplyEnrichments()
        {
            int numStepEnrichments = DefineStepEnrichments(0);
            int numJunctionEnrichments = DefineJunctionEnrichments(numStepEnrichments);
            EnrichNodes();
            RemoveRedundantJunctions();
        }

        

        private int DefineJunctionEnrichments(int idStart)
        {
            int id = idStart;
            foreach (PhaseJunction junction in geometricModel.Junctions)
            {
                // It is possible that junction enrichments already exists for these phases, from a nearby junction point.
                HashSet<JunctionEnrichment_v2> junctionEnrichments = null;
                foreach (PhaseJunction other in JunctionEnrichments.Keys)
                {
                    if (junction.HasSamePhasesAs(other))
                    {
                        junctionEnrichments = JunctionEnrichments[other];
                        break;
                    }
                }

                // Else create new ones. For n phases, apply n-1 junction enrichments
                if (junctionEnrichments == null)
                {
                    junctionEnrichments = new HashSet<JunctionEnrichment_v2>();
                    for (int i = 0; i < junction.Phases.Count - 1; ++i)
                    {
                        var enrichment = new JunctionEnrichment_v2(id++, junction, junction.Phases[i], junction.Phases[i + 1]);
                        junctionEnrichments.Add(enrichment);
                    }
                }
                JunctionEnrichments[junction] = junctionEnrichments;
            }
            return id - idStart;
        }

        private int DefineStepEnrichments(int idStart)
        {
            // Keep track of identified interactions between phases, to avoid duplicate enrichments
            var uniqueEnrichments = new Dictionary<int, Dictionary<int, StepEnrichment>>();

            //TODO: This would be faster, but only works for consecutive phase IDs starting from 0.
            //var uniqueEnrichments = new Dictionary<int, StepEnrichment>[geometricModel.Phases.Count];

            #region default phase
            //foreach (IPhase phase in geometricModel.Phases)
            #endregion
            for (int p = 1; p < geometricModel.Phases.Count; ++p)
            {
                uniqueEnrichments[geometricModel.Phases[p].ID] = new Dictionary<int, StepEnrichment>();
            }

            int id = idStart;
            #region default phase
            //foreach (IPhase phase in geometricModel.Phases)
            #endregion
            for (int p = 1; p < geometricModel.Phases.Count; ++p)
            {
                foreach (PhaseBoundary boundary in geometricModel.Phases[p].Boundaries)
                {
                    // It may have been processed when iterating the boundaries of the opposite phase.
                    if (boundary.StepEnrichment != null) continue;

                    // Find min/max phase IDs to uniquely identify the interaction
                    IPhase minPhase, maxPhase;
                    if (boundary.PositivePhase.ID < boundary.NegativePhase.ID)
                    {
                        minPhase = boundary.PositivePhase;
                        maxPhase = boundary.NegativePhase;
                    }
                    else
                    {
                        minPhase = boundary.NegativePhase;
                        maxPhase = boundary.PositivePhase;
                    }

                    // Find the existing enrichment for this phase interaction or create a new one
                    bool enrichmentsExists =
                        uniqueEnrichments[maxPhase.ID].TryGetValue(minPhase.ID, out StepEnrichment enrichment);
                    if (!enrichmentsExists)
                    {
                        enrichment = new StepEnrichment(id++, boundary.PositivePhase, boundary.NegativePhase);
                        uniqueEnrichments[maxPhase.ID][minPhase.ID] = enrichment;
                    }

                    boundary.StepEnrichment = enrichment;
                }
            }
            return id - idStart;
        }

        private void EnrichNode(XNode node, IEnrichment enrichment)
        {
            if (!node.Enrichments.ContainsKey(enrichment))
            {
                double value = enrichment.EvaluateAt(node);
                node.Enrichments[enrichment] = value;
            }
        }

        private void EnrichNodes()
        {
            // Junction enrichments
            foreach (var pair in JunctionEnrichments)
            {
                IXFiniteElement element = pair.Key.Element;
                foreach (JunctionEnrichment_v2 junctionEnrichment in pair.Value)
                {
                    foreach (XNode node in element.Nodes)
                    {
                        if (!HasCorrespondingJunction(node, junctionEnrichment))
                        {
                            EnrichNode(node, junctionEnrichment);
                        }
                    }
                }
            }

            #region debug
            //XNode problemNode = physicalModel.Nodes[1868];
            #endregion

            // Find nodes to potentially be enriched by step enrichments
            var nodesPerStepEnrichment = new Dictionary<IEnrichment, HashSet<XNode>>();
            #region default phase
            //foreach (IPhase phase in geometricModel.Phases)
            #endregion
            for (int p = 1; p < geometricModel.Phases.Count; ++p)
            {
                var phase = (ConvexPhase)(geometricModel.Phases[p]);
                foreach (IXFiniteElement element in phase.IntersectedElements)
                {
                    foreach (PhaseBoundary boundary in element.PhaseIntersections.Keys)
                    {
                        // Find the nodes to potentially be enriched by this step enrichment 
                        StepEnrichment stepEnrichment = (StepEnrichment)(boundary.StepEnrichment);
                        bool exists = nodesPerStepEnrichment.TryGetValue(stepEnrichment, out HashSet<XNode> nodesToEnrich);
                        if (!exists)
                        {
                            nodesToEnrich = new HashSet<XNode>();
                            nodesPerStepEnrichment[stepEnrichment] = nodesToEnrich;
                        }

                        // Only enrich a node if it does not have a corresponding junction enrichment
                        foreach (XNode node in element.Nodes)
                        {
                            if (!HasCorrespondingJunction(node, stepEnrichment)) nodesToEnrich.Add(node);
                        }
                    }
                }
            }

            // Enrich these nodes with the corresponding step enrichment
            foreach (var enrichmentNodesPair in nodesPerStepEnrichment)
            {
                IEnrichment stepEnrichment = enrichmentNodesPair.Key;
                HashSet<XNode> nodesToEnrich = enrichmentNodesPair.Value;

                // Some of these nodes may need to not be enriched after all, to avoid singularities in the global stiffness matrix
                //HashSet<XNode> rejectedNodes = singularityResolver.FindStepEnrichedNodesToRemove(nodesToEnrich, stepEnrichment);

                // Enrich the rest of them
                //nodesToEnrich.ExceptWith(rejectedNodes);
                foreach (XNode node in nodesToEnrich) EnrichNode(node, stepEnrichment);
            }
        }
        
        private bool HasCorrespondingJunction(XNode node, StepEnrichment newEnrichment)
        {
            foreach (IEnrichment enrichment in node.Enrichments.Keys)
            {
                if (enrichment is JunctionEnrichment_v2 junctionEnrichment)
                {
                    // Also make sure the junction and step enrichment refer to the same phases.
                    Debug.Assert(newEnrichment.Phases.Count == 2); // Perhaps IEnrichment.Phases should be removed.
                    if (junctionEnrichment.IntroducesJumpBetween(newEnrichment.Phases[0], newEnrichment.Phases[1])) return true;
                }
            }
            return false;
        }

        private bool HasCorrespondingJunction(XNode node, JunctionEnrichment_v2 newEnrichment)
        {
            foreach (IEnrichment enrichment in node.Enrichments.Keys)
            {
                if (enrichment is JunctionEnrichment_v2 otherEnrichment)
                {
                    // Also make sure the junction enrichments refer to the same phases.
                    if (otherEnrichment.HasSamePhasesAs(newEnrichment)) return true;
                }
            }
            return false;
        }

        //TODO: This implementation works if there is only 1 redundant enrichment. If there are more, care should be take to
        //      choose the correct one.
        private void RemoveRedundantJunctions()
        {
            foreach (XNode node in physicalModel.Nodes)
            {
                #region debug
                //double x0 = 3.13333, y0 = -1.13333, tol = 1E-3;
                //if (Math.Abs(node.X - x0) < tol && Math.Abs(node.Y - y0) < tol)
                //{
                //    tol /= 2;
                //}
                #endregion

                var phasesOfNode = new HashSet<IPhase>();
                foreach (IXFiniteElement element in node.ElementsDictionary.Values)
                {
                    foreach (IPhase phase in element.Phases)
                    {
                        phasesOfNode.Add(phase);
                    }
                }

                int numJunctions = 0;
                foreach (IEnrichment enrichment in node.Enrichments.Keys)
                {
                    if (enrichment is JunctionEnrichment_v2) ++numJunctions;
                }

                if (numJunctions == phasesOfNode.Count)
                {
                    node.Enrichments.Remove(node.Enrichments.Keys.Last());
                }
                else if (numJunctions > phasesOfNode.Count)
                {// TODO: perhaps this is the correct solution in all cases
                    // Keep 2 enrichments for 1st junction and 1 enrichmet per junction after that
                    IEnrichment firstEnrichment = node.Enrichments.Keys.First(enr => enr is JunctionEnrichment_v2);
                    PhaseJunction firstJunction = ((JunctionEnrichment_v2)firstEnrichment).Junction;
                    var numEnrichmentsPerJunction = new Dictionary<PhaseJunction, int>();
                    var enrichmentsToRemove = new HashSet<IEnrichment>();
                    foreach (IEnrichment enrichment in node.Enrichments.Keys)
                    {
                        if (enrichment is JunctionEnrichment_v2 junctionEnrichment)
                        {
                            PhaseJunction junction = junctionEnrichment.Junction;
                            bool exists = numEnrichmentsPerJunction.TryGetValue(junction, out int count);
                            if (!exists)
                            {
                                numEnrichmentsPerJunction[junction] = 1;
                            }
                            else
                            {
                                if (junction == firstJunction)
                                {
                                    if (count >= 2) enrichmentsToRemove.Add(junctionEnrichment);
                                    else ++numEnrichmentsPerJunction[junction];
                                }
                                else
                                {
                                    if (count >= 1) enrichmentsToRemove.Add(junctionEnrichment);
                                    else ++numEnrichmentsPerJunction[junction];
                                }
                            }
                        }
                    }
                    foreach (IEnrichment enrichment in enrichmentsToRemove) node.Enrichments.Remove(enrichment);

                    //throw new NotImplementedException();
                }
            }
        }
    }
}
