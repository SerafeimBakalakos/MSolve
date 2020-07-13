﻿using System;
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
using MGroup.XFEM.Integration.Quadratures;
using MGroup.XFEM.Integration;

namespace MGroup.XFEM.Geometry.LSM
{
    /// <summary>
    /// A surface resulting from the intersection of a parent surface with a 3D finite element.
    /// Degenerate cases are also possible: null, single point or single curve.
    /// </summary>
    public class LsmElementIntersection3D : IElementGeometryIntersection
    {
        private readonly IntersectionMesh intersectionMeshNatural;

        public LsmElementIntersection3D(int parentGeometryID, RelativePositionCurveElement relativePosition, 
            IXFiniteElement element, IntersectionMesh intersectionMeshNatural)
        {
            this.ParentGeometryID = parentGeometryID;
            this.RelativePosition = relativePosition;
            this.Element = element;
            this.intersectionMeshNatural = intersectionMeshNatural;
        }
        public int ParentGeometryID { get; }

        public RelativePositionCurveElement RelativePosition { get; }

        public IXFiniteElement Element { get; } //TODO: Perhaps this should be defined in the interface

        public IIntersectionMesh ApproximateGlobalCartesian()
        {
            var meshCartesian = new IntersectionMesh();
            foreach (double[] vertexNatural in intersectionMeshNatural.Vertices)
            {
                double[] vertexCartesian = Element.Interpolation.TransformNaturalToCartesian(
                    Element.Nodes, vertexNatural);
                meshCartesian.Vertices.Add(vertexCartesian);
            }
            foreach ((CellType cellType, int[] connectivity) in intersectionMeshNatural.Cells)
            {
                meshCartesian.Cells.Add((cellType, connectivity));
            }
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
            IList<double[]> allVertices = intersectionMeshNatural.Vertices;
            foreach ((CellType cellType, int[] cellConnectivity) in intersectionMeshNatural.Cells)
            {
                if (cellType == CellType.Tri3)
                {
                    // Vertices of triangle in natural system
                    var verticesNatural = new double[][]
                    {
                        allVertices[cellConnectivity[0]], allVertices[cellConnectivity[1]], allVertices[cellConnectivity[2]]
                    };

                    // Vertices of triangle in cartesian system
                    var verticesCartesian = new double[3][];
                    for (int v = 0; v < 3; ++v)
                    {
                        verticesCartesian[v] = Element.Interpolation.TransformNaturalToCartesian(
                            Element.Nodes, verticesNatural[v]);
                    }

                    // Determinant of jacobian from auxiliary system of triangle to global cartesian system.
                    // This is possible because the mappings auxiliary -> natural and natural -> cartesian are both affine.
                    // Therefore the normalized triangle in auxiliary system will be projected onto a triangle in global 
                    // cartesian system.
                    var side0 = new double[3];
                    for (int i = 0; i < 3; ++i)
                    {
                        side0[i] = verticesCartesian[1][i] - verticesCartesian[0][i];
                    }
                    var side1 = new double[3];
                    for (int i = 0; i < 3; ++i)
                    {
                        side1[i] = verticesCartesian[2][i] - verticesCartesian[0][i];
                    }
                    double triangleArea = 0.5 * side0.CrossProduct(side1).Norm2();
                    double detJAuxiliaryNatural = 2 * triangleArea;

                    TriangleQuadratureSymmetricGaussian quadrature = ChooseQuadrature(order);
                    foreach (GaussPoint gpAuxiliary in quadrature.IntegrationPoints)
                    {
                        var shapeFuncs = new double[3];
                        shapeFuncs[0] = 1 - gpAuxiliary.Coordinates[0] - gpAuxiliary.Coordinates[1];
                        shapeFuncs[1] = gpAuxiliary.Coordinates[0];
                        shapeFuncs[2] = gpAuxiliary.Coordinates[1];
                        var gpNatural = new double[3];
                        for (int n = 0; n < shapeFuncs.Length; ++n)
                        {
                            for (int i = 0; i < 3; ++i)
                            {
                                gpNatural[i] += shapeFuncs[n] * verticesNatural[n][i];
                            }
                        }

                        double weight = gpAuxiliary.Weight * detJAuxiliaryNatural * weightModifier;
                        integrationPoints.Add(new GaussPoint(gpNatural, weight));
                    }

                }
                else
                {
                    throw new NotImplementedException();
                }
            }
            return integrationPoints;
        }

        public IList<double[]> GetPointsForTriangulation()
        {
            return intersectionMeshNatural.Vertices;
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