using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Integration;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.XFEM.Multiphase.Elements;
using ISAAR.MSolve.XFEM.Multiphase.Entities;
using ISAAR.MSolve.XFEM.Multiphase.Geometry;

//TODO: Fill the rest of the not implemented tests
//TODO: Hardcode as much calculations (integrals, areas, jacobians) as possible and write part of the calculations in comments
//TODO: Jacobians should be provided analytically by this class
//TODO: Use dedicated BenchmarkFunction and BenchmarkElement classes.
namespace ISAAR.MSolve.XFEM.Tests.Multiphase.Integration
{
    public static class IntegrationBenchmarks
    {
        public enum ElementType { Natural, Rectangle, Quad }
        public enum FunctionType { One, Linear, PiecewiseConstant2, PiecewiseConstant4, PiecewiseLinear2 }

        public static (MockQuad4 element, Func<GaussPoint, double> func, double expectedIntegral) SetupVolumeIntegral0()
        {
            // 3 ----------
            //   |  |     |
            //   |  |     |
            //   |  |     |
            //   ----------
            // 0  1     4   
            // f(x,y) = { 2xy, 0 <= x <= 1
            //          { 4xy, 1 <= x <= 4
            // I = I1 + I2 = ... = 9 * 1/2 + 9 * 15 = 139.5
            double expectedIntegral = 139.5;

            // Element
            var nodes = new XNode[4];
            nodes[0] = new XNode(0, 0.0, 0.0);
            nodes[1] = new XNode(1, 4.0, 0.0);
            nodes[2] = new XNode(2, 4.0, 3.0);
            nodes[3] = new XNode(3, 0.0, 3.0);
            var element = new MockQuad4(0, nodes);

            // Phases 
            var phase1 = new ConvexPhase(1);
            var phase2 = new ConvexPhase(2);
            element.Phases.Add(phase1);
            element.Phases.Add(phase2);

            // Boundary
            var A = new CartesianPoint(1, -1);
            var B = new CartesianPoint(1, 4);
            var line = new LineSegment2D(A, B);
            CurveElementIntersection intersection = line.IntersectElement(element, new UserDefinedMeshTolerance(3));
            var boundary = new PhaseBoundary(line, phase1, phase2);
            element.PhaseIntersections[boundary] = intersection;

            // Function
            Func<GaussPoint, double> func = p =>
            {
                CartesianPoint cartesian = element.StandardInterpolation.TransformNaturalToCartesian(element.Nodes, p);
                double x = cartesian.X;
                double y = cartesian.Y;
                if ((0 <= x) && (x < 1)) return 2 * x * y;
                else if ((1 < x) && (x <= 4)) return 4 * x * y;
                else throw new ArgumentException("The point's x must belong to [0, 1) U (1, 4]");
            };

            return (element, func, expectedIntegral);
        }

        public static MockQuad4 SetupElement(ElementType elementType)
        {
            if (elementType == ElementType.Natural)
            {
                var nodes = new XNode[4];
                nodes[0] = new XNode(0, -1, -1);
                nodes[1] = new XNode(1, +1, -1);
                nodes[2] = new XNode(2, +1, +1);
                nodes[3] = new XNode(3, -1, +1);
                return new MockQuad4(0, nodes);
            }
            else if (elementType == ElementType.Rectangle)
            {
                // 3 ----------
                //   |        |
                //   |        |
                //   |        |
                //   ----------
                //  0         4   

                var nodes = new XNode[4];
                nodes[0] = new XNode(0, 0.0, 0.0);
                nodes[1] = new XNode(1, 4.0, 0.0);
                nodes[2] = new XNode(2, 4.0, 3.0);
                nodes[3] = new XNode(3, 0.0, 3.0);
                return new MockQuad4(0, nodes);
            }
            else if (elementType == ElementType.Quad)
            {
                //                 (4,5)
                //               /\
                //             /   \
                //           /      \
                //         /         \
                //(0,2)  /            \
                //      |              \ 
                //      |               \
                //      -----------------
                //   (0,0)          (6,0)  
                var nodes = new XNode[4];
                nodes[0] = new XNode(0, 0, 0);
                nodes[1] = new XNode(1, 6, 0);
                nodes[2] = new XNode(2, 4, 5);
                nodes[3] = new XNode(3, 0, 2);
                return new MockQuad4(0, nodes);
            }
            else throw new NotImplementedException();
        }

