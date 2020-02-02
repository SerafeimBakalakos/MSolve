using System;
using System.Collections.Generic;
using System.Diagnostics;
using ISAAR.MSolve.Discretization.Integration;
using ISAAR.MSolve.Discretization.Integration.Quadratures;
using ISAAR.MSolve.XFEM.Multiphase.Elements;
using ISAAR.MSolve.XFEM.Multiphase.Entities;
using ISAAR.MSolve.XFEM.Multiphase.Geometry;

//TODO: Perhaps avoid integration in triangles with very small area. Or should that be handled, by not crating those in the 
//      first place?
namespace ISAAR.MSolve.XFEM.Multiphase.Integration
{
    public class IntegrationWithConformingSubtriangles2D: IIntegrationStrategy
    {
        private readonly GeometricModel geometricModel;
        private readonly TriangleQuadratureSymmetricGaussian quadratureInSubcells;
        private readonly IQuadrature2D standardQuadrature;

        public IntegrationWithConformingSubtriangles2D(IQuadrature2D standardQuadrature, GeometricModel geometricModel, 
            TriangleQuadratureSymmetricGaussian quadratureInSubcells)
        {
            this.standardQuadrature = standardQuadrature;
            this.geometricModel = geometricModel;
            this.quadratureInSubcells = quadratureInSubcells;
        }

        public IReadOnlyList<GaussPoint> GenerateIntegrationPoints(IXFiniteElement element)
        {
            // Standard elements
            if (UseStandardQuadrature(element)) return standardQuadrature.IntegrationPoints;

            // Access the conforming subtriangles
            IReadOnlyList<ElementSubtriangle> subtriangles;
            try
            {
                subtriangles = geometricModel.ConformingMesh[element];
            }
            catch (Exception)
            {
                throw new InvalidOperationException("To use integration with subtriangles, a conforming triangulation must " +
                    "first be created for all elements intersected by curves");
            }

            // Create integration points for all subtriangles
            var integrationPoints = new List<GaussPoint>();
            foreach (ElementSubtriangle triangle in subtriangles)
            {
                integrationPoints.AddRange(GenerateIntegrationPointsOfSubtriangle(triangle));
            }
            return integrationPoints;
        }

        // These triangles are output by the delauny triangulation and the order of their nodes might be 
        // counter-clockwise or clockwise. In the second case the jacobian will be negative, 
        // but it doesn't matter otherwise. 
        private IReadOnlyList<GaussPoint> GenerateIntegrationPointsOfSubtriangle(ElementSubtriangle triangle)
        {
            // Coordinates of the triangle's nodes in the natural system of the element
            double xi1 = triangle.VerticesNatural[0].Xi;
            double eta1 = triangle.VerticesNatural[0].Eta;
            double xi2 = triangle.VerticesNatural[1].Xi;
            double eta2 = triangle.VerticesNatural[1].Eta;
            double xi3 = triangle.VerticesNatural[2].Xi;
            double eta3 = triangle.VerticesNatural[2].Eta;

            // Determinant of the Jacobian of the linear mapping from the natural system of the triangle  to the 
            // natural system of the element. If the triangle's nodes are in clockwise order, the determinant will be 
            // negative. It doesn't matter since its absolute value is used for integration with change of variables.
            double jacobian = Math.Abs(xi1 * (eta2 - eta3) + xi2 * (eta3 - eta1) + xi3 * (eta1 - eta2));

            IReadOnlyList<GaussPoint> triangleGaussPoints = quadratureInSubcells.IntegrationPoints;
            var elementGaussPoints = new GaussPoint[triangleGaussPoints.Count];
            for (int i = 0; i < triangleGaussPoints.Count; ++i)
            {
                GaussPoint triangleGP = triangleGaussPoints[i];

                // Linear shape functions evaluated at the Gauss point's coordinates in the triangle's natural system.
                double N1 = 1.0 - triangleGP.Xi - triangleGP.Eta;
                double N2 = triangleGP.Xi;
                double N3 = triangleGP.Eta;

                // Coordinates of the same gauss point in the element's natural system
                double elementXi = N1 * xi1 + N2 * xi2 + N3 * xi3;
                double elementEta = N1 * eta1 + N2 * eta2 + N3 * eta3;

                // The integral would need to be multiplied with |detJ|. 
                // It is simpler for the caller to have it already included in the weight.
                double elementWeight = triangleGP.Weight * jacobian;

                elementGaussPoints[i] = new GaussPoint(elementXi, elementEta, elementWeight);
            }

            return elementGaussPoints;
        }

        private bool UseStandardQuadrature(IXFiniteElement element)
        {
            Debug.Assert(element.Phases.Count > 0);
            if (element.Phases.Count == 1) return true;
            else return false;
        }
    }
}
