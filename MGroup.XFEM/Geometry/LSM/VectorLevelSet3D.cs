//using System;
//using System.Collections.Generic;
//using System.Text;
//using MGroup.XFEM.Elements;
//using MGroup.XFEM.Entities;
//using MGroup.XFEM.Geometry.Primitives;

//namespace MGroup.XFEM.Geometry.LSM
//{
//    public class VectorLevelSet3D
//    {
//        public VectorLevelSet3D(int id)
//        {
//            this.ID = id;
//        }

//        public int ID { get; }

//        public Dictionary<int, double> LevelSetsBody { get; } = new Dictionary<int, double>();

//        public Dictionary<int, double> LevelSetsTip { get; } = new Dictionary<int, double>();

//        /// <summary>
//        /// Based on "Abaqus implementation of extended finite element method using a level set representation for 
//        /// three-dimensional fatigue crack growth and life predictions, Shi et al., 2005". This a vary simple approach that does
//        /// not require a mesh generator, does not require setting tolerances and handles the crack front similar to the crack 
//        /// body. Unfortunately, it creates more subtetrahedra than necessary, which hinders performance due to all the Gauss 
//        /// points.
//        /// </summary>
//        /// <param name="element"></param>
//        /// <returns></returns>
//        public IElementOpenGeometryInteraction Intersect(IXFiniteElement element)
//        {
//            (Dictionary<int, double> nodalBodyLevelSets, Dictionary<int, double> nodalTipLevelSets) =
//                FindLevelSetsOfElementNodes(element);

//            // Check this first, since it is faster and most elements belong to this category 
//            if (IsElementDisjoint(element, nodalBodyLevelSets, nodalTipLevelSets))
//            {
//                return new NullElementDiscontinuityInteraction(ID, element);
//            }

//            //2: determine if this element contains part of the crack front

//            //3: Store the intersection points on each edge. No need to create a triangular mesh in 3D for now. I will need it in cohesive cracks thow

//            //4: The subtetrahedra will be created by a separate class. Move the appropriate comments there.

//            //5: There use the intersection points of each phase to define the above/below crack subarea in each element face

//            //6: Find the centroid of each subarea and connect it with the appropriate nodes and intersection points, creating conforming triangles

//            //7: Define the above/below crack subvolume of the whole element

//            //8: Find the centroid each subvolume and connect it to the vertices of the triangles on each face to create conforming subtetrahedra
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

//        private (Dictionary<int, double> bodyLevelSets, Dictionary<int, double> tipLevelSets) FindLevelSetsOfElementNodes(
//            IXFiniteElement element)
//        {
//            int numNodes = element.Nodes.Count;
//            var bodyLevelSets = new Dictionary<int, double>(numNodes);
//            var tipLevelSets = new Dictionary<int, double>(numNodes);
//            for (int n = 0; n < numNodes; ++n)
//            {
//                int nodeID = element.Nodes[n].ID;
//                bodyLevelSets[nodeID] = LevelSetsBody[nodeID];
//                tipLevelSets[nodeID] = LevelSetsTip[nodeID];
//            }
//            return (bodyLevelSets, tipLevelSets);
//        }

//        /// <summary>
//        /// Optimization for most elements. It is possible for this method to return false, even if the element is disjoint.
//        /// </summary>
//        /// <param name="element"></param>
//        private static bool IsElementDisjoint(IXFiniteElement element,
//            Dictionary<int, double> nodalBodyLevelSets, Dictionary<int, double> nodalTipLevelSets)
//        {
//            double minBodyLS = double.MaxValue;
//            double maxBodyLS = double.MinValue;
//            double minTipLS = double.MaxValue;

//            foreach (XNode node in element.Nodes)
//            {
//                double bodyLS = nodalBodyLevelSets[node.ID];
//                if (bodyLS < minBodyLS) minBodyLS = bodyLS;
//                if (bodyLS > maxBodyLS) maxBodyLS = bodyLS;

//                double tipLS = nodalTipLevelSets[node.ID];
//                if (tipLS < minTipLS) minTipLS = tipLS;
//            }

//            if (minBodyLS * maxBodyLS > 0.0) return true;
//            else if (minTipLS > 0.0) return true;
//            else return false;
//        }
//    }
//}
