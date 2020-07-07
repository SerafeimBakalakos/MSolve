using System;
using System.Collections.Generic;
using System.Text;
using MGroup.XFEM.Integration;
using ISAAR.MSolve.Geometry.Coordinates;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Geometry.Primitives;
using MGroup.XFEM.Integration.Quadratures;
using ISAAR.MSolve.Discretization.Mesh;

namespace MGroup.XFEM.Geometry.LSM
{
    /// <summary>
    /// A curve resulting from the intersection of a parent curve with a 2D element.
    /// Degenerate cases are also possible: null or single point.
    /// </summary>
    public class LsmElementIntersection2D : IElementGeometryIntersection
    {
        private readonly double[] startNatural;
        private readonly double[] endNatural;

        public LsmElementIntersection2D(RelativePositionCurveElement relativePosition, IXFiniteElement2D element,
            double[] startNatural, double[] endNatural)
        {
            if (relativePosition == RelativePositionCurveElement.Disjoint)
            {
                throw new ArgumentException("There is no intersection between the curve and element");
            }
            this.RelativePosition = relativePosition;
            this.Element = element;
            this.startNatural = startNatural;
            this.endNatural = endNatural;
        }

        public RelativePositionCurveElement RelativePosition { get; }

        public IXFiniteElement Element { get; } //TODO: Perhaps this should be defined in the interface

        public IIntersectionMesh ApproximateGlobalCartesian()
        {
            var meshCartesian = new IntersectionMesh();
            meshCartesian.Vertices.Add(Element.Interpolation.TransformNaturalToCartesian(Element.Nodes, startNatural));
            meshCartesian.Vertices.Add(Element.Interpolation.TransformNaturalToCartesian(Element.Nodes, endNatural));
            meshCartesian.Cells.Add((CellType.Line, new int[] { 0, 1 }));
            return meshCartesian;
        }

        //TODO: Perhaps a dedicated IBoundaryIntegration component is needed
        public IReadOnlyList<GaussPoint> GetIntegrationPoints(int order)
        {
            // Conforming curves intersect 2 elements, thus the integral will be computed twice. Halve the weights to avoid 
            // obtaining double the value of the integral.
            double weightModifier = 1.0;
            if (RelativePosition == RelativePositionCurveElement.Conforming) weightModifier = 0.5;

            // Absolute determinant of Jacobian of mapping from auxiliary to cartesian system. Constant for all Gauss points.
            double[] startCartesian = Element.Interpolation.TransformNaturalToCartesian(Element.Nodes, startNatural);
            double[] endCartesian = Element.Interpolation.TransformNaturalToCartesian(Element.Nodes, endNatural);
            double detJ = Math.Abs(0.5 * startCartesian.Distance2D(endCartesian));

            var quadrature1D = GaussLegendre1D.GetQuadratureWithOrder(order);
            int numIntegrationPoints = quadrature1D.IntegrationPoints.Count;
            var integrationPoints = new GaussPoint[numIntegrationPoints];
            for (int i = 0; i < numIntegrationPoints; ++i)
            {
                GaussPoint gp1D = quadrature1D.IntegrationPoints[i];
                double N0 = 0.5 * (1.0 - gp1D.Coordinates[0]);
                double N1 = 0.5 * (1.0 + gp1D.Coordinates[0]);
                double xi = N0 * startNatural[0] + N1 * endNatural[0];
                double eta = N0 * startNatural[1] + N1 * endNatural[1];
                integrationPoints[i] = new GaussPoint(new double[] { xi, eta }, gp1D.Weight * detJ * weightModifier);
            }

            return integrationPoints;
        }

        public IList<double[]> GetPointsForTriangulation()
        {
            return new double[][] { startNatural, endNatural };
        }
    }
}