        public static Func<IXFiniteElement, NaturalPoint, double> SetupFunction(FunctionType functionType)
        {
            if (functionType == FunctionType.One)
            {
                return (element, point) => 1.0;
            }
            else if (functionType == FunctionType.Linear)
            {
                return (element, point) =>
                {
                    CartesianPoint cartesian = element.StandardInterpolation.TransformNaturalToCartesian(element.Nodes, point);
                    return -3 + 2 * cartesian.X * cartesian.Y;
                };
            }
            else if (functionType == FunctionType.PiecewiseConstant2)
            {
                return (element, point) =>
                {
                    if (point.Xi < 0) return 4.0;
                    else if (point.Xi > 0) return 16.0;
                    else throw new ArgumentException("The point's xi must belong to [-1, 0) U (0, 1]");
                };
            }
            else if (functionType == FunctionType.PiecewiseConstant4)
            {
                return (element, point) =>
                {
                    if ((point.Xi < -0.5) && (point.Eta < 0)) return -1.0;
                    else if ((point.Xi > -0.5) && (point.Eta < 0)) return 3.0;
                    else if ((point.Xi < -0.5) && (point.Eta > 0)) return 7.0;
                    else if ((point.Xi > -0.5) && (point.Eta > 0)) return 11.0;
                    else throw new ArgumentException("Invalid region");
                };
            }
            else if (functionType == FunctionType.PiecewiseLinear2)
            {
                return (element, point) =>
                {
                    CartesianPoint cartesian = element.StandardInterpolation.TransformNaturalToCartesian(element.Nodes, point);
                    double x = cartesian.X;
                    double y = cartesian.Y;
                    double xi = point.Xi;
                    if ((-1 <= xi) && (xi < -0.5)) return 1 + 2 * x * y;
                    else if ((-0.5 < xi) && (xi <= 1)) return 3 + 4 * x * y;
                    else throw new ArgumentException("The point's xi must belong to [-1, -0.5) U (-0.5, 1]");
                };
            }
            else throw new NotImplementedException();
        }

