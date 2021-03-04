using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using ISAAR.MSolve.Discretization.Mesh;
using MGroup.XFEM.ElementGeometry;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Entities;
using MGroup.XFEM.Geometry.LSM.Utilities;
using MGroup.XFEM.Geometry.Primitives;

//TODO: Implement the cases, where the body level set or its extension go through an single node or edge without having any
//      more common points with an element.
namespace MGroup.XFEM.Geometry.LSM
{
    /// <summary>
    /// Based on "Non-planar 3D crack growth by the extended finite element and level sets—Part II: Level set update, 2002, 
    /// Gravouli et al.:.
    /// </summary>
    public class OpenLsm3D : IOpenLsmGeometry
    {
        public OpenLsm3D(int id)
        {
            this.ID = id;
        }

        public int ID { get; }

        Dictionary<int, double> ILsmGeometry.LevelSets => LevelSetsBody;
        public Dictionary<int, double> LevelSetsBody { get; } = new Dictionary<int, double>();
        public Dictionary<int, double> LevelSetsTip { get; } = new Dictionary<int, double>();

        public IElementOpenGeometryInteraction Intersect(IXFiniteElement element)
        {
            var elementLevelSets = new ElementLevelSets(element, LevelSetsBody, LevelSetsTip);

            // Check this first, since it is faster and most elements belong to this category 
            if (IsElementDisjoint(element, elementLevelSets))
            {
                return new NullElementDiscontinuityInteraction(ID, element);
            }

            // Determine whether the LSM geometry conforms to an element face or if it intersects it
            // Also find these intersection points (nodes in the conforming case) and the mesh they create (2D mesh in 3D space).
            IntersectionMesh3D intersectionMesh;
            List<IntersectionPoint> intersectionPoints;
            bool isConforming = 
                TryFindInteractionConforming(element, elementLevelSets, out intersectionPoints, out intersectionMesh);
            if (!isConforming)
            {
                (intersectionPoints, intersectionMesh) = FindInteractionIntersecting(element, elementLevelSets);
            }

            if (intersectionPoints.Count < 3) // The only common points are an edge or a node 
            {
                throw new NotImplementedException();
            }
            else
            {
                // Find min, max tip level sets of intersection points
                double minIntersectionPsi = double.MaxValue;
                double maxIntersectionPsi = double.MinValue;
                foreach (IntersectionPoint intersection in intersectionPoints)
                {
                    if (intersection.TipLevelSet < minIntersectionPsi) minIntersectionPsi = intersection.TipLevelSet;
                    if (intersection.TipLevelSet > maxIntersectionPsi) maxIntersectionPsi = intersection.TipLevelSet;
                }

                // Based on these min, max determine whether the element contains the crack front
                if (minIntersectionPsi > 0)
                {
                    // All points lie on the extension of the crack beyond its front
                    return new NullElementDiscontinuityInteraction(this.ID, element);
                }
                else if (maxIntersectionPsi < 0)
                {
                    // All points lie on the crack surface before the crack front
                    var pos = isConforming ? RelativePositionCurveElement.Conforming : RelativePositionCurveElement.Intersecting;
                    return new OpenLsmElementIntersection3D(this.ID, element, pos, false, intersectionMesh);
                }
                else
                {
                    // The crack front lies inside the element
                    // Take the intersection the mesh between the element and phi=0 and intersect it again with psi=0.
                    Plane3D psi0Plane = IntersectElementWithTipLevelSet(element, elementLevelSets);
                    var intersector = new IntersectionMesh3DIntersector(intersectionMesh, psi0Plane);
                    IntersectionMesh3D finalMesh = intersector.IntersectMesh();

                    var pos = isConforming ? RelativePositionCurveElement.Conforming : RelativePositionCurveElement.Intersecting;
                    return new OpenLsmElementIntersection3D(this.ID, element, pos, true, finalMesh);
                }

            }
        }

        public double SignedDistanceOf(XNode node) => LevelSetsBody[node.ID];

        public double SignedDistanceOf(XPoint point)
        {
            IReadOnlyList<XNode> nodes = point.Element.Nodes;
            double signedDistance = 0.0;
            for (int n = 0; n < nodes.Count; ++n)
            {
                signedDistance += point.ShapeFunctions[n] * LevelSetsBody[nodes[n].ID];
            }
            return signedDistance;
        }

