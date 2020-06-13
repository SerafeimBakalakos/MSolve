using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Discretization.Mesh;
using ISAAR.MSolve.FEM.Entities;
using ISAAR.MSolve.Geometry.Coordinates;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Entities;
using MGroup.XFEM.Geometry.Primitives;
using MGroup.XFEM.Geometry.Tolerances;

//TODO: Remove duplication between this and 2D case.
namespace MGroup.XFEM.Geometry.LSM
{
    public class SimpleLsm3D : IImplicitSurface3D
    {
        public SimpleLsm3D(XModel physicalModel, ISurface3D closedSurface)
        {
            NodalLevelSets = new double[physicalModel.Nodes.Count];
            for (int n = 0; n < physicalModel.Nodes.Count; ++n)
            {
                double[] node = physicalModel.Nodes[n].Coordinates;
                NodalLevelSets[n] = closedSurface.SignedDistanceOf(node);
            }
        }

        public IMeshTolerance MeshTolerance { get; set; } = new ArbitrarySideMeshTolerance();

        public double[] NodalLevelSets { get; }

        public IElementSurfaceIntersection3D Intersect(IXFiniteElement element)
        {
            var element3D = (IXFiniteElement3D)element;
            RelativePositionCurveElement position = FindRelativePosition(element);
            if (position == RelativePositionCurveElement.Disjoint)
            {
                return new NullElementIntersection3D();
            }
            else if (position == RelativePositionCurveElement.Conforming)
            {
                // Find the nodes that lie on the surface
                var zeroNodes = new HashSet<XNode>();
                foreach (XNode node in element.Nodes)
                {
                    double distance = NodalLevelSets[node.ID];
                    if (distance == 0) zeroNodes.Add(node);
                }

                // Find which face has exactly these nodes
                foreach (ElementFace face in element3D.Faces)
                {
                    if (zeroNodes.SetEquals(face.Nodes))
                    {
                        // Intersection segment is a single cell with the same shape, nodes, etc as the face.
                        var intersectionMesh = new IntersectionMesh<NaturalPoint>();
                        intersectionMesh.AddVertices(face.NodesNatural);
                        intersectionMesh.AddCell(face.CellType, face.NodesNatural);
                    }
                }

                // At this point no face has exactly the zero nodes of the whole element.
                throw new NotImplementedException(
                    "Element marked as conforming, but the zero nodes of the element do not belong to a single face.");
            }
            else //TODO: Perhaps I should start by going through each face directly. Allow repeated operations and finally remove all duplicate points and triangles
            {

                // Find the intersection points of the surface with each edge (if they exist).
                var edgeIntersections = new Dictionary<ElementEdge, NaturalPoint[]>();
                var allIntersections = new SortedSet<NaturalPoint>(new Point3DComparer<NaturalPoint>());
                foreach (ElementEdge edge in element.Edges)
                {
                    var intersectionOfThisEdge = IntersectEdge(edge);
                    edgeIntersections[edge] = intersectionOfThisEdge;
                    allIntersections.UnionWith(intersectionOfThisEdge);
                }

                //TODO: Add optimizations for 4 and 5 intersection points
                var intersectionMesh = new IntersectionMesh<NaturalPoint>();
                if (allIntersections.Count == 3) // Intersection is a single triangle
                {
                    NaturalPoint[] triangleVertices = allIntersections.ToArray();
                    intersectionMesh.AddVertices(triangleVertices);
                    intersectionMesh.AddCell(CellType.Tri3, triangleVertices);
                    return new LsmElementIntersection3D(RelativePositionCurveElement.Intersecting, element3D, intersectionMesh);
                }
                else // General case: intersection is a mesh of triangles
                {
                    // Find their centroid
                    NaturalPoint centroid = FindIntersectionCentroid(allIntersections);
                    allIntersections.Add(centroid);

                    // Use the 2 intersection points of each face and the centroid to create a triangle of the intersection mesh
                    intersectionMesh.AddVertices(allIntersections);
                    foreach (ElementFace face in element3D.Faces)
                    {
                        var intersectionsOfFace = new SortedSet<NaturalPoint>(new Point3DComparer<NaturalPoint>());
                        foreach (ElementEdge edge in face.Edges)
                        {
                            intersectionsOfFace.UnionWith(edgeIntersections[edge]);
                            if (intersectionsOfFace.Count == 0) continue;
                            else if (intersectionsOfFace.Count == 2)
                            {
                                intersectionsOfFace.Add(centroid);
                                intersectionMesh.AddCell(CellType.Tri3, intersectionsOfFace.ToArray());
                            }
                        }
                    }
                    return new LsmElementIntersection3D(RelativePositionCurveElement.Intersecting, element3D, intersectionMesh);
                }
            }
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

        private NaturalPoint FindIntersectionCentroid(IEnumerable<NaturalPoint> intersectionPoints)
        {
            int count = 0;
            double centroidXi = 0;
            double centroidEta = 0;
            double centroidZeta = 0;
            foreach (NaturalPoint point in intersectionPoints)
            {
                ++count;
                centroidXi += point.Xi;
                centroidEta += point.Eta;
                centroidZeta += point.Zeta;
            }
            return new NaturalPoint(centroidXi / count, centroidEta / count, centroidZeta / count);
            
            //TODO: Unfortunately this centroid does not always lie on the surface. 
            //      For better accuracy, find the projection of this centroid onto the surface.
        }

        private RelativePositionCurveElement FindRelativePosition(IXFiniteElement element)
        {
            int numPositiveNodes = 0;
            int numNegativeNodes = 0;
            int numZeroNodes = 0;
            foreach (XNode node in element.Nodes)
            {
                double levelSet = NodalLevelSets[node.ID];
                if (levelSet > 0) ++numPositiveNodes;
                else if (levelSet < 0) ++numNegativeNodes;
                else ++numZeroNodes;
            }

            if ((numPositiveNodes == element.Nodes.Count) || (numNegativeNodes == element.Nodes.Count))
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
                return RelativePositionCurveElement.Disjoint;
            }
            else
            {
                // One of the element's faces conforms to the surface.
                //TODO: Assert that all zero nodes do indeed belong to the same face
                return RelativePositionCurveElement.Conforming;
            }
        }

