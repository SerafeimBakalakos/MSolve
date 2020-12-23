using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Commons;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Entities;
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
            IStructuredMesh lsmMesh = dualMesh.LsmMesh;
            this.ID = id;
            NodalLevelSets = new double[lsmMesh.NumNodesTotal];
            for (int n = 0; n < NodalLevelSets.Length; ++n)
            {
                double[] node = lsmMesh.GetNodeCoordinates(lsmMesh.GetNodeIdx(n));
                NodalLevelSets[n] = closedSurface.SignedDistanceOf(node);
            }

            this.comparer = new ValueComparer(1E-6);
        }

        public double[] NodalLevelSets { get; }

        public int ID { get; }

        //TODO: How can I check and what to do if the intersection mesh or part of it conforms to the element edges?
        public IElementDiscontinuityInteraction Intersect(IXFiniteElement element)
        {
            if (IsFemElementDisjoint(element))
            {
                return new NullElementDiscontinuityInteraction(this.ID, element);
            }

            int[] lsmElementIDs = dualMesh.MapElementFemToLsm(element.ID);
            var intersectionsOfElements = new Dictionary<int, IntersectionMesh3D_NEW>();
            foreach (int lsmElementID in lsmElementIDs)
            {
                int[] lsmElementIdx = dualMesh.LsmMesh.GetElementIdx(lsmElementID);
                int[] lsmElementNodes = dualMesh.LsmMesh.GetElementConnectivity(lsmElementIdx);
                RelativePositionCurveElement position = FindRelativePosition(lsmElementNodes);
                if ((position == RelativePositionCurveElement.Disjoint) || (position == RelativePositionCurveElement.Tangent))
                {
                    // Do nothing
                }
                else if (position == RelativePositionCurveElement.Intersecting)
                {
                    IntersectionMesh3D_NEW intersectionMesh = FindInteractionIntersecting(lsmElementIdx, lsmElementNodes);
                    intersectionsOfElements[lsmElementID] = intersectionMesh;
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
            var jointIntersectionMesh = IntersectionMesh3D_NEW.JoinMeshes(intersectionsOfElements);
            return new LsmElementIntersection3D_NEW(ID, RelativePositionCurveElement.Intersecting, element, jointIntersectionMesh);
        }

        public double SignedDistanceOf(XNode node)
        {
            return NodalLevelSets[dualMesh.MapNodeIDFemToLsm(node.ID)];
        }

        public double SignedDistanceOf(XPoint point)
        {
            int femElementID = point.Element.ID;
            double[] femNaturalCoords = point.Coordinates[CoordinateSystem.ElementNatural];
            DualMeshPoint dualMeshPoint = dualMesh.CalcShapeFunctions(femElementID, femNaturalCoords);
            double[] shapeFunctions = dualMeshPoint.LsmShapeFunctions;
            int[] lsmNodes = dualMesh.LsmMesh.GetElementConnectivity(dualMeshPoint.LsmElementIdx);

            double result = 0;
            for (int n = 0; n < lsmNodes.Length; ++n)
            {
                result += shapeFunctions[n] * NodalLevelSets[lsmNodes[n]];
            }
            return result;
        }

        public void UnionWith(IClosedGeometry otherGeometry)
        {
            if (otherGeometry is DualMeshLsm3D otherLsm)
            {
                if (this.NodalLevelSets.Length != otherLsm.NodalLevelSets.Length)
                {
                    throw new ArgumentException("Incompatible Level Set geometry");
                }
                for (int i = 0; i < this.NodalLevelSets.Length; ++i)
                {
                    this.NodalLevelSets[i] = Math.Min(this.NodalLevelSets[i], otherLsm.NodalLevelSets[i]);
                }
            }
            else throw new ArgumentException("Incompatible Level Set geometry");
        }

        private IntersectionMesh3D_NEW FindInteractionIntersecting(int[] lsmElementIdx, int[] lsmNodeIDs)
        {
            var elementGeometry = new ElementHexa8Geometry_NEW();
            (ElementEdge_NEW[] edges, ElementFace_NEW[] allFaces) = elementGeometry.FindEdgesFaces(lsmNodeIDs);
            IReadOnlyList<double[]> nodesNatural = InterpolationHexa8.UniqueInstance.NodalNaturalCoordinates;

            var intersectionPoints = new Dictionary<double[], HashSet<ElementFace_NEW>>();

            // Find any nodes that may lie on the LSM geometry
            for (int n = 0; n < lsmNodeIDs.Length; ++n)
            {
                int nodeID = lsmNodeIDs[n];
                if (NodalLevelSets[nodeID] == 0)
                {
                    HashSet<ElementFace_NEW> facesOfNode = Extensions.FindFacesOfNode(nodeID, allFaces);
                    double[] intersection = nodesNatural[n];
                    if (!PointExistsAlready(intersection, intersectionPoints.Keys))
                    {
                        intersectionPoints.Add(nodesNatural[n], facesOfNode);
                    }
                }
            }

            // Find intersection points that lie on element edges, excluding nodes
            foreach (ElementEdge_NEW edge in edges)
            {
                double[] intersection = IntersectEdgeExcludingNodes(edge);
                if (intersection != null)
                {
                    if (!PointExistsAlready(intersection, intersectionPoints.Keys))
                    {
                        HashSet<ElementFace_NEW> facesOfEdge = edge.FindFacesOfEdge(allFaces);
                        intersectionPoints.Add(intersection, facesOfEdge);
                    }
                }
            }

            // Convert the coordinates of the intersection points from the natural system of the LSM element to the natural
            // system of the FEM element.
            var intersectionPointsFem = new Dictionary<double[], HashSet<ElementFace_NEW>>();
            foreach (var pair in intersectionPoints)
            {
                double[] pointLsm = pair.Key;
                double[] pointFem = dualMesh.MapPointLsmNaturalToFemNatural(lsmElementIdx, pointLsm);
                intersectionPointsFem[pointFem] = pair.Value;
            }

            // Create mesh
            return IntersectionMesh3D_NEW.CreateMultiCellMesh3D(intersectionPointsFem);
        }

        private RelativePositionCurveElement FindRelativePosition(int[] lsmElementNodes)
        {
            int numPositiveNodes = 0;
            int numNegativeNodes = 0;
            int numZeroNodes = 0;
            foreach (int nodeID in lsmElementNodes)
            {
                double levelSet = NodalLevelSets[nodeID];
                if (levelSet > 0) ++numPositiveNodes;
                else if (levelSet < 0) ++numNegativeNodes;
                else ++numZeroNodes;
            }

            if ((numPositiveNodes == lsmElementNodes.Length) || (numNegativeNodes == lsmElementNodes.Length))
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

        private double[] IntersectEdgeExcludingNodes(ElementEdge_NEW edge)
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
        private bool IsFemElementDisjoint(IXFiniteElement element)
        {
            double minLevelSet = double.MaxValue;
            double maxLevelSet = double.MinValue;

            foreach (XNode node in element.Nodes)
            {
                int lsmNodeID = dualMesh.MapNodeIDFemToLsm(node.ID);
                double levelSet = NodalLevelSets[lsmNodeID];
                if (levelSet < minLevelSet) minLevelSet = levelSet;
                if (levelSet > maxLevelSet) maxLevelSet = levelSet;
            }

            if (minLevelSet * maxLevelSet > 0.0) return true;
            else return false;
        }

        private bool PointsCoincide(double[] point0, double[] point1)
        {
            //TODO: Possibly add some tolerance
            for (int d = 0; d < dim; ++d)
            {
                if (!comparer.AreEqual(point0[d], point1[d]))
                {
                    return false;
                }
            }
            return true;
        }

        private bool PointExistsAlready(double[] newPoint, IEnumerable<double[]> currentIntersectionPoints)
        {
            foreach (double[] point in currentIntersectionPoints)
            {
                if (PointsCoincide(point, newPoint))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