        public double[] SignedDistanceGradientThrough(XPoint point)
        {
            IReadOnlyList<XNode> nodes = point.Element.Nodes;
            double gradientX = 0.0;
            double gradientY = 0.0;
            double gradientZ = 0.0;
            for (int n = 0; n < nodes.Count; ++n)
            {
                double dNdx = point.ShapeFunctionDerivatives[n, 0];
                double dNdy = point.ShapeFunctionDerivatives[n, 1];
                double dNdz = point.ShapeFunctionDerivatives[n, 2];

                double levelSet = LevelSetsBody[nodes[n].ID];
                gradientX += dNdx * levelSet;
                gradientY += dNdy * levelSet;
                gradientZ += dNdz * levelSet;
            }
            return new double[] { gradientX, gradientY, gradientZ };
        }

        private (List<IntersectionPoint> intersectionPoints, IntersectionMesh3D intersectionMesh) 
            FindInteractionIntersecting(IXFiniteElement element, ElementLevelSets elementLevelSets)
        {
            ElementFace[] allFaces = element.Faces;
            var intersectionPoints = new List<IntersectionPoint>();

            // Find any nodes that may lie on phi=0
            for (int n = 0; n < element.Nodes.Count; ++n)
            {
                XNode node = element.Nodes[n];
                if (elementLevelSets.BodyLevelSets[node.ID] == 0)
                {
                    var intersection = new IntersectionPoint();
                    intersection.CoordinatesNatural = element.Interpolation.NodalNaturalCoordinates[n];
                    intersection.Faces = ElementFace.FindFacesOfNode(node.ID, allFaces);
                    intersectionPoints.Add(intersection);
                }
            }

            // Find intersection points that lie on element edges, excluding nodes
            foreach (ElementEdge edge in element.Edges)
            {
                IntersectionPoint intersection =
                    IntersectEdgeExcludingNodesWithBodyLevelSet(edge, elementLevelSets);
                if (intersection != null)
                {
                    intersection.Faces = edge.FindFacesOfEdge(allFaces);
                    intersectionPoints.Add(intersection);
                }
            }

            // Create mesh
            var intersectionMesh = IntersectionMesh3D.CreateMultiCellMesh3D(intersectionPoints);
            return (intersectionPoints, intersectionMesh);
        }

        private IntersectionPoint IntersectEdgeExcludingNodesWithBodyLevelSet(ElementEdge edge,
            ElementLevelSets elementLevelSets)
        {
            double[] node0 = edge.NodesNatural[0];
            double[] node1 = edge.NodesNatural[1];
            double phi0 = elementLevelSets.BodyLevelSets[edge.NodeIDs[0]];
            double phi1 = elementLevelSets.BodyLevelSets[edge.NodeIDs[1]];
            double psi0 = elementLevelSets.TipLevelSets[edge.NodeIDs[0]];
            double psi1 = elementLevelSets.TipLevelSets[edge.NodeIDs[1]];

            if (phi0 * phi1 < 0.0) // Edge is intersected but not at its nodes
            {
                // The intersection point between these nodes can be found using the linear interpolation, see 
                // Sukumar 2001
                double k = -phi0 / (phi1 - phi0);
                var intersectionNatural = new double[3];
                for (int d = 0; d < 3; ++d)
                {
                    intersectionNatural[d] = node0[d] + k * (node1[d] - node0[d]);
                }
                double psi = psi0 + k * (psi1 - psi0);

                return new IntersectionPoint()
                {
                    CoordinatesNatural = intersectionNatural,
                    Edge = edge,
                    TipLevelSet = psi
                };
            }
            else return null;
        }

