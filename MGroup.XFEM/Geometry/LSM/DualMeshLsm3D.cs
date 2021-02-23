using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Commons;
using MGroup.XFEM.ElementGeometry;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Entities;
using MGroup.XFEM.Geometry.LSM.Utilities;
using MGroup.XFEM.Geometry.Mesh;
using MGroup.XFEM.Geometry.Primitives;
using MGroup.XFEM.Interpolation;

namespace MGroup.XFEM.Geometry.LSM
{
    public class DualMeshLsm3D : IClosedGeometry
    {
        private const int dim = 3;

        private readonly DualMesh3D dualMesh;
        private readonly ValueComparer comparer;

        public DualMeshLsm3D(int id, DualMesh3D dualMesh, ISurface3D closedSurface)
        {
            this.dualMesh = dualMesh;
            IStructuredMesh fineMesh = dualMesh.FineMesh;
            this.ID = id;
            NodalLevelSets = new double[fineMesh.NumNodesTotal];
            for (int n = 0; n < NodalLevelSets.Length; ++n)
            {
                double[] node = fineMesh.GetNodeCoordinates(fineMesh.GetNodeIdx(n));
                NodalLevelSets[n] = closedSurface.SignedDistanceOf(node);
            }

            this.comparer = new ValueComparer(1E-6);
        }

        public double[] NodalLevelSets { get; }

        public int ID { get; }

        //TODO: How can I check and what to do if the intersection mesh or part of it conforms to the element edges?
        public IElementDiscontinuityInteraction Intersect(IXFiniteElement element)
        {
            if (IsCoarseElementDisjoint(element))
            {
                return new NullElementDiscontinuityInteraction(this.ID, element);
            }

            int[] fineElementIDs = dualMesh.MapElementCoarseToFine(element.ID);
            var intersectionsOfElements = new Dictionary<int, IntersectionMesh3D>();
            foreach (int fineElementID in fineElementIDs)
            {
                int[] fineElementIdx = dualMesh.FineMesh.GetElementIdx(fineElementID);
                int[] fineElementNodes = dualMesh.FineMesh.GetElementConnectivity(fineElementIdx);
                RelativePositionCurveElement position = FindRelativePosition(fineElementNodes);
                if ((position == RelativePositionCurveElement.Disjoint) || (position == RelativePositionCurveElement.Tangent))
                {
                    // Do nothing
                }
                else if (position == RelativePositionCurveElement.Intersecting)
                {
                    IntersectionMesh3D intersectionMesh = FindInteractionIntersecting(fineElementIdx, fineElementNodes);
                    intersectionsOfElements[fineElementID] = intersectionMesh;
                }
                else if (position == RelativePositionCurveElement.Conforming)
                {
                    throw new NotImplementedException();
                }
                else throw new NotImplementedException();
            }

            // Combine the line segments into a mesh
            if (intersectionsOfElements.Count == 0)
            {
                return new NullElementDiscontinuityInteraction(this.ID, element);
            }
            var jointIntersectionMesh = IntersectionMesh3D.JoinMeshes(intersectionsOfElements);
            return new LsmElementIntersection3D(ID, RelativePositionCurveElement.Intersecting, element, jointIntersectionMesh);
        }

        public double SignedDistanceOf(XNode node)
        {
            return NodalLevelSets[dualMesh.MapNodeIDCoarseToFine(node.ID)];
        }

        public double SignedDistanceOf(XPoint point)
        {
            int coarseElementID = point.Element.ID;
            double[] coarseNaturalCoords = point.Coordinates[CoordinateSystem.ElementNatural];
            DualMeshPoint dualMeshPoint = dualMesh.CalcShapeFunctions(coarseElementID, coarseNaturalCoords);
            double[] shapeFunctions = dualMeshPoint.FineShapeFunctions;
            int[] fineNodes = dualMesh.FineMesh.GetElementConnectivity(dualMeshPoint.FineElementIdx);

            double result = 0;
            for (int n = 0; n < fineNodes.Length; ++n)
            {
                result += shapeFunctions[n] * NodalLevelSets[fineNodes[n]];
            }
            return result;
        }

        public void UnionWith(IClosedGeometry otherGeometry)
        {
            if (otherGeometry is DualMeshLsm3D otherLsm)
            {
                if (this.dualMesh.CoarseMesh != otherLsm.dualMesh.CoarseMesh)
                {
                    throw new ArgumentException("The two LSM geometries refer to a different coarse mesh");
                }
                else if (this.dualMesh.FineMesh != otherLsm.dualMesh.FineMesh)
                {
                    throw new ArgumentException("The two LSM geometries refer to a different fine mesh");
                }

                for (int i = 0; i < this.NodalLevelSets.Length; ++i)
                {
                    this.NodalLevelSets[i] = Math.Min(this.NodalLevelSets[i], otherLsm.NodalLevelSets[i]);
                }
            }
            else throw new ArgumentException("Incompatible Level Set geometry");
        }

