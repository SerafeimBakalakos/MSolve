using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Integration;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.XFEM.Multiphase.Elements;
using ISAAR.MSolve.XFEM.Multiphase.Geometry;
using static ISAAR.MSolve.XFEM.Tests.Multiphase.Integration.BenchmarkDomain;
using static ISAAR.MSolve.XFEM.Tests.Multiphase.Integration.Utilities;

//TODO: Hardcode as much calculations (integrals, areas, jacobians) as possible and write part of the calculations in comments
//TODO: Fill the rest of the not implemented tests
namespace ISAAR.MSolve.XFEM.Tests.Multiphase.Integration
{
    public class ConstantFunction : IBenchmarkVolumeFunction
    {
        private readonly double value;

        public ConstantFunction(double value = 1.0) => this.value = value;

        public double Evaluate(GaussPoint point, IXFiniteElement element) => value;

        public double GetExpectedIntegral(GeometryType geometryType)
        {
            var element = new BenchmarkDomain(geometryType);
            return value * Utilities.CalcPolygonArea(element.Element.Nodes);
        }

        public CurveElementIntersection[] GetIntersectionSegments()
        {
            var intersection = new CurveElementIntersection(RelativePositionCurveElement.Intersection,
                new NaturalPoint[] { new NaturalPoint(-1.0, -1.0), new NaturalPoint(+1.0, +1.0) });
            return new CurveElementIntersection[] { intersection };
        }

        public bool IsInValidRegion(GaussPoint point) => true;
    }

    public class LinearFunction : IBenchmarkVolumeFunction
    {
        public double Evaluate(GaussPoint point, IXFiniteElement element)
        {
            CartesianPoint cartesian = element.InterpolationStandard.TransformNaturalToCartesian(element.Nodes, point);
            return -3 + 2 * cartesian.X * cartesian.Y;
        }

        public double GetExpectedIntegral(GeometryType geometryType)
        {
            if (geometryType == GeometryType.Natural)
            {
                return -12 + 0;
            }
            else if (geometryType == GeometryType.Rectangle)
            {
                return -36 + 72;
            }
            else if (geometryType == GeometryType.Quad)
            {
                throw new NotImplementedException();
            }
            else throw new NotImplementedException();
        }

        public CurveElementIntersection[] GetIntersectionSegments()
        {
            var intersection = new CurveElementIntersection(RelativePositionCurveElement.Intersection,
                new NaturalPoint[] { new NaturalPoint(-1.0, -1.0), new NaturalPoint(+1.0, +1.0) });
            return new CurveElementIntersection[] { intersection };
        }

        public bool IsInValidRegion(GaussPoint point) => true;
    }

    public class PiecewiseConstant2Function : IBenchmarkVolumeFunction
    {
        public double Evaluate(GaussPoint point, IXFiniteElement element)
        {
            if (point.Xi < 0) return 4.0;
            else if (point.Xi > 0) return 16.0;
            else throw new ArgumentException("The point's xi must belong to [-1, 0) U (0, 1]");
        }

        public double GetExpectedIntegral(GeometryType geometryType)
        {
            if (geometryType == GeometryType.Natural)
            {
                return 1 * 2 * 4.0 + 1 * 2 * 16.0;
            }
            else if (geometryType == GeometryType.Rectangle)
            {
                return 2 * 3 * 4.0 + 2 * 3 * 16.0;
            }
            else if (geometryType == GeometryType.Quad)
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

                IXFiniteElement element = new BenchmarkDomain(geometryType).Element;
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

        public CurveElementIntersection[] GetIntersectionSegments()
        {
            var intersection = new CurveElementIntersection(RelativePositionCurveElement.Intersection,
                new NaturalPoint[] { new NaturalPoint(0.0, -1.0), new NaturalPoint(0.0, +1.0) });
            return new CurveElementIntersection[] { intersection };
        }

        public bool IsInValidRegion(GaussPoint point) => (point.Xi < 0.0) || (point.Xi > 0.0);
    }

