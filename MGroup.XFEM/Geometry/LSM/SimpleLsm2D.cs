using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ISAAR.MSolve.FEM.Entities;
using ISAAR.MSolve.Geometry.Coordinates;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Entities;
using MGroup.XFEM.Geometry.Primitives;
using MGroup.XFEM.Geometry.Tolerances;

namespace MGroup.XFEM.Geometry.LSM
{
    public class SimpleLsm2D : IImplicitCurve2D
    {
        public SimpleLsm2D(XModel physicalModel, ICurve2D closedCurve)
        {
            NodalValues = new double[physicalModel.Nodes.Count];
            for (int n = 0; n < physicalModel.Nodes.Count; ++n)
            {
                double[] node = physicalModel.Nodes[n].Coordinates;
                NodalValues[n] = closedCurve.SignedDistanceOf(node);
            }
        }

        public double[] NodalValues { get; }

        public IMeshTolerance MeshTolerance { get; set; } = new ArbitrarySideMeshTolerance();

        public IElementCurveIntersection2D Intersect(IXFiniteElement element)
        {
            if (IsElementDisjoint(element)) // Check this first, since it is faster and most elements are in this category 
            {
                return new NullElementCurveIntersection2D();
            }

            double tol = MeshTolerance.CalcTolerance(element);
            var intersections = new HashSet<NaturalPoint>();
            IReadOnlyList<(XNode node1, XNode node2)> edgesCartesian = element.EdgeNodes;
            IReadOnlyList<(NaturalPoint node1, NaturalPoint node2)> edgesNatural = element.EdgesNodesNatural;
            for (int i = 0; i < edgesCartesian.Count; ++i)
            {
                XNode node1Cartesian = edgesCartesian[i].node1;
                XNode node2Cartesian = edgesCartesian[i].node2;
                NaturalPoint node1Natural = edgesNatural[i].node1;
                NaturalPoint node2Natural = edgesNatural[i].node2;

                //TODO: Add tolerance control or modify the level sets directly when I first calculate them
                double levelSet1 = CalcLevelSetNearZero(node1Cartesian, tol);
                double levelSet2 = CalcLevelSetNearZero(node2Cartesian, tol);

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
                    return new LsmElementIntersection2D(RelativePositionCurveElement.Conforming, element,
                        node1Natural, node2Natural);
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
                //TODO: Make sure the intersection point is a node (debug only)
                return new NullElementCurveIntersection2D();
            }
            else if (intersections.Count == 2)
            {
                NaturalPoint[] points = intersections.ToArray();
                return new LsmElementIntersection2D(RelativePositionCurveElement.Intersecting, element, points[0], points[1]);
            }
            else throw new Exception("This should not have happened");
        }

        public double SignedDistanceOf(XNode node) => NodalValues[node.ID];

        public double SignedDistanceOf(XPoint point)
        {
            int[] nodes = point.Element.Nodes.Select(n => n.ID).ToArray();
            double[] shapeFunctions = point.ShapeFunctions;
            double result = 0;
            for (int n = 0; n < nodes.Length; ++n)
            {
                result += shapeFunctions[n] * NodalValues[n];
            }
            return result;
        }

        /// <summary>
        /// Optimization for most elements.
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        private bool IsElementDisjoint(IXFiniteElement element)
        {
            double minLevelSet = double.MaxValue;
            double maxLevelSet = double.MinValue;

            foreach (XNode node in element.Nodes)
            {
                double levelSet = NodalValues[node.ID];
                if (levelSet < minLevelSet) minLevelSet = levelSet;
                if (levelSet > maxLevelSet) maxLevelSet = levelSet;
            }

            if (minLevelSet * maxLevelSet > 0.0) return true;
            else return false;
        }

        private double CalcLevelSetNearZero(XNode node, double zeroTolerance)
        {
            double levelSet = NodalValues[node.ID];
            if (Math.Abs(levelSet) <= zeroTolerance) return 0.0;
            else return levelSet;
        }
    }
}
