using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.XFEM.Multiphase.Elements;
using ISAAR.MSolve.XFEM.Multiphase.Geometry;

//TODO: This probably does not work if an element is so large that it completely contains a phase
namespace ISAAR.MSolve.XFEM.Multiphase.Entities
{
    public class ConvexPhase : IPhase
    {
        public ConvexPhase(int id)
        {
            //if (id == DefaultPhase.DefaultPhaseID) throw new ArgumentException("Phase ID must be > 0");
            this.ID = id;
        }

        public List<PhaseBoundary> Boundaries { get; } = new List<PhaseBoundary>(4);

        public HashSet<XNode> ContainedNodes { get; } = new HashSet<XNode>();

        public HashSet<IXFiniteElement> ContainedElements { get; } = new HashSet<IXFiniteElement>();

        public int ID { get; }

        public HashSet<IXFiniteElement> IntersectedElements { get; } = new HashSet<IXFiniteElement>();

        public HashSet<IPhase> Neighbors { get; } = new HashSet<IPhase>();

        public bool Contains(CartesianPoint point)
        {
            foreach (PhaseBoundary boundary in Boundaries)
            {
                double distance = boundary.Segment.SignedDistanceOf(point);
                bool sameSide = (distance > 0) && (boundary.PositivePhase == this);
                sameSide |= (distance < 0) && (boundary.NegativePhase == this);
                if (!sameSide) return false;
            }
            return true;
        }

        public void InteractWithNodes(IEnumerable<XNode> nodes)
        {
            ContainedNodes.Clear();
            foreach (XNode node in nodes)
            {
                if (Contains(node))
                {
                    ContainedNodes.Add(node);
                    node.SurroundingPhase = this;
                }
            }
        }

        public void InteractWithElements(IEnumerable<IXFiniteElement> elements, IMeshTolerance meshTolerance)
        {
            //TODO: This does not necessarily provide correct results in coarse meshes. E.g. Scattered benchmark with 20x20 mesh


            // Only process the elements near the contained nodes. Of course not all of them will be completely inside the phase.
            IEnumerable<IXFiniteElement> nearbyElements = FindNearbyElements();
            foreach (IXFiniteElement element in nearbyElements)
            {
                bool isInside = ContainsCompletely(element);
                if (isInside)
                {
                    ContainedElements.Add(element);
                    element.Phases.Add(this);
                }
                else
                {
                    bool isIntersected = false;
                    foreach (PhaseBoundary boundary in Boundaries)
                    {
                        // This boundary-element intersection may have already been calculated from the opposite phase. 
                        if (element.PhaseIntersections.ContainsKey(boundary))
                        {
                            isIntersected = true;
                            continue;
                        }

                        CurveElementIntersection intersection = boundary.Segment.IntersectElement(element, meshTolerance);
                        if (intersection.RelativePosition == RelativePositionCurveElement.Intersection)
                        {
                            element.Phases.Add(boundary.PositivePhase);
                            element.Phases.Add(boundary.NegativePhase);
                            element.PhaseIntersections.Add(boundary, intersection);
                            isIntersected = true;
                        }
                        else if (intersection.RelativePosition == RelativePositionCurveElement.Tangent)
                        {
                            throw new NotImplementedException();
                        }
                        else if (intersection.RelativePosition != RelativePositionCurveElement.Disjoint)
                        {
                            throw new NotImplementedException();
                        }
                    }
                    if (isIntersected) IntersectedElements.Add(element);
                }
            }
        }

        private bool ContainsCompletely(IXFiniteElement element)
        {
            // The element is completely inside the phase if all its nodes are, since both the element and phase are convex.
            int numNodesInside = 0;
            int numNodesOutside = 0;
            foreach (XNode node in element.Nodes)
            {
                if (ContainedNodes.Contains(node)) ++numNodesInside;
                else ++numNodesOutside;
            }

            //TODO: Even if all nodes are outside, the element might still be intersected by a corner of the phase.
            //Debug.Assert(numNodesInside > 0); 

            if (numNodesOutside == 0) return true;
            else return false;

            #region faster
            //foreach (XNode node in element.Nodes)
            //{
            //    if (!ContainedNodes.Contains(node)) return false;
            //}
            //return true;
            #endregion
        }

        private IEnumerable<IXFiniteElement> FindNearbyElements()
        {
            var nearbyElements = new HashSet<IXFiniteElement>();

            // All elements of the contained nodes. 
            foreach (XNode node in ContainedNodes)
            {
                nearbyElements.UnionWith(node.ElementsDictionary.Values);
            }

            // However an element that is intersected by just the tip of a phase corner will not be included in the above.
            // We need another layer.
            var moreNodes = new HashSet<XNode>();
            foreach (IXFiniteElement element in nearbyElements) moreNodes.UnionWith(element.Nodes);
            foreach (XNode node in moreNodes) nearbyElements.UnionWith(node.ElementsDictionary.Values);

            return nearbyElements;
        }
    }
}
