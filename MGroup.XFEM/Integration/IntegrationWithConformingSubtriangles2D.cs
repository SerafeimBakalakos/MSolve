using System;
using System.Collections.Generic;
using System.Diagnostics;
using MGroup.XFEM.Integration;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Geometry.ConformingMesh;
using MGroup.XFEM.Integration.Quadratures;
using MGroup.XFEM.Integration;

//TODO: Perhaps avoid integration in triangles with very small area. Or should that be handled, by not creating those in the 
//      first place? The former, would interfere with the code that decides whether to not enrich nodes to avoid singularities.
namespace MGroup.XFEM.Integration
{
    public class IntegrationWithConformingSubtriangles2D: IBulkIntegration
    {
        private readonly TriangleQuadratureSymmetricGaussian quadratureInSubcells;
        private readonly IQuadrature standardQuadrature; //TODO: This should be accessed from the element

        public IntegrationWithConformingSubtriangles2D(IQuadrature standardQuadrature, 
            TriangleQuadratureSymmetricGaussian quadratureInSubcells)
        {
            this.standardQuadrature = standardQuadrature;
            this.quadratureInSubcells = quadratureInSubcells;
        }

        public IReadOnlyList<GaussPoint> GenerateIntegrationPoints(IXFiniteElement element)
        {
            // Standard elements
            if (element.ConformingSubcells == null) return standardQuadrature.IntegrationPoints;

            // Create integration points for all subtriangles
            var integrationPoints = new List<GaussPoint>();
            foreach (ElementSubtriangle2D triangle in element.ConformingSubcells)
            {
                integrationPoints.AddRange(GenerateIntegrationPointsOfSubtriangle(triangle));
            }
            return integrationPoints;
        }

        // These triangles are output by the delauny triangulation and the order of their nodes might be 
        // counter-clockwise or clockwise. In the second case the jacobian will be negative, 
        // but it doesn't matter otherwise. 
        private IReadOnlyList<GaussPoint> GenerateIntegrationPointsOfSubtriangle(ElementSubtriangle2D triangle)
        {
            // Coordinates of the triangle's nodes in the natural system of the element
            double xi0 = triangle. VerticesNatural[0].Xi;
            double eta0 = triangle.VerticesNatural[0].Eta;
            double xi1 = triangle.VerticesNatural[1].Xi;
            double eta1 = triangle.VerticesNatural[1].Eta;
            double xi2 = triangle.VerticesNatural[2].Xi;
            double eta2 = triangle.VerticesNatural[2].Eta;

            // Determinant of the Jacobian of the linear mapping from the natural system of the triangle  to the 
            // natural system of the element. If the triangle's nodes are in clockwise order, the determinant will be 
            // negative. It doesn't matter since its absolute value is used for integration with change of variables.
            double jacobian = Math.Abs(xi0 * (eta1 - eta2) + xi1 * (eta2 - eta0) + xi2 * (eta0 - eta1));

            IReadOnlyList<GaussPoint> triangleGaussPoints = quadratureInSubcells.IntegrationPoints;
            var elementGaussPoints = new GaussPoint[triangleGaussPoints.Count];
            for (int i = 0; i < triangleGaussPoints.Count; ++i)
            {
                GaussPoint triangleGP = triangleGaussPoints[i];

                // Linear shape functions evaluated at the Gauss point's coordinates in the triangle's natural system.
                double N0 = 1.0 - triangleGP.Coordinates[0] - triangleGP.Coordinates[1];
                double N1 = triangleGP.Coordinates[0];
                double N2 = triangleGP.Coordinates[1];

                // Coordinates of the same gauss point in the element's natural system
                var naturalCoords = new double[3];
                naturalCoords[0] = N0 * xi0 + N1 * xi1 + N2 * xi2;
                naturalCoords[1] = N0 * eta0 + N1 * eta1 + N2 * eta2;

                // The integral would need to be multiplied with |detJ|. 
                // It is simpler for the caller to have it already included in the weight.
                double elementWeight = triangleGP.Weight * jacobian;

                elementGaussPoints[i] = new GaussPoint(naturalCoords, elementWeight);
            }

            return elementGaussPoints;
        }
    }
}
