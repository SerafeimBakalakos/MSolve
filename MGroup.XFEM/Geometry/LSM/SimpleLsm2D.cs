﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ISAAR.MSolve.Geometry.Coordinates;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Entities;
using MGroup.XFEM.Geometry.Primitives;
using MGroup.XFEM.Geometry.Tolerances;

namespace MGroup.XFEM.Geometry.LSM
{
    public class SimpleLsm2D : IImplicitGeometry
    {
        public SimpleLsm2D(int id, double[] nodalLevelSets)
        {
            this.ID = id;
            this.NodalLevelSets = nodalLevelSets;
        }

        public SimpleLsm2D(int id, XModel physicalModel, ICurve2D closedCurve)
        {
            this.ID = id;
            NodalLevelSets = new double[physicalModel.Nodes.Count];
            for (int n = 0; n < physicalModel.Nodes.Count; ++n)
            {
                double[] node = physicalModel.Nodes[n].Coordinates;
                NodalLevelSets[n] = closedCurve.SignedDistanceOf(node);
            }
        }

        public int ID { get; }

        public double[] NodalLevelSets { get; }

        public IMeshTolerance MeshTolerance { get; set; } = new ArbitrarySideMeshTolerance();

        public virtual IElementGeometryIntersection Intersect(IXFiniteElement element)
        {
            if (IsElementDisjoint(element)) // Check this first, since it is faster and most elements are in this category 
            {
                return new NullElementIntersection(ID, element);
            }

            double tol = MeshTolerance.CalcTolerance(element);
            var intersections = new HashSet<double[]>();
            IReadOnlyList<ElementEdge> edges = element.Edges;
            for (int i = 0; i < edges.Count; ++i)
            {
                XNode node0Cartesian = edges[i].Nodes[0];
                XNode node1Cartesian = edges[i].Nodes[1];
                double[] node0Natural = edges[i].NodesNatural[0];
                double[] node1Natural = edges[i].NodesNatural[1];
                double levelSet0 = CalcLevelSetNearZero(node0Cartesian, tol);
                double levelSet1 = CalcLevelSetNearZero(node1Cartesian, tol);

                if (levelSet0 * levelSet1 > 0.0) continue; // Edge is not intersected
                else if (levelSet0 * levelSet1 < 0.0) // Edge is intersected but not at its nodes
                {
                    // The intersection point between these nodes can be found using the linear interpolation, see 
                    // Sukumar 2001
                    double k = -levelSet0 / (levelSet1 - levelSet0);
                    double xi = node0Natural[0] + k * (node1Natural[0] - node0Natural[0]);
                    double eta = node0Natural[1] + k * (node1Natural[1] - node0Natural[1]);

                    intersections.Add(new double[] { xi, eta });
                }
                else if ((levelSet0 == 0) && (levelSet1 == 0)) // Curve is tangent to the element. Edge lies on the curve.
                {
                    //TODO: also check (DEBUG only) that all other edges are not intersected unless its is at these 2 nodes
                    return new LsmElementIntersection2D(ID, RelativePositionCurveElement.Conforming, element,
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
                return new NullElementIntersection(ID, element);
            }
            else if (intersections.Count == 2)
            {
                double[][] points = intersections.ToArray();
                return new LsmElementIntersection2D(ID, RelativePositionCurveElement.Intersecting, 
                    element, points[0], points[1]);
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

        public virtual void UnionWith(IImplicitGeometry otherGeometry)
        {
            if (otherGeometry is SimpleLsm2D otherLsm)
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