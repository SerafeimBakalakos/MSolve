using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using ISAAR.MSolve.Discretization.Integration;
using ISAAR.MSolve.Discretization.Integration.Quadratures;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.Geometry.Shapes;
using ISAAR.MSolve.XFEM.Thermal.Elements;
using ISAAR.MSolve.XFEM.Thermal.Entities;
using ISAAR.MSolve.XFEM.Thermal.LevelSetMethod.MeshInteraction;

namespace ISAAR.MSolve.XFEM.Thermal.LevelSetMethod
{
    public class SimpleLsmCurve2D : ILsmCurve2D
    {
        private readonly Dictionary<XNode, double> levelSets;

        public SimpleLsmCurve2D(double interfaceThickness = 1.0)
        {
            this.levelSets = new Dictionary<XNode, double>();
            this.Thickness = interfaceThickness;
        }

        public double Thickness { get; }

        public void InitializeGeometry(IEnumerable<XNode> nodes, ICurve2D discontinuity)
        {
            foreach (XNode node in nodes) levelSets[node] = discontinuity.SignedDistanceOf(node);
        }

        //TODO: This only works for Tri3 and Quad4 elements
        public CurveElementIntersection IntersectElement(IXFiniteElement element)
        {
            // TODO: perhaps some tolerance is needed for the cases levelSet[node] == 0.

            if (IsElementDisjoint(element)) // Check this first, since it is faster and most elements are in this category 
            {
                return new CurveElementIntersection(RelativePositionCurveElement.Disjoint, new NaturalPoint[0], new XNode[0]);
            }

            var intersections = new HashSet<NaturalPoint>();
            IReadOnlyList<(XNode node1, XNode node2)> edgesCartesian = element.EdgeNodes;
            IReadOnlyList<(NaturalPoint node1, NaturalPoint node2)> edgesNatural = element.EdgesNodesNatural;
            for (int i = 0; i < edgesCartesian.Count; ++i)
            {
                XNode node1Cartesian = edgesCartesian[i].node1;
                XNode node2Cartesian = edgesCartesian[i].node2;
                NaturalPoint node1Natural = edgesNatural[i].node1;
                NaturalPoint node2Natural = edgesNatural[i].node2;

                double levelSet1 = levelSets[node1Cartesian];
                double levelSet2 = levelSets[node2Cartesian];

                if (levelSet1 * levelSet2 > 0.0) continue; // Edge is not intersected
                else if (levelSet1 * levelSet2 < 0.0) // Edge is intersected but not at its nodes
                {
                    // The intersection point between these nodes can be found using the linear interpolation, see 
                    // Sukumar 2001
                    double k = -levelSet1 / (levelSet2 - levelSet1);
                    double xi = node1Natural.Xi + k * (node2Natural.Xi - node1Natural.Xi);
                    double eta = node1Natural.Eta + k * (node2Natural.Eta - node1Natural.Eta);

                    intersections.Add(new NaturalPoint(xi, eta));
                }
                else if ((levelSet1 == 0) && (levelSet2 == 0)) // Curve is tangent to the element. Edge lies on the curve.
                {
                    //TODO: also check (DEBUG only) that all other edges are not intersected unless its is at these 2 nodes
                    return new CurveElementIntersection(RelativePositionCurveElement.TangentAlongElementEdge,
                        new NaturalPoint[] { node1Natural, node2Natural }, new XNode[] { node1Cartesian, node2Cartesian });
                }
                else if ((levelSet1 == 0) && (levelSet2 != 0)) // Curve runs through a node. Not sure if it is tangent yet.
                {
                    intersections.Add(node1Natural); 
                }
                else /*if ((levelSet1 != 0) && (levelSet2 == 0))*/ // Curve runs through a node. Not sure if it is tangent yet.
                {
                    intersections.Add(node2Natural);
                }
            }

            if (intersections.Count == 1) // Curve is tangent to the element at a single node
            {
                NaturalPoint intersectionPoint = intersections.First();
                XNode contactNode = null;
                IReadOnlyList<NaturalPoint> nodesNatural = element.StandardInterpolation.NodalNaturalCoordinates;
                for (int i = 0; i < element.Nodes.Count; ++i)
                {
                    if (nodesNatural[i] == intersectionPoint)
                    {
                        contactNode = element.Nodes[i];
                        break;
                    }
                }
                Debug.Assert(contactNode != null, "Curve is tangent at a point that is not a node");

                return new CurveElementIntersection(RelativePositionCurveElement.TangentAtSingleNode, 
                    new NaturalPoint[] { intersectionPoint }, new XNode[] { contactNode });
            }
            else if (intersections.Count == 2)
            {
                return new CurveElementIntersection(RelativePositionCurveElement.Intersection, 
                    intersections.ToArray(), new XNode[0]);
            }
            else throw new Exception("This should not have happened");
        }

        public double SignedDistanceOf(XNode node) => levelSets[node];

        public double SignedDistanceOf(IXFiniteElement element, double[] shapeFunctionsAtNaturalPoint)
        {
            double signedDistance = 0.0;
            for (int n = 0; n < element.Nodes.Count; ++n)
            {
                signedDistance += shapeFunctionsAtNaturalPoint[n] * levelSets[element.Nodes[n]];
            }
            return signedDistance;
        }

        private bool IsElementDisjoint(IXFiniteElement element)
        {
            double minLevelSet = double.MaxValue;
            double maxLevelSet = double.MinValue;

            foreach (XNode node in element.Nodes)
            {
                double levelSet = levelSets[node];
                if (levelSet < minLevelSet) minLevelSet = levelSet;
                if (levelSet > maxLevelSet) maxLevelSet = levelSet;
            }

            if (minLevelSet * maxLevelSet > 0.0) return true;
            else return false;
        }
    }
}