        //TODO: This operates on the knowledge that the IntersectionMesh3D will take all IntersectionPoint and use its
        //      IntersectionPoint.CoordinatesNatural as vertices, without adding new vertices or copying the underlying 
        //      double[] arrays. This is not safe at all.
        private Plane3D IntersectElementWithTipLevelSet(IXFiniteElement element, ElementLevelSets elementLevelSets)
        {
            // Using the psi level sets of the intersection points to further intersect the triangles of the intersection mesh 
            // would cause inaccuracies due to them being calculated from the curved surface. Instead find the plane defined as 
            // the intersection of psi=0 and the element and use that to calculated signed distances.
            var intersections = new List<double[]>();

            // Find any nodes that may lie on psi=0
            for (int n = 0; n < element.Nodes.Count; ++n)
            {
                XNode node = element.Nodes[n];
                if (elementLevelSets.TipLevelSets[node.ID] == 0)
                {
                    intersections.Add(element.Interpolation.NodalNaturalCoordinates[n]);
                }
            }

            // Find intersection points that lie on element edges, excluding nodes
            foreach (ElementEdge edge in element.Edges)
            {
                double[] node0 = edge.NodesNatural[0];
                double[] node1 = edge.NodesNatural[1];
                double psi0 = elementLevelSets.TipLevelSets[edge.NodeIDs[0]];
                double psi1 = elementLevelSets.TipLevelSets[edge.NodeIDs[1]];

                if (psi0 * psi1 < 0.0) // Edge is intersected but not at its nodes
                {
                    // The intersection point between these nodes can be found using the linear interpolation, see 
                    // Sukumar 2001
                    double k = -psi0 / (psi1 - psi0);
                    var intersectionNatural = new double[3];
                    for (int d = 0; d < 3; ++d)
                    {
                        intersectionNatural[d] = node0[d] + k * (node1[d] - node0[d]);
                    }

                    intersections.Add(intersectionNatural);
                }
            }

            // Find a point with psi>0 or psi<0 to define the positive and negative halfspaces. 
            // Preferably the node with max |psi|
            double[] pointOffPlane = null;
            double signedDistanceOfPointOffPlane = 0.0;
            for (int n = 0; n < element.Nodes.Count; ++n)
            {
                XNode node = element.Nodes[n];
                if (Math.Abs(elementLevelSets.TipLevelSets[node.ID]) > Math.Abs(signedDistanceOfPointOffPlane))
                {
                    signedDistanceOfPointOffPlane = elementLevelSets.TipLevelSets[node.ID];
                    pointOffPlane = element.Interpolation.NodalNaturalCoordinates[n];
                }
            }
            Debug.Assert(pointOffPlane != null);
            Debug.Assert(signedDistanceOfPointOffPlane != 0.0);

            // Find a plane that goes through these intersection points and has a unit nomal directed towards psi>0 
            var plane = Plane3D.FitPlaneThroughPoints(intersections, pointOffPlane, signedDistanceOfPointOffPlane);

            return plane;
        }

        /// <summary>
        /// Optimization for most elements. It is possible for this method to return false, even if the element is disjoint.
        /// </summary>
        /// <param name="element"></param>
        private static bool IsElementDisjoint(IXFiniteElement element, ElementLevelSets elementLevelSets)
        {
            double minBodyLS = double.MaxValue;
            double maxBodyLS = double.MinValue;
            double minTipLS = double.MaxValue;

            foreach (XNode node in element.Nodes)
            {
                double bodyLS = elementLevelSets.BodyLevelSets[node.ID];
                if (bodyLS < minBodyLS) minBodyLS = bodyLS;
                if (bodyLS > maxBodyLS) maxBodyLS = bodyLS;

                double tipLS = elementLevelSets.TipLevelSets[node.ID];
                if (tipLS < minTipLS) minTipLS = tipLS;
            }

            if (minBodyLS * maxBodyLS > 0.0) return true;
            else if (minTipLS > 0.0) return true;
            else return false;
        }

        private bool TryFindInteractionConforming(IXFiniteElement element, ElementLevelSets elementLevelSets,
            out List<IntersectionPoint> intersectionPoints, out IntersectionMesh3D intersectionMesh)
        {
            // Find the nodes that lie on phi=0
            var zeroNodes = new HashSet<int>();
            foreach (XNode node in element.Nodes)
            {
                double distance = elementLevelSets.BodyLevelSets[node.ID];
                if (distance == 0) zeroNodes.Add(node.ID);
            }

            // Find which face has exactly these nodes
            intersectionPoints = null;
            intersectionMesh = null;
            foreach (ElementFace face in element.Faces)
            {
                if (zeroNodes.SetEquals(face.NodeIDs))
                {
                    intersectionPoints = new List<IntersectionPoint>();
                    for (int i = 0; i < element.Nodes.Count; ++i)
                    {
                        var intersection = new IntersectionPoint();
                        intersection.CoordinatesNatural = face.NodesNatural[i];
                        intersection.TipLevelSet = elementLevelSets.TipLevelSets[element.Nodes[i].ID];
                        intersectionPoints.Add(intersection);
                    }

                    intersectionMesh = IntersectionMesh3D.CreateTriagleMeshForElementFace(face.CellType, face.NodesNatural);
                    return true;
                }
            }

            // At this point no face has exactly the zero nodes of the whole element.
            return false;
        }
    }
}