        public static double SetupExpectedIntegral(ElementType elementType, 
            FunctionType functionType)
        {
            if (functionType == FunctionType.One)
            {
                MockQuad4 element = SetupElement(elementType);
                return CalcPolygonArea(element.Nodes);
            }
            else if (functionType == FunctionType.Linear)
            {
                if (elementType == ElementType.Natural)
                {
                    return -12 + 0;
                }
                else if (elementType == ElementType.Rectangle)
                {
                    return -36 + 72;
                }
                else if (elementType == ElementType.Quad)
                {
                    throw new NotImplementedException();
                }
                else throw new NotImplementedException();
            }
            else if (functionType == FunctionType.PiecewiseConstant4)
            {
                if (elementType == ElementType.Natural)
                {
                    return 0.5 * 1 * (-1.0) + 1.5 * 1 * 3.0 + 0.5 * 1 * 7.0 + 1.5 * 1 * 11.0;
                }
                else if (elementType == ElementType.Rectangle)
                {
                    return 1 * 1.5 * (-1.0) + 3 * 1.5 * 3.0 + 1 * 1.5 * 7.0 + 3 * 1.5 * 11.0;
                }
                else if (elementType == ElementType.Quad)
                {
                    //                 (4,5)
                    //               /\
                    //             /   \
                    //         B /      \
                    //         /     ___ \ D
                    //(0,2)  /  \___/     \
                    //      |___/|E        \ 
                    //    C |    \          \
                    //      -----------------
                    //   (0,0)    A       (6,0)  

                    MockQuad4 element = SetupElement(elementType);
                    CartesianPoint A = FindMiddle(element.Nodes[0], FindMiddle(element.Nodes[0], element.Nodes[1]));
                    CartesianPoint B = FindMiddle(element.Nodes[3], FindMiddle(element.Nodes[2], element.Nodes[3]));
                    CartesianPoint C = FindMiddle(element.Nodes[0], element.Nodes[3]);
                    CartesianPoint D = FindMiddle(element.Nodes[1], element.Nodes[2]);
                    CartesianPoint E = FindMiddle(A, B);

                    var bottomLeft = new CartesianPoint[4]
                    {
                        element.Nodes[0], A, E, C
                    };
                    var bottomRight = new CartesianPoint[4]
                    {
                        A, element.Nodes[1], D, E
                    };
                    var topLeft = new CartesianPoint[4]
                    {
                        C, E, B, element.Nodes[3]
                    };
                    var topRight = new CartesianPoint[4]
                    {
                        E, D, element.Nodes[2], B
                    };

                    return CalcPolygonArea(bottomLeft) * (-1.0) + CalcPolygonArea(bottomRight) * 3.0
                        + CalcPolygonArea(topLeft) * 7.0 + CalcPolygonArea(topRight) * 11.0;
                }
                else throw new NotImplementedException();
            }
            else if (functionType == FunctionType.PiecewiseConstant2)
            {
                if (elementType == ElementType.Natural)
                {
                    return 1 * 2 * 4.0 + 1 * 2 * 16.0;
                }
                else if (elementType == ElementType.Rectangle)
                {
                    return 2 * 3 * 4.0 + 2 * 3 * 16.0;
                }
                else if (elementType == ElementType.Quad)
                {
                    //                 (4,5)
                    //               /\
                    //             /   \
                    //         B /      \
                    //         /  \      \
                    //(0,2)  /     |      \
                    //      |       \      \ 
                    //      |       |       \
                    //      ---------\-------
                    //   (0,0)       A    (6,0)  

                    MockQuad4 element = SetupElement(elementType);
                    CartesianPoint A = FindMiddle(element.Nodes[0], element.Nodes[1]);
                    CartesianPoint B = FindMiddle(element.Nodes[2], element.Nodes[3]);

                    var leftNodes = new CartesianPoint[4]
                    {
                        element.Nodes[0], A, B, element.Nodes[3]
                    };
                    var rightNodes = new CartesianPoint[4]
                    {
                        A, element.Nodes[1], element.Nodes[2], B
                    };

                    return 4.0 * CalcPolygonArea(leftNodes) + 16.0 * CalcPolygonArea(rightNodes);
                }
                else throw new NotImplementedException();
            }
            else if (functionType == FunctionType.PiecewiseLinear2)
            {

                if (elementType == ElementType.Natural)
                {
                    // I = I1 + I2 = ... = (0.5 * 2 * 1.0 + 0) + (1.5 * 2 * 3.0 + 0) = 10
                    return 10.0;
                }
                else if (elementType == ElementType.Rectangle)
                {
                    // 3 ----------
                    //   |  |     |
                    //   |  |     |
                    //   |  |     |
                    //   ----------
                    // 0  1     4   
                    // f(x,y) = { 1 + 2xy, 0 <= xi <= -0.5
                    //          { 3 + 4xy, 0.5 <= xi <= 1
                    // I = I1 + I2 = ... = (1 * 3 * 1.0 + 9 * 1/2) + (3 * 3 * 3.0 + 9 * 15) = 169.5
                    return 169.5;
                }
                else if (elementType == ElementType.Quad)
                {
                    throw new NotImplementedException();
                }
                else throw new NotImplementedException();
            }
            else throw new NotImplementedException();
        }

        private static double CalcPolygonArea(IReadOnlyList<CartesianPoint> points)
        {
            double sum = 0.0;
            for (int i = 0; i < points.Count; ++i)
            {
                CartesianPoint point1 = points[i];
                CartesianPoint point2 = points[(i + 1) % points.Count];
                sum += point1.X * point2.Y - point2.X * point1.Y;
            }
            return Math.Abs(0.5 * sum); // area would be negative if vertices were in counter-clockwise order
        }

        private static CartesianPoint FindMiddle(CartesianPoint point1, CartesianPoint point2)
        {
            return new CartesianPoint(0.5 * (point1.X + point2.X), 0.5 * (point1.Y + point2.Y));
        }
    }
}
