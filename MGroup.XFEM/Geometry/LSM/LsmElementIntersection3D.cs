using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using MGroup.XFEM.Integration;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.Discretization.Mesh;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.LinearAlgebra;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using MGroup.XFEM.Elements;
using ISAAR.MSolve.Discretization.Integration;
using ISAAR.MSolve.Discretization.Integration.Quadratures;

namespace MGroup.XFEM.Geometry.LSM
{
    public class LsmElementIntersection3D : IElementSurfaceIntersection3D
    {
        private readonly IntersectionMesh<NaturalPoint> intersectionMesh;

        public LsmElementIntersection3D(RelativePositionCurveElement relativePosition, IXFiniteElement3D element,
            IntersectionMesh<NaturalPoint> intersectionMesh)
        {
            this.RelativePosition = relativePosition;
            this.Element = element;
            this.intersectionMesh = intersectionMesh;
        }

        public RelativePositionCurveElement RelativePosition { get; }

        public IXFiniteElement3D Element { get; } //TODO: Perhaps this should be defined in the interface

        public IntersectionMesh<CartesianPoint> ApproximateGlobalCartesian()
        {
            var meshCartesian = new IntersectionMesh<CartesianPoint>();
            NaturalPoint[] verticesNatural = intersectionMesh.GetVerticesList();
            foreach (NaturalPoint vertexNatural in verticesNatural)
            {
                CartesianPoint vertexCartesian = Element.Interpolation.TransformNaturalToCartesian(
                    Element.Nodes, vertexNatural);
                meshCartesian.AddVertex(vertexCartesian);
            }
            meshCartesian.Cells = intersectionMesh.Cells;
            return meshCartesian;
        }
        //TODO: Perhaps a dedicated IBoundaryIntegration component is needed,
        //      along with dedicated concrete integrations for triangles, quads, etc
        public IReadOnlyList<GaussPoint> GetIntegrationPoints(int order)
        {
            if ((((IElementType)Element).CellType != CellType.Hexa8) && (((IElementType)Element).CellType != CellType.Tet4))
            {
                throw new NotImplementedException();
            }

            // Conforming surfaces intersect 2 elements, thus the integral will be computed twice. Halve the weights to avoid 
            // obtaining double the value of the integral.
            double weightModifier = 1.0;
            if (RelativePosition == RelativePositionCurveElement.Conforming) weightModifier = 0.5;

            var integrationPoints = new List<GaussPoint>();
            NaturalPoint[] allVertices = intersectionMesh.GetVerticesList();
            foreach ((CellType cellType, int[] cellConnectivity) in intersectionMesh.Cells)
            {
                if (cellType == CellType.Tri3)
                {
                    // Vertices of triangle in natural system
                    var verticesNatural = new NaturalPoint[]
                    {
                        allVertices[cellConnectivity[0]], allVertices[cellConnectivity[1]], allVertices[cellConnectivity[2]]
                    };

                    // Vertices of triangle in cartesian system
                    var verticesCartesian = new CartesianPoint[3];
                    for (int v = 0; v < 3; ++v)
                    {
                        verticesCartesian[v] = Element.Interpolation.TransformNaturalToCartesian(
                            Element.Nodes, verticesNatural[v]);
                    }

                    // Determinant of jacobian from auxiliary system of triangle to global cartesian system.
                    // This is possible because the mappings auxiliary -> natural and natural -> cartesian are both affine.
                    // Therefore the normalized triangle in auxiliary system will be projected onto a triangle in global 
                    // cartesian system.
                    double[] side0 =
                    {
                        verticesCartesian[1].X - verticesCartesian[0].X,
                        verticesCartesian[1].Y - verticesCartesian[0].Y,
                        verticesCartesian[1].Z - verticesCartesian[0].Z
                    };
                    double[] side1 =
                    {
                        verticesCartesian[2].X - verticesCartesian[0].X,
                        verticesCartesian[2].Y - verticesCartesian[0].Y,
                        verticesCartesian[2].Z - verticesCartesian[0].Z
                    };
                    double triangleArea = 0.5 * side0.CrossProduct(side1).Norm2();
                    double detJAuxiliaryNatural = 2 * triangleArea;

                    TriangleQuadratureSymmetricGaussian quadrature = ChooseQuadrature(order);
                    foreach (GaussPoint gpAuxiliary in quadrature.IntegrationPoints)
                    {
                        double N0 = (1 - gpAuxiliary.Xi - gpAuxiliary.Eta);
                        double N1 = gpAuxiliary.Xi;
                        double N2 = gpAuxiliary.Eta;

                        double xi = N0 * verticesNatural[0].Xi + N1 * verticesNatural[1].Xi + N2 * verticesNatural[2].Xi;
                        double eta = N0 * verticesNatural[0].Eta + N1 * verticesNatural[1].Eta + N2 * verticesNatural[2].Eta;
                        double zeta = N0 * verticesNatural[0].Zeta + N1 * verticesNatural[1].Zeta + N2 * verticesNatural[2].Zeta;

                        double weight = gpAuxiliary.Weight * detJAuxiliaryNatural * weightModifier;
                        integrationPoints.Add(new GaussPoint(xi, eta, zeta, weight));
                    }

                }
                else
                {
                    throw new NotImplementedException();
                }
            }
            return integrationPoints;
        }

        public NaturalPoint[] GetPointsForTriangulation()
        {
            return intersectionMesh.GetVerticesList();
        }

        private TriangleQuadratureSymmetricGaussian ChooseQuadrature(int order)
        {
            if (order <= 1) return TriangleQuadratureSymmetricGaussian.Order1Point1;
            else if (order == 2) return TriangleQuadratureSymmetricGaussian.Order2Points3;
            else if (order == 3) return TriangleQuadratureSymmetricGaussian.Order3Points4;
            else throw new NotImplementedException();
        }
    }
}
