//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.Text;
//using MGroup.XFEM.ElementGeometry;
//using MGroup.XFEM.Elements;
//using MGroup.XFEM.Entities;
//using MGroup.XFEM.Geometry.LSM.Utilities;
//using MGroup.XFEM.Geometry.Primitives;

////TODO: Implement the cases, where the body level set or its extension go through an single node or edge without haveing any
////      more common points with an element.
//namespace MGroup.XFEM.Geometry.LSM
//{
//    /// <summary>
//    /// Based on "Non-planar 3D crack growth by the extended finite element and level sets—Part II: Level set update, 2002, 
//    /// Gravouli et al.:.
//    /// </summary>
//    public class OpenLsm3D : ISingleTipLsmGeometry
//    {
//        public OpenLsm3D(int id)
//        {
//            this.ID = id;
//        }

//        public int ID { get; }

//        Dictionary<int, double> ILsmGeometry.LevelSets => LevelSetsBody;
//        public Dictionary<int, double> LevelSetsBody { get; } = new Dictionary<int, double>();
//        public Dictionary<int, double> LevelSetsTip { get; } = new Dictionary<int, double>();

//        public void InitializeLevelSetBody()
//        {

//        }

//        public void InitializeLevelSetTip()
//        {

//        }

//        public IElementOpenGeometryInteraction Intersect(IXFiniteElement element)
//        {
//            var elementLevelSets = new ElementLevelSets(element, LevelSetsBody, LevelSetsTip);

//            // Check this first, since it is faster and most elements belong to this category 
//            if (IsElementDisjoint(element, elementLevelSets))
//            {
//                return new NullElementDiscontinuityInteraction(ID, element);
//            }

//            //HERE: I also need to store the psi of each vector of the intersection mesh
//            IntersectionMesh3D intersectionMesh;
//            bool isConforming = TryFindInteractionConforming(element, elementLevelSets, out intersectionMesh);
//            if (!isConforming) intersectionMesh = FindInteractionIntersecting(element, elementLevelSets);

//            if (intersectionMesh.Vertices.Count <= 2) // The only common points are an edge or a node 
//            {
//                throw new NotImplementedException();
//            }
//            else // At least 3 intersection points
//            {
//                // Find min, max tip level sets of intersection points
//                double minIntersectionPsi = double.MaxValue;
//                double maxIntersectionPsi = double.MinValue;
//                foreach (IntersectionPoint intersection in intersections)
//                {
//                    if (intersection.TipLevelSet < minIntersectionPsi) minIntersectionPsi = intersection.TipLevelSet;
//                    if (intersection.TipLevelSet > maxIntersectionPsi) maxIntersectionPsi = intersection.TipLevelSet;
//                }

//                // Based on these min, max determine whether the element contains the crack front
//                if (minIntersectionPsi > 0)
//                {
//                    // All points lie on the extension of the crack beyond its front
//                    return new NullElementDiscontinuityInteraction(this.ID, element);
//                }
//                else if (maxIntersectionPsi < 0)
//                {
//                    // All points lie on the crack surface before the crack front
//                    var pos = conforming ? RelativePositionCurveElement.Conforming : RelativePositionCurveElement.Intersecting;
//                    return new OpenLsmElementIntersection3D(this.ID, element, pos, false,
//                        new double[][] { point0.CoordinatesNatural, point1.CoordinatesNatural });
//                }
//                else
//                {
//                    // The crack front lies inside the element
//                    throw new NotImplementedException();
//                    //HERE: See what I did in 2D. In 3D I will modify the triangular mesh created for the intersections for phi=0. 
//                    //Triangles intersected by psi=0 will be split into subtriangles with vertices that have psi<=0. 
//                    //Vertices with psi>0 will be discarded. I should do these in this class, rather than the mesh class.
//                }


                
//            }

//        }

//        public double SignedDistanceOf(XNode node) => LevelSetsBody[node.ID];

//        public double SignedDistanceOf(XPoint point)
//        {
//            IReadOnlyList<XNode> nodes = point.Element.Nodes;
//            double signedDistance = 0.0;
//            for (int n = 0; n < nodes.Count; ++n)
//            {
//                signedDistance += point.ShapeFunctions[n] * LevelSetsBody[nodes[n].ID];
//            }
//            return signedDistance;
//        }

//        public double[] SignedDistanceGradientThrough(XPoint point)
//        {
//            IReadOnlyList<XNode> nodes = point.Element.Nodes;
//            double gradientX = 0.0;
//            double gradientY = 0.0;
//            double gradientZ = 0.0;
//            for (int n = 0; n < nodes.Count; ++n)
//            {
//                double dNdx = point.ShapeFunctionDerivatives[n, 0];
//                double dNdy = point.ShapeFunctionDerivatives[n, 1];
//                double dNdz = point.ShapeFunctionDerivatives[n, 2];

//                double levelSet = LevelSetsBody[nodes[n].ID];
//                gradientX += dNdx * levelSet;
//                gradientY += dNdy * levelSet;
//                gradientZ += dNdz * levelSet;
//            }
//            return new double[] { gradientX, gradientY, gradientZ };
//        }