        private IntersectionMesh3D FindInteractionIntersecting(int[] fineElementIdx, int[] fineNodeIDs)
        {
            var elementGeometry = new ElementHexa8Geometry();
            (ElementEdge[] edges, ElementFace[] allFaces) = elementGeometry.FindEdgesFaces(fineNodeIDs);
            IReadOnlyList<double[]> nodesNatural = InterpolationHexa8.UniqueInstance.NodalNaturalCoordinates;

            var intersectionPoints = new List<IntersectionPoint>();

            // Find any nodes that may lie on the LSM geometry
            var comparer = new ValueComparer(1E-7);
            for (int n = 0; n < fineNodeIDs.Length; ++n)
            {
                int nodeID = fineNodeIDs[n];
                if (comparer.AreEqual(0, NodalLevelSets[nodeID]))
                {
                    var intersection = new IntersectionPoint();
                    intersection.CoordinatesNatural = nodesNatural[n];
                    if (!PointExistsAlready(intersection, intersectionPoints))
                    {
                        intersection.Faces = ElementFace.FindFacesOfNode(nodeID, allFaces);
                        intersectionPoints.Add(intersection);
                    }
                }
            }

            // Find intersection points that lie on element edges, excluding nodes
            foreach (ElementEdge edge in edges)
            {
                var intersection = new IntersectionPoint();
                intersection.CoordinatesNatural = IntersectEdgeExcludingNodes(edge);
                if (intersection.CoordinatesNatural != null)
                {
                    if (!PointExistsAlready(intersection, intersectionPoints))
                    {
                        intersection.Faces = edge.FindFacesOfEdge(allFaces);
                        intersectionPoints.Add(intersection);
                    }
                }
            }

            // Convert the coordinates of the intersection points from the natural system of the fine element to the natural
            // system of the FEM element.
            foreach (IntersectionPoint point in intersectionPoints)
            {
                double[] coordsFine = point.CoordinatesNatural;
                double[] coordsCoarse = dualMesh.MapPointFineNaturalToCoarseNatural(fineElementIdx, coordsFine);
                point.CoordinatesNatural = coordsCoarse;

            }

            // Create mesh
            return IntersectionMesh3D.CreateMultiCellMesh3D(intersectionPoints);
        }

        private RelativePositionCurveElement FindRelativePosition(int[] fineElementNodes)
        {
            int numPositiveNodes = 0;
            int numNegativeNodes = 0;
            int numZeroNodes = 0;
            foreach (int nodeID in fineElementNodes)
            {
                double levelSet = NodalLevelSets[nodeID];
                if (comparer.AreEqual(0, levelSet)) ++numZeroNodes;
                else if (levelSet > 0) ++numPositiveNodes;
                else /*if (levelSet < 0)*/ ++numNegativeNodes;
            }

            if ((numPositiveNodes == fineElementNodes.Length) || (numNegativeNodes == fineElementNodes.Length))
            {
                return RelativePositionCurveElement.Disjoint;
            }
            else if ((numPositiveNodes > 0) && (numNegativeNodes > 0))
            {
                return RelativePositionCurveElement.Intersecting;
            }
            else if (numZeroNodes < 3)
            {
                // The surface is assumed to be a plane. In rare cases, it can go through 1 node or 1 edge. 
                // Even then, no surface segment can be defined.
                //TODO: Is the assumption that surface == plane correct? 
                return RelativePositionCurveElement.Tangent;
            }
            else
            {
                // One of the element's faces conforms to the surface.
                //TODO: Assert that all zero nodes do indeed belong to the same face
                return RelativePositionCurveElement.Conforming;
            }
        }

        private double[] IntersectEdgeExcludingNodes(ElementEdge edge)
        {
            double levelSet0 = NodalLevelSets[edge.NodeIDs[0]];
            double levelSet1 = NodalLevelSets[edge.NodeIDs[1]];
            double[] node0 = edge.NodesNatural[0];
            double[] node1 = edge.NodesNatural[1];

            if (levelSet0 * levelSet1 < 0.0) // Edge is intersected but not at its nodes
            {
                // The intersection point between these nodes can be found using the linear interpolation, see 
                // Sukumar 2001
                double k = -levelSet0 / (levelSet1 - levelSet0);
                var intersection = new double[3];
                for (int d = 0; d < 3; ++d)
                {
                    intersection[d] = node0[d] + k * (node1[d] - node0[d]);
                }
                return intersection;
            }
            else return null;
        }

        /// <summary>
        /// Optimization for most elements.
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        private bool IsCoarseElementDisjoint(IXFiniteElement element)
        {
            double minLevelSet = double.MaxValue;
            double maxLevelSet = double.MinValue;

            foreach (XNode node in element.Nodes)
            {
                int fineNodeID = dualMesh.MapNodeIDCoarseToFine(node.ID);
                double levelSet = NodalLevelSets[fineNodeID];
                if (levelSet < minLevelSet) minLevelSet = levelSet;
                if (levelSet > maxLevelSet) maxLevelSet = levelSet;
            }

            if (minLevelSet * maxLevelSet > 0.0) return true;
            else return false;
        }

        private bool PointExistsAlready(IntersectionPoint newPoint, IEnumerable<IntersectionPoint> currentIntersectionPoints)
        {
            foreach (IntersectionPoint point in currentIntersectionPoints)
            {
                if (point.CoincidesWith(newPoint, comparer))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