    public class PiecewiseConstant4Function : IBenchmarkVolumeFunction
    {
        public double Evaluate(GaussPoint point, IXFiniteElement element)
        {
            if ((point.Xi < -0.5) && (point.Eta < 0)) return -1.0;
            else if ((point.Xi > -0.5) && (point.Eta < 0)) return 3.0;
            else if ((point.Xi < -0.5) && (point.Eta > 0)) return 7.0;
            else if ((point.Xi > -0.5) && (point.Eta > 0)) return 11.0;
            else throw new ArgumentException("Invalid region");
        }

        public double GetExpectedIntegral(GeometryType geometryType)
        {
            if (geometryType == GeometryType.Natural)
            {
                return 0.5 * 1 * (-1.0) + 1.5 * 1 * 3.0 + 0.5 * 1 * 7.0 + 1.5 * 1 * 11.0;
            }
            else if (geometryType == GeometryType.Rectangle)
            {
                return 1 * 1.5 * (-1.0) + 3 * 1.5 * 3.0 + 1 * 1.5 * 7.0 + 3 * 1.5 * 11.0;
            }
            else if (geometryType == GeometryType.Quad)
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

                IXFiniteElement element = new BenchmarkDomain(geometryType).Element;
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

        public CurveElementIntersection[] GetIntersectionSegments()
        {
            var intersections = new CurveElementIntersection[4];

            intersections[0] = new CurveElementIntersection(RelativePositionCurveElement.Intersection,
                new NaturalPoint[] { new NaturalPoint(-0.5, -1.0), new NaturalPoint(-0.5, 0.0) });
            intersections[1] = new CurveElementIntersection(RelativePositionCurveElement.Intersection,
                new NaturalPoint[] { new NaturalPoint(-0.5, 0.0), new NaturalPoint(-0.5, 1.0) });
            intersections[2] = new CurveElementIntersection(RelativePositionCurveElement.Intersection,
                new NaturalPoint[] { new NaturalPoint(-1, 0.0), new NaturalPoint(-0.5, 0.0) });
            intersections[3] = new CurveElementIntersection(RelativePositionCurveElement.Intersection,
                new NaturalPoint[] { new NaturalPoint(-0.5, 0.0), new NaturalPoint(1.0, 0.0) });
            return intersections;
        }

        public bool IsInValidRegion(GaussPoint point) 
            => ((point.Xi < -0.5) || (point.Xi > -0.5)) && ((point.Eta < 0.0) || (point.Eta > 0.0));
    }

    public class PiecewiseLinear2Function : IBenchmarkVolumeFunction
    {
        public double Evaluate(GaussPoint point, IXFiniteElement element)
        {
            CartesianPoint cartesian = element.InterpolationStandard.TransformNaturalToCartesian(element.Nodes, point);
            double xi = point.Xi;
            double x = cartesian.X;
            double y = cartesian.Y;
            if ((-1 <= xi) && (xi < -0.5)) return 1 + 2 * x * y;
            else if ((-0.5 < xi) && (xi <= 1)) return 3 + 4 * x * y;
            else throw new ArgumentException("The point's xi must belong to [-1, -0.5) U (-0.5, 1]");
        }

        public double GetExpectedIntegral(GeometryType geometryType)
        {
            if (geometryType == GeometryType.Natural)
            {
                // I = I1 + I2 = ... = (0.5 * 2 * 1.0 + 0) + (1.5 * 2 * 3.0 + 0) = 10
                return 10.0;
            }
            else if (geometryType == GeometryType.Rectangle)
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
            else if (geometryType == GeometryType.Quad)
            {
                throw new NotImplementedException();
            }
            else throw new NotImplementedException();
        }

        public CurveElementIntersection[] GetIntersectionSegments()
        {
            var intersection = new CurveElementIntersection(RelativePositionCurveElement.Intersection,
                new NaturalPoint[] { new NaturalPoint(-0.5, -1.0), new NaturalPoint(-0.5, +1.0) });
            return new CurveElementIntersection[] { intersection };
        }

        public bool IsInValidRegion(GaussPoint point) => (point.Xi < -0.5) || (point.Xi > -0.5);
    }
}
