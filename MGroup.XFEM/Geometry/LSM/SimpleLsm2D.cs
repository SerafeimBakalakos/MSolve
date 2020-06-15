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
    public class SimpleLsm2D : IImplicitGeometry
    {
        public SimpleLsm2D(XModel physicalModel, ICurve2D closedCurve)
        {
            NodalLevelSets = new double[physicalModel.Nodes.Count];
            for (int n = 0; n < physicalModel.Nodes.Count; ++n)
            {
                double[] node = physicalModel.Nodes[n].Coordinates;
                NodalLevelSets[n] = closedCurve.SignedDistanceOf(node);
            }
        }

        public double[] NodalLevelSets { get; }

        public IMeshTolerance MeshTolerance { get; set; } = new ArbitrarySideMeshTolerance();

        public IElementGeometryIntersection Intersect(IXFiniteElement element)
        {
            var element2D = (IXFiniteElement2D)element;
            if (IsElementDisjoint(element)) // Check this first, since it is faster and most elements are in this category 
            {
                return new NullElementIntersection2D();
            }

            double tol = MeshTolerance.CalcTolerance(element);
            var intersections = new HashSet<NaturalPoint>();
            IReadOnlyList<ElementEdge> edges = element.Edges;
            //IReadOnlyList<(XNode node1, XNode node2)> edgesCartesian = element.EdgeNodes;
            //IReadOnlyList<(NaturalPoint node1, NaturalPoint node2)> edgesNatural = element.EdgesNodesNatural;
            for (int i = 0; i < edges.Count; ++i)
            {
                XNode node0Cartesian = edges[i].Nodes[0];
                XNode node1Cartesian = edges[i].Nodes[1];
                NaturalPoint node0Natural = edges[i].NodesNatural[0];
                NaturalPoint node1Natural = edges[i].NodesNatural[1];
                double levelSet0 = CalcLevelSetNearZero(node0Cartesian, tol);
                double levelSet1 = CalcLevelSetNearZero(node1Cartesian, tol);

                if (levelSet0 * levelSet1 > 0.0) continue; // Edge is not intersected
                else if (levelSet0 * levelSet1 < 0.0) // Edge is intersected but not at its nodes
                {
                    // The intersection point between these nodes can be found using the linear interpolation, see 
                    // Sukumar 2001
                    double k = -levelSet0 / (levelSet1 - levelSet0);
                    double xi = node0Natural.Xi + k * (node1Natural.Xi - node0Natural.Xi);
                    double eta = node0Natural.Eta + k * (node1Natural.Eta - node0Natural.Eta);

                    intersections.Add(new NaturalPoint(xi, eta));
                }
                else if ((levelSet0 == 0) && (levelSet1 == 0)) // Curve is tangent to the element. Edge lies on the curve.
                {
                    //TODO: also check (DEBUG only) that all other edges are not intersected unless its is at these 2 nodes
                    return new LsmElementIntersection2D(RelativePositionCurveElement.Conforming, element2D,
                        node0Natural, node1Natural);
                }
                else if ((levelSet0 == 0) && (levelSet1 != 0)) // Curve runs through a node. Not sure if it is tangent yet.
                {
                    intersections.Add(node0Natural);
                }
                else /*if ((levelSet0 != 0) && (levelSet1 == 0))*/ // Curve runs through a node. Not sure if it is tangent yet.
                {
                    intersections.Add(node1Natural);
                }
            }

            if (intersections.Count == 1) // Curve is tangent to the element at a single node
            {
                //TODO: Make sure the intersection point is a node (debug only)
                return new NullElementIntersection2D();
            }
            else if (intersections.Count == 2)
            {
                NaturalPoint[] points = intersections.ToArray();
                return new LsmElementIntersection2D(RelativePositionCurveElement.Intersecting, element2D, points[0], points[1]);
            }
            else throw new Exception("This should not have happened");
        }

        public double SignedDistanceOf(XNode node) => NodalLevelSets[node.ID];

        public double SignedDistanceOf(XPoint point)
        {
            int[] nodes = point.Element.Nodes.Select(n => n.ID).ToArray();
            double[] shapeFunctions = point.ShapeFunctions;
            double result = 0;
            for (int n = 0; n < nodes.Length; ++n)
            {
                result += shapeFunctions[n] * NodalLevelSets[nodes[n]];
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
                double levelSet = NodalLevelSets[node.ID];
                if (levelSet < minLevelSet) minLevelSet = levelSet;
                if (levelSet > maxLevelSet) maxLevelSet = levelSet;
            }

            if (minLevelSet * maxLevelSet > 0.0) return true;
            else return false;
        }

        private double CalcLevelSetNearZero(XNode node, double zeroTolerance)
        {
            double levelSet = NodalLevelSets[node.ID];
            if (Math.Abs(levelSet) <= zeroTolerance) return 0.0;
            else return levelSet;
        }
    }
}
