using System;
using System.Collections.Generic;
using System.Text;
using MGroup.XFEM.Geometry.Primitives;
using Xunit;

namespace MGroup.XFEM.Tests.Geometry
{
    public static class Line2DIntersectionTests
    {
        public static void TestQuadDisjoint()
        {

        }

        public static void TestQuadTangent()
        {

        }

        public static void TestQuadIntersecting0Nodes()
        {

        }

        public static void TestQuadIntersecting1Node()
        {

        }

        public static void TestQuadIntersecting2Nodes()
        {

        }





        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void TestTriangleDisjoint(bool simple)
        {
            // 3             |         
            //       /\      |
            //      /  \     |
            //     /    \    |
            //    /      \   |
            // 1 /________\  |
            //               |
            //  1    2   3  4

            double[][] triangle = CreateTriangle();
            double[] p1 = new double[] { 4, 0 };
            double[] p2 = new double[] { 4, 1 };
            ILine2D line;
            if (simple) line = new DirectedLine2D_Simpler(p1, p2);
            else line = new DirectedLine2D(p1, p2);
            (RelativePositionCurveCurve pos, double[] intersections) = line.IntersectPolygon(triangle);

            Assert.True(pos == RelativePositionCurveCurve.Disjoint);
            Assert.Equal(0, intersections.Length);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void TestTriangleTangent(bool simple)
        {
            // 3  ___________          
            //       /\     
            //      /  \    
            //     /    \   
            //    /      \  
            // 1 /________\ 
            //   
            //  1    2    3  

            double[][] triangle = CreateTriangle();
            double[] p1 = new double[] { 0, 3 };
            double[] p2 = new double[] { 4, 3 };
            ILine2D line;
            if (simple) line = new DirectedLine2D_Simpler(p1, p2);
            else line = new DirectedLine2D(p1, p2);
            (RelativePositionCurveCurve pos, double[] intersectionsLocal) = line.IntersectPolygon(triangle);

            Assert.True(pos == RelativePositionCurveCurve.Tangent);
            Assert.Equal(1, intersectionsLocal.Length);

            double[] intersection = line.LocalToGlobal(intersectionsLocal[0]);
            Assert.Equal(2, intersection[0], 5);
            Assert.Equal(3, intersection[1], 5);

        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void TestTriangleIntersecting0Nodes(bool simple)
        {
            // 3          
            //       /\     
            //      /  \    
            // 2 --/----\---   
            //    /      \  
            // 1 /________\ 
            //  1    2     3 

            double[][] triangle = CreateTriangle();
            double[] p1 = new double[] { 0, 2 };
            double[] p2 = new double[] { 4, 2 };
            ILine2D line;
            if (simple) line = new DirectedLine2D_Simpler(p1, p2);
            else line = new DirectedLine2D(p1, p2);
            (RelativePositionCurveCurve pos, double[] intersectionsLocal) = line.IntersectPolygon(triangle);

            Assert.True(pos == RelativePositionCurveCurve.Intersection);
            Assert.Equal(2, intersectionsLocal.Length);

            double[] intersection1 = line.LocalToGlobal(intersectionsLocal[0]);
            Assert.Equal(1.5, intersection1[0], 5);
            Assert.Equal(2, intersection1[1], 5);

            double[] intersection2 = line.LocalToGlobal(intersectionsLocal[1]);
            Assert.Equal(2.5, intersection2[0], 5);
            Assert.Equal(2, intersection2[1], 5);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void TestTriangleIntersecting1Node(bool simple)
        {
            // 3      |     
            //       /|\     
            //      / | \    
            //     /  |  \   
            //    /   |   \  
            // 1 /____|____\ 
            //        |     
            //  1     2     3 

            double[][] triangle = CreateTriangle();
            double[] p1 = new double[] { 2, 0 };
            double[] p2 = new double[] { 2, 4 };
            ILine2D line;
            if (simple) line = new DirectedLine2D_Simpler(p1, p2);
            else line = new DirectedLine2D(p1, p2);
            (RelativePositionCurveCurve pos, double[] intersectionsLocal) = line.IntersectPolygon(triangle);

            Assert.True(pos == RelativePositionCurveCurve.Intersection);
            Assert.Equal(2, intersectionsLocal.Length);

            double[] intersection1 = line.LocalToGlobal(intersectionsLocal[0]);
            Assert.Equal(2, intersection1[0], 5);
            Assert.Equal(1, intersection1[1], 5);

            double[] intersection2 = line.LocalToGlobal(intersectionsLocal[1]);
            Assert.Equal(2, intersection2[0], 5);
            Assert.Equal(3, intersection2[1], 5);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void TestTriangleConforming(bool simple)
        {
            //      \
            // 3     \         
            //       /\     
            //      /  \    
            //     /    \   
            //    /      \  
            // 1 /________\ 
            //             \
            //              \
            //  1    2    3  

            double[][] triangle = CreateTriangle();
            //double[] p1 = new double[] { 2, 3 };
            //double[] p2 = new double[] { 3, 1 };
            double[] p1 = new double[] { 0, 7 };
            double[] p2 = new double[] { 3.5, 0 };
            ILine2D line;
            if (simple) line = new DirectedLine2D_Simpler(p1, p2);
            else line = new DirectedLine2D(p1, p2);
            (RelativePositionCurveCurve pos, double[] intersectionsLocal) = line.IntersectPolygon(triangle);

            Assert.True(pos == RelativePositionCurveCurve.Conforming);
            Assert.Equal(2, intersectionsLocal.Length);

            double[] intersection1 = line.LocalToGlobal(intersectionsLocal[0]);
            Assert.Equal(2, intersection1[0], 5);
            Assert.Equal(3, intersection1[1], 5);

            double[] intersection2 = line.LocalToGlobal(intersectionsLocal[1]);
            Assert.Equal(3, intersection2[0], 5);
            Assert.Equal(1, intersection2[1], 5);
        }

        private static double[][] CreateQuad()
        {
            var nodes = new double[4][];
            nodes[0] = new double[] { 0, 0 };
            nodes[1] = new double[] { 4, 0 };
            nodes[2] = new double[] { 4, 1 };
            nodes[3] = new double[] { 0, 1 };
            return nodes;
        }

        private static double[][] CreateTriangle()
        {
            var nodes = new double[3][];
            nodes[0] = new double[] { 1, 1 };
            nodes[1] = new double[] { 3, 1 };
            nodes[2] = new double[] { 2, 3 };
            return nodes;
        }
    }
}