        //TODO: Use this everywhere in this class. Alternatively, modify the nodal level sets before storing them
        private double CalcLevelSetNearZero(XNode node, double zeroTolerance) 
        {
            double levelSet = NodalLevelSets[node.ID];
            if (Math.Abs(levelSet) <= zeroTolerance) return 0.0;
            else return levelSet;
        }

        private NaturalPoint[] IntersectEdge(ElementEdge edge)
        {
            double levelSet0 = NodalLevelSets[edge.Nodes[0].ID];
            double levelSet1 = NodalLevelSets[edge.Nodes[1].ID];
            NaturalPoint node0 = edge.NodesNatural[0];
            NaturalPoint node1 = edge.NodesNatural[1];

            if (levelSet0 * levelSet1 > 0.0) return new NaturalPoint[0]; // Edge is not intersected
            else if (levelSet0 * levelSet1 < 0.0) // Edge is intersected but not at its nodes
            {
                // The intersection point between these nodes can be found using the linear interpolation, see 
                // Sukumar 2001
                double k = -levelSet0 / (levelSet1 - levelSet0);
                double xi = node0.Xi + k * (node1.Xi - node0.Xi);
                double eta = node0.Eta + k * (node1.Eta - node0.Eta);
                double zeta = node0.Zeta + k * (node1.Zeta - node0.Zeta);

                return new NaturalPoint[] { new NaturalPoint(xi, eta, zeta) };
            }
            else
            {
                // The surface runs through one or both of the nodes.
                if ((levelSet0 == 0) && (levelSet1 == 0))
                {
                    return new NaturalPoint[] { node0, node1 };
                }
                else if (levelSet0 == 0) return new NaturalPoint[] { node0 };
                else/* (levelSet1 == 0)*/ return new NaturalPoint[] { node1 };
            }
        }

        private SortedSet<NaturalPoint> IntersectFace(ElementFace face)
        {
            var intersections = new SortedSet<NaturalPoint>(new Point3DComparer<NaturalPoint>());
            foreach (ElementEdge edge in face.Edges)
            {
                NaturalPoint[] edgeIntersections = IntersectEdge(edge);
                intersections.UnionWith(edgeIntersections);
            }
            return intersections;
        }
    }
}