//        private IntersectionMesh3D FindInteractionIntersecting(IXFiniteElement element, ElementLevelSets elementLevelSets)
//        {
//            ElementFace[] allFaces = element.Faces;
//            var intersectionPoints = new Dictionary<double[], HashSet<ElementFace>>();

//            // Find any nodes that may lie on the LSM geometry
//            for (int n = 0; n < element.Nodes.Count; ++n)
//            {
//                XNode node = element.Nodes[n];
//                if (elementLevelSets.BodyLevelSets[node.ID] == 0)
//                {
//                    HashSet<ElementFace> facesOfNode = ElementFace.FindFacesOfNode(node.ID, allFaces);
//                    intersectionPoints.Add(element.Interpolation.NodalNaturalCoordinates[n], facesOfNode);
//                }
//            }

//            // Find intersection points that lie on element edges, excluding nodes
//            foreach (ElementEdge edge in element.Edges)
//            {
//                IntersectionPoint intersection = 
//                    IntersectEdgeExcludingNodesWithBodyLevelSet(edge, elementLevelSets);
//                if (intersection != null)
//                {
//                    HashSet<ElementFace> facesOfEdge = edge.FindFacesOfEdge(allFaces);
//                    intersectionPoints.Add(intersection.CoordinatesNatural, facesOfEdge);
//                }
//            }

//            // Create mesh
//            return IntersectionMesh3D.CreateMultiCellMesh3D(intersectionPoints);
//        }

//        private IntersectionPoint IntersectEdgeExcludingNodesWithBodyLevelSet(ElementEdge edge,
//            ElementLevelSets elementLevelSets)
//        {
//            double[] node0 = edge.NodesNatural[0];
//            double[] node1 = edge.NodesNatural[1];
//            double phi0 = elementLevelSets.BodyLevelSets[edge.NodeIDs[0]];
//            double phi1 = elementLevelSets.BodyLevelSets[edge.NodeIDs[1]];
//            double psi0 = elementLevelSets.TipLevelSets[edge.NodeIDs[0]];
//            double psi1 = elementLevelSets.TipLevelSets[edge.NodeIDs[1]];

//            if (phi0 * phi1 < 0.0) // Edge is intersected but not at its nodes
//            {
//                // The intersection point between these nodes can be found using the linear interpolation, see 
//                // Sukumar 2001
//                double k = -phi0 / (phi1 - phi0);
//                var intersectionNatural = new double[3];
//                for (int d = 0; d < 3; ++d)
//                {
//                    intersectionNatural[d] = node0[d] + k * (node1[d] - node0[d]);
//                }
//                double psi = psi0 + k * (psi1 - psi0);

//                return new IntersectionPoint() 
//                { 
//                    CoordinatesNatural = intersectionNatural, 
//                    Edge = edge, 
//                    TipLevelSet = psi 
//                };
//            }
//            else return null;
//        }

//        /// <summary>
//        /// Optimization for most elements. It is possible for this method to return false, even if the element is disjoint.
//        /// </summary>
//        /// <param name="element"></param>
//        private static bool IsElementDisjoint(IXFiniteElement element, ElementLevelSets elementLevelSets)
//        {
//            double minBodyLS = double.MaxValue;
//            double maxBodyLS = double.MinValue;
//            double minTipLS = double.MaxValue;

//            foreach (XNode node in element.Nodes)
//            {
//                double bodyLS = elementLevelSets.BodyLevelSets[node.ID];
//                if (bodyLS < minBodyLS) minBodyLS = bodyLS;
//                if (bodyLS > maxBodyLS) maxBodyLS = bodyLS;

//                double tipLS = elementLevelSets.TipLevelSets[node.ID];
//                if (tipLS < minTipLS) minTipLS = tipLS;
//            }

//            if (minBodyLS * maxBodyLS > 0.0) return true;
//            else if (minTipLS > 0.0) return true;
//            else return false;
//        }

//        private bool TryFindInteractionConforming(IXFiniteElement element, ElementLevelSets elementLevelSets, 
//            out IntersectionMesh3D intersectionMesh)
//        {
//            // Find the nodes that lie on the surface
//            var zeroNodes = new HashSet<int>();
//            foreach (XNode node in element.Nodes)
//            {
//                double distance = elementLevelSets.BodyLevelSets[node.ID];
//                if (distance == 0) zeroNodes.Add(node.ID);
//            }

//            // Find which face has exactly these nodes
//            intersectionMesh = null;
//            foreach (ElementFace face in element.Faces)
//            {
//                if (zeroNodes.SetEquals(face.NodeIDs))
//                {
//                    intersectionMesh = IntersectionMesh3D.CreateTriagleMeshForElementFace(face.CellType, face.NodesNatural);
//                    return true;
//                }
//            }

//            // At this point no face has exactly the zero nodes of the whole element.
//            return false;
//        }
//    }
//}
