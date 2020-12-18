using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Commons;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Entities;
using MGroup.XFEM.Geometry.Mesh;
using MGroup.XFEM.Geometry.Primitives;

namespace MGroup.XFEM.Geometry.LSM
{
    public class DualMeshLsm2D : IClosedGeometry
    {
        private const int dim = 2;

        private readonly DualMesh2D dualMesh;
        private readonly ValueComparer comparer;

        public DualMeshLsm2D(int id, DualMesh2D dualMesh, ICurve2D closedCurve)
        {
            this.dualMesh = dualMesh;
            IStructuredMesh lsmMesh = dualMesh.LsmMesh;
            this.ID = id;
            NodalLevelSets = new double[lsmMesh.NumNodesTotal];
            for (int n = 0; n < NodalLevelSets.Length; ++n)
            {
                double[] node = lsmMesh.GetNodeCoordinates(lsmMesh.GetNodeIdx(n));
                NodalLevelSets[n] = closedCurve.SignedDistanceOf(node);
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
            var intersectionsOfElements = new Dictionary<int, List<double[]>>();
            foreach (int lsmElementID in lsmElementIDs)
            {
                var intersections = IntersectLsmElement(lsmElementID);
                if (intersections.Count == 2)
                {
                    intersectionsOfElements[lsmElementID] = intersections;
                }
            }

            // Combine the line segments into a mesh
            if (intersectionsOfElements.Count == 0)
            {
                return new NullElementDiscontinuityInteraction(this.ID, element);
            }
            var mesh = IntersectionMesh2D_NEW.CreateMesh(intersectionsOfElements);
            return new LsmElementIntersection2D_NEW(this.ID, RelativePositionCurveElement.Intersecting, element, mesh);
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
            if (otherGeometry is DualMeshLsm2D otherLsm)
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

        private List<double[]> IntersectLsmElement(int lsmElementID)
        {
            int[] lsmElementIdx = dualMesh.LsmMesh.GetElementIdx(lsmElementID);
            int[] lsmNodes = dualMesh.LsmMesh.GetElementConnectivity(lsmElementIdx);

            var intersections = new List<double[]>();
            if (IsLsmElementDisjoint(lsmElementIdx)) // Check this first, since it is faster and most elements are in this category 
            {
                return intersections;
            }

            var elementGeometry = new ElementQuad4Geometry_NEW();
            (ElementEdge_NEW[] edges, _)  = elementGeometry.FindEdgesFaces(lsmNodes);
            for (int i = 0; i < edges.Length; ++i)
            {
                int node0ID = edges[i].NodeIDs[0];
                int node1ID = edges[i].NodeIDs[1];
                double[] node0Natural = edges[i].NodesNatural[0];
                double[] node1Natural = edges[i].NodesNatural[1];
                double levelSet0 = NodalLevelSets[node0ID];
                double levelSet1 = NodalLevelSets[node1ID];

                if (levelSet0 * levelSet1 > 0.0) continue; // Edge is not intersected
                else if (levelSet0 * levelSet1 < 0.0) // Edge is intersected but not at its nodes
                {
                    // The intersection point between these nodes can be found using the linear interpolation, see 
                    // Sukumar 2001
                    double k = -levelSet0 / (levelSet1 - levelSet0);
                    double xi = node0Natural[0] + k * (node1Natural[0] - node0Natural[0]);
                    double eta = node0Natural[1] + k * (node1Natural[1] - node0Natural[1]);

                    AddPossiblyDuplicateIntersectionPoint(new double[] { xi, eta }, intersections);
                }
                else if ((levelSet0 == 0) && (levelSet1 == 0)) // This edge of the element conforms to the curve.
                {
                    throw new NotImplementedException();
                    //TODO: also check (DEBUG only) that all other edges are not intersected unless its is at these 2 nodes
                    //return new LsmElementIntersection2D(ID, RelativePositionCurveElement.Conforming, element,
                    //    node0Natural, node1Natural);
                }
                else if ((levelSet0 == 0) && (levelSet1 != 0)) // Curve runs through a node. Not sure if it is tangent yet.
                {
                    // Check if this node is already added. If not add it.
                    AddPossiblyDuplicateIntersectionPoint(node0Natural, intersections);
                }
                else /*if ((levelSet0 != 0) && (levelSet1 == 0))*/ // Curve runs through a node. Not sure if it is tangent yet.
                {
                    // Check if this node is already added. If not add it.
                    AddPossiblyDuplicateIntersectionPoint(node1Natural, intersections);
                }
            }

            // Convert the coordinates of the intersection points from the natural system of the LSM element to the natural
            // system of the FEM element.
            for (int p = 0; p < intersections.Count; ++p)
            {
                intersections[p] = dualMesh.MapPointLsmNaturalToFemNatural(lsmElementIdx, intersections[p]);
            }
            return intersections;
        }

        private void AddPossiblyDuplicateIntersectionPoint(double[] newPoint, List<double[]> currentIntersectionPoints)
        {
            foreach (double[] point in currentIntersectionPoints)
            {
                if (PointsCoincide(point, newPoint))
                {
                    return;
                }
            }
            currentIntersectionPoints.Add(newPoint); // If this code is reached, then the new point has no duplicate.
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

        /// <summary>
        /// Optimization for most elements.
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        private bool IsLsmElementDisjoint(int[] elementIdx)
        {
            double minLevelSet = double.MaxValue;
            double maxLevelSet = double.MinValue;

            int[] lsmNodes = dualMesh.LsmMesh.GetElementConnectivity(elementIdx);
            foreach (int nodeId in lsmNodes)
            {
                double levelSet = NodalLevelSets[nodeId];
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
    }
}
