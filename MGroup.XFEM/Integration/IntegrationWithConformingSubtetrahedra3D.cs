using System;
using System.Collections.Generic;
using System.Diagnostics;
using ISAAR.MSolve.Discretization.Integration;
using ISAAR.MSolve.Discretization.Integration.Quadratures;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Geometry.ConformingMesh;

namespace MGroup.XFEM.Integration
{
    public class IntegrationWithConformingSubtetrahedra3D : IBulkIntegration
    {
        private readonly TetrahedronQuadrature quadratureInSubcells;
        private readonly IQuadrature3D standardQuadrature;

        public IntegrationWithConformingSubtetrahedra3D(IQuadrature3D standardQuadrature, 
            TetrahedronQuadrature quadratureInSubcells)
        {
            this.standardQuadrature = standardQuadrature;
            this.quadratureInSubcells = quadratureInSubcells;
        }

        public IReadOnlyList<GaussPoint> GenerateIntegrationPoints(IXFiniteElement element)
        {
            // Standard elements
            if (element.ConformingSubtetrahedra3D == null) return standardQuadrature.IntegrationPoints;

            // Create integration points for all subtriangles
            var integrationPoints = new List<GaussPoint>();
            foreach (ElementSubtetrahedron3D tetra in element.ConformingSubtetrahedra3D)
            {
                integrationPoints.AddRange(GenerateIntegrationPointsOfSubtriangle(tetra));
            }
            return integrationPoints;
        }

        // These triangles are output by the delauny triangulation and the order of their nodes might be 
        // counter-clockwise or clockwise. In the second case the jacobian will be negative, 
        // but it doesn't matter otherwise. 
        private IReadOnlyList<GaussPoint> GenerateIntegrationPointsOfSubtriangle(ElementSubtetrahedron3D tetra)
        {
            //TODO: Write this method in array form, instead of playing with Xi, Eta, Zeta

            // Coordinates of the triangle's nodes in the natural system of the element
            double xi0 = tetra.VerticesNatural[0].Xi;
            double eta0 = tetra.VerticesNatural[0].Eta;
            double zeta0 = tetra.VerticesNatural[0].Zeta;
            double xi1 = tetra.VerticesNatural[1].Xi;
            double eta1 = tetra.VerticesNatural[1].Eta;
            double zeta1 = tetra.VerticesNatural[1].Zeta;
            double xi2 = tetra.VerticesNatural[2].Xi;
            double eta2 = tetra.VerticesNatural[2].Eta;
            double zeta2 = tetra.VerticesNatural[2].Zeta;
            double xi3 = tetra.VerticesNatural[3].Xi;
            double eta3 = tetra.VerticesNatural[3].Eta;
            double zeta3 = tetra.VerticesNatural[3].Zeta;

            // Determinant of the Jacobian of the linear mapping from the natural system of the tetrahedron to the 
            // natural system of the element. We will take the absolute, so the vertex order is irrelevant.
            // See https://www.iue.tuwien.ac.at/phd/hollauer/node29.html eq (5.23)
            var jacobian = Matrix.CreateZero(3, 3);
            jacobian[0, 0] = xi0 - xi3;
            jacobian[1, 0] = eta0 - eta3;
            jacobian[2, 0] = zeta0 - zeta3;
            jacobian[0, 1] = xi1 - xi3;
            jacobian[1, 1] = eta1 - eta3;
            jacobian[2, 1] = zeta1 - zeta3;
            jacobian[0, 2] = xi2 - xi3;
            jacobian[1, 2] = eta2 - eta3;
            jacobian[2, 2] = zeta2 - zeta3;
            double detJ = jacobian.CalcDeterminant();

            IReadOnlyList<GaussPoint> triangleGaussPoints = quadratureInSubcells.IntegrationPoints;
            var elementGaussPoints = new GaussPoint[triangleGaussPoints.Count];
            for (int i = 0; i < triangleGaussPoints.Count; ++i)
            {
                GaussPoint tetraGP = triangleGaussPoints[i];

                // Linear shape functions evaluated at the Gauss point's coordinates in the triangle's natural system.
                double N0 = 1.0 - tetraGP.Xi - tetraGP.Eta - tetraGP.Zeta;
                double N1 = tetraGP.Xi;
                double N2 = tetraGP.Eta;
                double N3 = tetraGP.Zeta;

                // Coordinates of the same gauss point in the element's natural system
                double elementXi = N0 * xi0 + N1 * xi1 + N2 * xi2 + N3 * xi3;
                double elementEta = N0 * eta0 + N1 * eta1 + N2 * eta2 + N3 * eta3;
                double elementZeta = N0 * zeta0 + N1 * zeta1 + N2 * zeta2 + N3 * zeta3;

                // The integral would need to be multiplied with |detJ|. 
                // It is simpler for the caller to have it already included in the weight.
                double elementWeight = tetraGP.Weight * detJ;
                elementGaussPoints[i] = new GaussPoint(elementXi, elementEta, elementZeta, elementWeight);
            }

            return elementGaussPoints;
        }
    }
}
