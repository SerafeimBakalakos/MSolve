using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.Logging;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Geometry;
using MGroup.XFEM.Geometry.Primitives;
using MGroup.XFEM.Geometry.Tolerances;

namespace MGroup.XFEM.Entities
{
    public class LsmPhase : IPhase
    {
        private readonly GeometricModel geometricModel;

        public LsmPhase(int id, GeometricModel geometricModel)
        {
            this.ID = id;
            this.geometricModel = geometricModel;
        }

        public List<PhaseBoundary> Boundaries { get; } = new List<PhaseBoundary>();

        public HashSet<XNode> ContainedNodes { get; } = new HashSet<XNode>();

        public HashSet<IXFiniteElement> ContainedElements { get; } = new HashSet<IXFiniteElement>();

        public int ID { get; }

        public HashSet<IXFiniteElement> BoundaryElements { get; } = new HashSet<IXFiniteElement>();

        public HashSet<IPhase> Neighbors { get; } = new HashSet<IPhase>();

        public virtual bool Contains(XNode node)
        {
            Debug.Assert(Boundaries.Count == 1);
            PhaseBoundary boundary = Boundaries[0];
            double distance = boundary.Geometry.SignedDistanceOf(node);
            bool sameSide = (distance > 0) && (boundary.PositivePhase == this);
            sameSide |= (distance < 0) && (boundary.NegativePhase == this);
            if (!sameSide) return false;
            return true;
        }

        public virtual bool Contains(XPoint point)
        {
            Debug.Assert(Boundaries.Count == 1);
            PhaseBoundary boundary = Boundaries[0];
            double distance = boundary.Geometry.SignedDistanceOf(point);
            bool sameSide = (distance > 0) && (boundary.PositivePhase == this);
            sameSide |= (distance < 0) && (boundary.NegativePhase == this);
            if (!sameSide) return false;
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
                    node.Phase = this;
                }
            }
        }

        public void InteractWithElements(IEnumerable<IXFiniteElement> elements)
        {
            Debug.Assert(Boundaries.Count == 1);
            PhaseBoundary boundary = Boundaries[0];

            //TODO: This does not necessarily provide correct results in coarse meshes.

            // Only process the elements near the contained nodes. Of course not all of them will be completely inside the phase.
            IEnumerable<IXFiniteElement> nearBoundaryElements = FindNearbyElements();
            foreach (IXFiniteElement element in nearBoundaryElements)
            {
                bool isInside = ContainsCompletely(element);
                if (isInside)
                {
                    ContainedElements.Add(element);
                    element.Phases.Add(this);
                }
                else
                {
                    bool isBoundary = false;
                    // This boundary-element intersection may have already been calculated from the opposite phase. 
                    if (element.PhaseIntersections.ContainsKey(boundary))
                    {
                        isBoundary = true;
                        continue;
                    }

                    IElementGeometryIntersection intersection = boundary.Geometry.Intersect(element);
                    if (intersection.RelativePosition == RelativePositionCurveElement.Intersecting)
                    {
                        element.Phases.Add(boundary.PositivePhase);
                        element.Phases.Add(boundary.NegativePhase);
                        element.PhaseIntersections[boundary] = intersection;
                        isBoundary = true;
                    }
                    else if (intersection.RelativePosition == RelativePositionCurveElement.Conforming)
                    {
                        throw new NotImplementedException();
                    }
                    else if (intersection.RelativePosition != RelativePositionCurveElement.Disjoint)
                    {
                        throw new Exception("This should not have happenned");
                    }
                    if (isBoundary) BoundaryElements.Add(element);
                }
            }
        }

        public bool UnionWith(IPhase otherPhase)
        {
            

            if (otherPhase is LsmPhase otherLsmPhase)
            {

                if (this.Overlaps(otherPhase))
                {
                    // TODO: These should be enforced by this class.
                    if ((this.Boundaries.Count != 1) && (otherPhase.Boundaries.Count != 1))
                    {
                        throw new InvalidOperationException();
                    }
                    if (this.Boundaries[0].NegativePhase != this) throw new NotImplementedException();
                    if (otherPhase.Boundaries[0].NegativePhase != otherPhase) throw new NotImplementedException();
                    IPhase externalPhase = this.Boundaries[0].PositivePhase;
                    if (externalPhase != otherPhase.Boundaries[0].PositivePhase)
                    {
                        throw new NotImplementedException();
                    }

                    // Join level sets
                    this.Boundaries[0].Geometry.UnionWith(otherPhase.Boundaries[0].Geometry);

                    // Join boundaries
                    externalPhase.Boundaries.Remove(this.Boundaries[0]);
                    externalPhase.Boundaries.Remove(otherPhase.Boundaries[0]);
                    externalPhase.Neighbors.Remove(this);
                    externalPhase.Neighbors.Remove(otherPhase);
                    var unifiedBoundary = new PhaseBoundary(this.Boundaries[0].Geometry, externalPhase, this);

                    // Join nodes
                    foreach (XNode node in otherPhase.ContainedNodes)
                    {
                        this.ContainedNodes.Add(node);
                        node.Phase = this;
                    }

                    // Join elements
                    if ((this.BoundaryElements.Count != 0) && (otherLsmPhase.BoundaryElements.Count != 0))
                    {
                        throw new NotImplementedException();
                    }

                    return true;
                }
            }
            return false;
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

            #region this is faster, but does not take into account all cases.
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
