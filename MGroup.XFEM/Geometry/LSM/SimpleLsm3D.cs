using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Discretization.Mesh;
using ISAAR.MSolve.Geometry.Coordinates;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Entities;
using MGroup.XFEM.Geometry.Primitives;
using MGroup.XFEM.Geometry.Tolerances;

//TODO: Remove duplication between this and 2D case.
namespace MGroup.XFEM.Geometry.LSM
{
    public class SimpleLsm3D : IImplicitGeometry
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

        public IElementGeometryIntersection Intersect(IXFiniteElement element)
        {
            var element3D = (IXFiniteElement3D)element;
            RelativePositionCurveElement position = FindRelativePosition(element);
            if (position == RelativePositionCurveElement.Disjoint)
            {
                return new NullElementIntersection(element);
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
                        List<double[]> nodesOfFace = face.NodesNatural.Select(p => p.Coordinates).ToList();
                        var intersectionMesh = IntersectionMesh.CreateSingleCellMesh(face.CellType, nodesOfFace);
                        return new LsmElementIntersection3D(RelativePositionCurveElement.Conforming, element3D, intersectionMesh);
                    }
                }

                // At this point no face has exactly the zero nodes of the whole element.
                throw new NotImplementedException(
                    "Element marked as conforming, but the zero nodes of the element do not belong to a single face.");
            }
            else //TODO: Perhaps I should start by going through each face directly. Allow repeated operations and finally remove all duplicate points and triangles
            {
                ElementEdge[] allEdges = element.Edges;
                ElementFace[] allFaces = element.Faces;
                var intersectionPoints = new Dictionary<double[], HashSet<ElementFace>>();

                // Find any nodes that may lie on the LSM geometry
                for (int n = 0; n < element.Nodes.Count; ++n)
                {
                    XNode node = element.Nodes[n];
                    if (NodalLevelSets[node.ID] == 0)
                    {
                        HashSet<ElementFace> facesOfNode = node.FindFacesOfNode(allFaces);
                        intersectionPoints.Add(element.Interpolation.NodalNaturalCoordinates[n], facesOfNode);
                    }
                }

                // Find intersection points that lie on element edges, excluding nodes
                foreach (ElementEdge edge in element.Edges)
                {
                    double[] intersection = IntersectEdgeExcludingNodes(edge);
                    if (intersection != null)
                    {
                        HashSet<ElementFace> facesOfEdge = edge.FindFacesOfEdge(allFaces);
                        intersectionPoints.Add(intersection, facesOfEdge);
                    }
                }

                // Create mesh
                var intersectionMesh = IntersectionMesh.CreateMultiCellMesh3D(intersectionPoints);
                return new LsmElementIntersection3D(RelativePositionCurveElement.Intersecting, element3D, intersectionMesh);
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

        public void UnionWith(IImplicitGeometry otherGeometry)
        {
            if (otherGeometry is SimpleLsm3D otherLsm)
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
        
        private double[] IntersectEdgeExcludingNodes(ElementEdge edge)
        {
            double levelSet0 = NodalLevelSets[edge.Nodes[0].ID];
            double levelSet1 = NodalLevelSets[edge.Nodes[1].ID];
            NaturalPoint node0 = edge.NodesNatural[0];
            NaturalPoint node1 = edge.NodesNatural[1];

            if (levelSet0 * levelSet1 < 0.0) // Edge is intersected but not at its nodes
            {
                // The intersection point between these nodes can be found using the linear interpolation, see 
                // Sukumar 2001
                double k = -levelSet0 / (levelSet1 - levelSet0);
                double xi = node0.Xi + k * (node1.Xi - node0.Xi);
                double eta = node0.Eta + k * (node1.Eta - node0.Eta);
                double zeta = node0.Zeta + k * (node1.Zeta - node0.Zeta);

                return new double[] { xi, eta, zeta };
            }
            else return null;
        }
    }
}
