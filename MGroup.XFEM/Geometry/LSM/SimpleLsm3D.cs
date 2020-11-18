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
    public class SimpleLsm3D : IClosedGeometry
    {
        public SimpleLsm3D(int id, double[] nodalLevelSets)
        {
            this.ID = id;
            this.NodalLevelSets = nodalLevelSets;
        }

        public SimpleLsm3D(int id, IReadOnlyList<XNode> nodes, ISurface3D closedSurface)
        {
            this.ID = id;
            NodalLevelSets = new double[nodes.Count];
            for (int n = 0; n < nodes.Count; ++n)
            {
                double[] node = nodes[n].Coordinates;
                NodalLevelSets[n] = closedSurface.SignedDistanceOf(node);
            }
        }

        public int ID { get; }

        public IMeshTolerance MeshTolerance { get; set; } = new ArbitrarySideMeshTolerance();

        public double[] NodalLevelSets { get; protected set; }

        public virtual IElementDiscontinuityInteraction Intersect(IXFiniteElement element)
        {
            Dictionary<int, double> levelSetSubset = FindLevelSetsOfElementNodes(element, NodalLevelSets);
            RelativePositionCurveElement position = FindRelativePosition(element, levelSetSubset);
            if (position == RelativePositionCurveElement.Disjoint)
            {
                return new NullElementIntersection(ID, element);
            }
            else if (position == RelativePositionCurveElement.Conforming)
            {
                IntersectionMesh intersectionMesh = FindIntersectionConforming(element);
                return new LsmElementIntersection3D(ID, RelativePositionCurveElement.Conforming, element, intersectionMesh);
            }
            else if (position == RelativePositionCurveElement.Intersecting)
            {
                var intersectionMesh = FindIntersectionIntersecting(element, levelSetSubset);
                return new LsmElementIntersection3D(ID, RelativePositionCurveElement.Intersecting, element, intersectionMesh);
            }
            else throw new NotImplementedException();
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

        public virtual void UnionWith(IClosedGeometry otherGeometry)
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

        protected IntersectionMesh FindIntersectionConforming(IXFiniteElement element)
        {
            // Find the nodes that lie on the surface
            var zeroNodes = new HashSet<XNode>();
            foreach (XNode node in element.Nodes)
            {
                double distance = NodalLevelSets[node.ID];
                if (distance == 0) zeroNodes.Add(node);
            }

            // Find which face has exactly these nodes
            foreach (ElementFace face in element.Faces)
            {
                if (zeroNodes.SetEquals(face.Nodes))
                {
                    // Intersection segment is a single cell with the same shape, nodes, etc as the face.
                    return IntersectionMesh.CreateSingleCellMesh(face.CellType, face.NodesNatural);
                    
                }
            }

            // At this point no face has exactly the zero nodes of the whole element.
            throw new NotImplementedException(
                "Element marked as conforming, but the zero nodes of the element do not belong to a single face.");
        }

        protected IntersectionMesh FindIntersectionIntersecting(IXFiniteElement element, Dictionary<int, double> levelSetSubset)
        {
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
                double[] intersection = IntersectEdgeExcludingNodes(edge, levelSetSubset);
                if (intersection != null)
                {
                    HashSet<ElementFace> facesOfEdge = edge.FindFacesOfEdge(allFaces);
                    intersectionPoints.Add(intersection, facesOfEdge);
                }
            }

            // Create mesh
            return IntersectionMesh.CreateMultiCellMesh3D(intersectionPoints);
        }

        protected static Dictionary<int, double> FindLevelSetsOfElementNodes(IXFiniteElement element, double[] nodalLevelSets)
        {
            var levelSetSubset = new Dictionary<int, double>();
            foreach (XNode node in element.Nodes)
            {
                levelSetSubset[node.ID] = nodalLevelSets[node.ID];
            }
            return levelSetSubset;
        }

        protected static RelativePositionCurveElement FindRelativePosition(IXFiniteElement element, 
            Dictionary<int, double> levelSetSubset)
        {
            int numPositiveNodes = 0;
            int numNegativeNodes = 0;
            int numZeroNodes = 0;
            foreach (XNode node in element.Nodes)
            {
                double levelSet = levelSetSubset[node.ID];
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
        
        private double[] IntersectEdgeExcludingNodes(ElementEdge edge, Dictionary<int, double> levelSetSubset)
        {
            double levelSet0 = levelSetSubset[edge.Nodes[0].ID];
            double levelSet1 = levelSetSubset[edge.Nodes[1].ID];
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
    }
}
