using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using ISAAR.MSolve.Geometry.Coordinates;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Geometry;
using MGroup.XFEM.Geometry.Primitives;
using MGroup.XFEM.Geometry.Tolerances;

//TODO: This probably does not work if an element is so large that it completely contains a phase
namespace MGroup.XFEM.Entities
{
    public class ConvexPhase2D : IPhase
    {
        private readonly GeometricModel2D geometricModel;

        public ConvexPhase2D(int id, GeometricModel2D geometricModel)
        {
            this.ID = id;
            this.geometricModel = geometricModel;
        }

        public List<PhaseBoundary2D> Boundaries { get; } = new List<PhaseBoundary2D>();

        public HashSet<XNode> ContainedNodes { get; } = new HashSet<XNode>();

        public HashSet<IXFiniteElement> ContainedElements { get; } = new HashSet<IXFiniteElement>();

        public int ID { get; }

        public HashSet<IXFiniteElement> BoundaryElements { get; } = new HashSet<IXFiniteElement>();

        public HashSet<IPhase> Neighbors { get; } = new HashSet<IPhase>();

        public virtual bool Contains(XNode node)
        {
            foreach (PhaseBoundary2D boundary in Boundaries)
            {
                double distance = boundary.Geometry.SignedDistanceOf(node);
                bool sameSide = (distance > 0) && (boundary.PositivePhase == this);
                sameSide |= (distance < 0) && (boundary.NegativePhase == this);
                if (!sameSide) return false;
            }
            return true;
        }

        public virtual bool Contains(XPoint point)
        {
            foreach (PhaseBoundary2D boundary in Boundaries)
            {
                double distance = boundary.Geometry.SignedDistanceOf(point);
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
                    geometricModel.AddPhaseToNode(node, this);
                }
            }
        }

        public void InteractWithElements(IEnumerable<IXFiniteElement> elements)
        {
            //TODO: This does not necessarily provide correct results in coarse meshes.

            // Only process the elements near the contained nodes. Of course not all of them will be completely inside the phase.
            IEnumerable<IXFiniteElement> nearBoundaryElements = FindNearbyElements();
            foreach (IXFiniteElement element in nearBoundaryElements)
            {
                bool isInside = ContainsCompletely(element);
                if (isInside)
                {
                    ContainedElements.Add(element);
                    geometricModel.AddPhaseToElement(element, this);
                }
                else
                {
                    bool isBoundary = false;
                    foreach (PhaseBoundary2D boundary in Boundaries)
                    {
                        // This boundary-element intersection may have already been calculated from the opposite phase. 
                        if (geometricModel.GetPhaseBoundariesOfElement(element).ContainsKey(boundary))
                        {
                            isBoundary = true;
                            continue;
                        }

                        IElementCurveIntersection2D intersection = boundary.Geometry.Intersect(element);
                        if (intersection.RelativePosition == RelativePositionCurveElement.Intersecting)
                        {
                            geometricModel.AddPhaseToElement(element, boundary.PositivePhase);
                            geometricModel.AddPhaseToElement(element, boundary.NegativePhase);
                            geometricModel.AddPhaseBoundaryToElement(element, boundary, intersection);
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
                    }
                    if (isBoundary) BoundaryElements.Add(element);
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
