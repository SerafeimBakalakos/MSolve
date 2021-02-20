using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using MGroup.XFEM.ElementGeometry;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Entities;
using MGroup.XFEM.Geometry.Primitives;

namespace MGroup.XFEM.Geometry.LSM
{
    public class VectorLevelSet3D
    {
        public VectorLevelSet3D(int id)
        {
            this.ID = id;
        }

        public int ID { get; }

        public Dictionary<int, double> LevelSetsBody { get; } = new Dictionary<int, double>();

        public Dictionary<int, double> LevelSetsTip { get; } = new Dictionary<int, double>();

        /// <summary>
        /// Based on "Abaqus implementation of extended finite element method using a level set representation for 
        /// three-dimensional fatigue crack growth and life predictions, Shi et al., 2005". This a vary simple approach that does
        /// not require a mesh generator, does not require setting tolerances and handles the crack front similar to the crack 
        /// body. Unfortunately, it creates more subtetrahedra than necessary, which hinders performance due to all the Gauss 
        /// points.
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        public IElementOpenGeometryInteraction Intersect(IXFiniteElement element)
        {
            (Dictionary<int, double> nodalBodyLevelSets, Dictionary<int, double> nodalTipLevelSets) =
                FindLevelSetsOfElementNodes(element);

            // Check this first, since it is faster and most elements belong to this category 
            if (IsElementDisjoint(element, nodalBodyLevelSets, nodalTipLevelSets))
            {
                return new NullElementDiscontinuityInteraction(ID, element);
            }

            //2: Find the intersection points on each edge. No need to create a triangular mesh in 3D for now. I will need it in cohesive cracks thow
            // Find intersection points that lie on element edges, excluding nodes
            var intersectionPoints = new List<IntersectionPoint>();
            foreach (ElementEdge edge in element.Edges)
            {
                IntersectionPoint intersection = 
                    IntersectEdgeExcludingNodesWithBodyLevelSet(edge, nodalBodyLevelSets, nodalTipLevelSets);
                if (intersection != null)
                {
                    intersectionPoints.Add(intersection);
                }
            }
            if (intersectionPoints.Count < 3) throw new NotImplementedException("Cannot handle conforming cases yet.");

            //3: Determine if the element is intersected, tip or false positive/disjoint
            (bool isIntersected, bool containsFront) = CategorizeElementIntersectedByZeroPhi(intersectionPoints);

            //4: The subtetrahedra will be created by a separate class. Move the appropriate comments there.

            //5: There use the intersection points of each phase to define the above/below crack subarea in each element face

            //6: Find the centroid of each subarea and connect it with the appropriate nodes and intersection points, creating conforming triangles

            //7: Define the above/below crack subvolume of the whole element

            //8: Find the centroid each subvolume and connect it to the vertices of the triangles on each face to create conforming subtetrahedra

            //9: Delete these comments

            throw new NotImplementedException();
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

        /// <summary>
        /// Better approach to identify intersected and tip elements than the one proposed in "Modelling crack growth by level 
        /// sets in the extended finite element method, Stolarska et al., 2001". For details see MSc thesis of Serafeim Bakalakos, 
        /// section 5.3.9 
        /// </summary>
        /// <param name="intersections"></param>
        /// <returns></returns>
        private (bool isIntersected, bool containsFront) CategorizeElementIntersectedByZeroPhi(
            IEnumerable<IntersectionPoint> intersections)
        {
            // Find min, max tip level sets
            double minIntersectionPsi = double.MaxValue;
            double maxIntersectionPsi = double.MinValue;
            foreach (IntersectionPoint intersection in intersections)
            {
                if (intersection.TipLevelSet < minIntersectionPsi) minIntersectionPsi = intersection.TipLevelSet;
                if (intersection.TipLevelSet > maxIntersectionPsi) maxIntersectionPsi = intersection.TipLevelSet;
            }

            if (minIntersectionPsi > 0)
            {
                // All points lie on the extension of the crack beyond its front
                return (false, false);
            }
            else if (maxIntersectionPsi < 0)
            {
                // All points lie on the crack surface before the crack front
                return (true, false);
            }
            else
            {
                // The crack front lies inside the element
                return (true, true);
            }
        }

        private (Dictionary<int, double> bodyLevelSets, Dictionary<int, double> tipLevelSets) FindLevelSetsOfElementNodes(
            IXFiniteElement element)
        {
            int numNodes = element.Nodes.Count;
            var bodyLevelSets = new Dictionary<int, double>(numNodes);
            var tipLevelSets = new Dictionary<int, double>(numNodes);
            for (int n = 0; n < numNodes; ++n)
            {
                int nodeID = element.Nodes[n].ID;
                bodyLevelSets[nodeID] = LevelSetsBody[nodeID];
                if (bodyLevelSets[nodeID] == 0) throw new NotImplementedException();
                tipLevelSets[nodeID] = LevelSetsTip[nodeID];
                if (tipLevelSets[nodeID] == 0) throw new NotImplementedException();
            }
            return (bodyLevelSets, tipLevelSets);
        }

        private IntersectionPoint IntersectEdgeExcludingNodesWithBodyLevelSet(ElementEdge edge, 
            Dictionary<int, double> nodalBodyLevelSets, Dictionary<int, double> nodalTipLevelSets)
        {
            double[] node0 = edge.NodesNatural[0];
            double[] node1 = edge.NodesNatural[1];
            double phi0 = nodalBodyLevelSets[edge.NodeIDs[0]];
            double phi1 = nodalBodyLevelSets[edge.NodeIDs[1]];
            double psi0 = nodalTipLevelSets[edge.NodeIDs[0]];
            double psi1 = nodalTipLevelSets[edge.NodeIDs[1]];

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

                return new IntersectionPoint() { CoordinatesNatural = intersectionNatural, Edge = edge, TipLevelSet = psi };
            }
            else return null;
        }

        /// <summary>
        /// Optimization for most elements. It is possible for this method to return false, even if the element is disjoint.
        /// </summary>
        /// <param name="element"></param>
        private static bool IsElementDisjoint(IXFiniteElement element,
            Dictionary<int, double> nodalBodyLevelSets, Dictionary<int, double> nodalTipLevelSets)
        {
            double minBodyLS = double.MaxValue;
            double maxBodyLS = double.MinValue;
            double minTipLS = double.MaxValue;

            foreach (XNode node in element.Nodes)
            {
                double bodyLS = nodalBodyLevelSets[node.ID];
                if (bodyLS < minBodyLS) minBodyLS = bodyLS;
                if (bodyLS > maxBodyLS) maxBodyLS = bodyLS;

                double tipLS = nodalTipLevelSets[node.ID];
                if (tipLS < minTipLS) minTipLS = tipLS;
            }

            if (minBodyLS * maxBodyLS > 0.0) return true;
            else if (minTipLS > 0.0) return true;
            else return false;
        }

        //TODO: This is repeated in 2D crack LSM class
        public class IntersectionPoint
        {
            public double[] CoordinatesNatural { get; set; }

            public ElementEdge Edge { get; set; }

            public double TipLevelSet { get; set; }
        }
    }
}
