using System;
using System.Collections.Generic;
using System.Text;
using MGroup.XFEM.Integration;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Integration.Quadratures;
using ISAAR.MSolve.Discretization.Mesh;

namespace MGroup.XFEM.Geometry.LSM
{
    /// <summary>
    /// A curve resulting from the intersection of a parent curve with a 2D element.
    /// </summary>
    public class LsmElementIntersection2D : IElementDiscontinuityInteraction
    {
        private readonly IntersectionMesh2D intersectionMesh;

        public LsmElementIntersection2D(int parentGeometryID, RelativePositionCurveElement relativePosition,
            IXFiniteElement element, double[] startNatural, double[] endNatural)
        {
            this.ParentGeometryID = parentGeometryID;
            if ((relativePosition == RelativePositionCurveElement.Disjoint) /*|| (relativePosition == RelativePositionCurveElement.Tangent)*/)
            {
                throw new ArgumentException("There is no intersection between the curve and element");
            }
            this.RelativePosition = relativePosition;
            this.Element = element;

            this.intersectionMesh = new IntersectionMesh2D();
            this.intersectionMesh.Vertices.Add(startNatural);
            this.intersectionMesh.Vertices.Add(endNatural);
            this.intersectionMesh.Cells.Add((CellType.Line, new int[] { 0, 1 }));
        }

        public LsmElementIntersection2D(int parentGeometryID, RelativePositionCurveElement relativePosition, 
            IXFiniteElement element, IntersectionMesh2D intersectionMesh)
        {
            this.ParentGeometryID = parentGeometryID;
            if ((relativePosition == RelativePositionCurveElement.Disjoint) /*|| (relativePosition == RelativePositionCurveElement.Tangent)*/)
            {
                throw new ArgumentException("There is no intersection between the curve and element");
            }
            this.RelativePosition = relativePosition;
            this.Element = element;
            this.intersectionMesh = intersectionMesh;
        }

        public RelativePositionCurveElement RelativePosition { get; }

        public IXFiniteElement Element { get; } //TODO: Perhaps this should be defined in the interface

        public int ParentGeometryID { get; }

        public IIntersectionMesh ApproximateGlobalCartesian()
        {
            var meshCartesian = new IntersectionMesh2D();
            foreach (double[] vertexNatural in intersectionMesh.Vertices)
            {
                meshCartesian.Vertices.Add(Element.Interpolation.TransformNaturalToCartesian(Element.Nodes, vertexNatural));
            }

            foreach ((CellType, int[]) cell in intersectionMesh.Cells) // same connectivity
            {
                meshCartesian.Cells.Add(cell);
            }
            return meshCartesian;
        }

        //TODO: Perhaps a dedicated IBoundaryIntegration component is needed
        public IReadOnlyList<GaussPoint> GetBoundaryIntegrationPoints(int order)
        {
            var integrationPoints = new List<GaussPoint>();

            // Map intersection points to cartesian system
            var intersectionsCartesian = new List<double[]>(intersectionMesh.Vertices.Count);
            foreach (double[] vertexNatural in intersectionMesh.Vertices)
            {
                intersectionsCartesian.Add(Element.Interpolation.TransformNaturalToCartesian(Element.Nodes, vertexNatural));
            }

            for (int c = 0; c < intersectionMesh.Cells.Count; ++c)
            {
                //TODO: It would be safer to find the vertices from the cells, instead of assuming that they are in order.
                double[] startNatural = intersectionMesh.Vertices[c];
                double[] endNatural = intersectionMesh.Vertices[c + 1];

                // Conforming curves intersect 2 elements, thus the integral will be computed twice. Halve the weights to avoid 
                // obtaining double the value of the integral.
                double weightModifier = 1.0;
                if (RelativePosition == RelativePositionCurveElement.Conforming) weightModifier = 0.5; //MODIFICATION NEEDED: This should be different for each segment

                // Absolute determinant of Jacobian of mapping from auxiliary to cartesian system. Constant for all Gauss points.
                double length = Utilities.Distance2D(intersectionsCartesian[c], intersectionsCartesian[c + 1]);
                double detJ = Math.Abs(0.5 * length);

                var quadrature1D = GaussLegendre1D.GetQuadratureWithOrder(order);
                int numIntegrationPoints = quadrature1D.IntegrationPoints.Count;
                for (int i = 0; i < numIntegrationPoints; ++i)
                {
                    GaussPoint gp1D = quadrature1D.IntegrationPoints[i];
                    double N0 = 0.5 * (1.0 - gp1D.Coordinates[0]);
                    double N1 = 0.5 * (1.0 + gp1D.Coordinates[0]);
                    double xi = N0 * startNatural[0] + N1 * endNatural[0];
                    double eta = N0 * startNatural[1] + N1 * endNatural[1];
                    integrationPoints.Add(new GaussPoint(new double[] { xi, eta }, gp1D.Weight * detJ * weightModifier));
                }
            }

            return integrationPoints;
        }

        public IReadOnlyList<double[]> GetNormalsAtBoundaryIntegrationPoints(int order)
        {
            // Cartesian coordinates of vertices
            var verticesCartesian = new List<double[]>(intersectionMesh.Vertices.Count);
            foreach (double[] vertexNatural in intersectionMesh.Vertices)
            {
                verticesCartesian.Add(Element.Interpolation.TransformNaturalToCartesian(Element.Nodes, vertexNatural));
            }

            // Num points per segment
            var quadrature1D = GaussLegendre1D.GetQuadratureWithOrder(order);
            int numGaussPointsPerSegment = quadrature1D.IntegrationPoints.Count;

            // Find normal vectors of each segment
            var allNormals = new List<double[]>();
            for (int c = 0; c < intersectionMesh.Cells.Count; ++c)
            {
                //TODO: It would be safer to find the vertices from the cells, instead of assuming that they are in order.
                double[] startCartesian = verticesCartesian[c];
                double[] endCartesian = verticesCartesian[c + 1];

                IList<double[]> normalsOfSegment = 
                    GetNormalVectorsOfSegment(numGaussPointsPerSegment, startCartesian, endCartesian);
                allNormals.AddRange(normalsOfSegment);
            }

            return allNormals;
        }

        public IList<double[]> GetVerticesForTriangulation()
        {
            return intersectionMesh.Vertices;
        }

        private IList<double[]> GetNormalVectorsOfSegment(int numGaussPoints, double[] startCartesian, double[] endCartesian)
        {
            double dx = endCartesian[0] - startCartesian[0];
            double dy = endCartesian[1] - startCartesian[1];
            double length = Math.Sqrt(dx * dx + dy * dy);
            double cosa = dx / length;
            double sina = dy / length;
            double[] normalVector = { -sina, cosa };

            var normalVectors = new double[numGaussPoints][];
            for (int i = 0; i < numGaussPoints; ++i)
            {
                normalVectors[i] = normalVector;
            }

            return normalVectors;
        }
    }
}
