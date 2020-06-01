using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Commons;
using ISAAR.MSolve.Geometry.Coordinates;
using MGroup.XFEM.Geometry.ConformingMesh;

namespace MGroup.XFEM.Tests.Triangulation
{
    internal static class TriangulationUtilities
    {
        internal static bool AreEqual(double[] expected, double[] computed, double tol)
        {
            if (expected.Length != computed.Length) return false;
            var comparer = new ValueComparer(tol);
            for (int i = 0; i < expected.Length; ++i)
            {
                if (!comparer.AreEqual(expected[i], computed[i])) return false;
            }
            return true;
        }

        internal static bool AreEqual(TriangleCell2D expected, TriangleCell2D computed, double tol)
        {
            if (expected.Vertices.Count != computed.Vertices.Count) return false;
            foreach (double[] computedVertex in computed.Vertices)
            {
                bool isInExpected = false;
                foreach (double[] expectedVertex in expected.Vertices)
                {
                    if (AreEqual(expectedVertex, computedVertex, tol))
                    {
                        isInExpected = true;
                        break;
                    }
                }
                if (!isInExpected) return false;
            }
            return true;
        }

        internal static bool AreEqual(TetrahedronCell3D expected, TetrahedronCell3D computed, double tol)
        {
            if (expected.Vertices.Count != computed.Vertices.Count) return false;
            foreach (double[] computedVertex in computed.Vertices)
            {
                bool isInExpected = false;
                foreach (double[] expectedVertex in expected.Vertices)
                {
                    if (AreEqual(expectedVertex, computedVertex, tol))
                    {
                        isInExpected = true;
                        break;
                    }
                }
                if (!isInExpected) return false;
            }
            return true;
        }

        internal static bool AreEqual(IList<TriangleCell2D> expected, IList<TriangleCell2D> computed, double tol)
        {
            if (expected.Count != computed.Count) return false;
            foreach (TriangleCell2D computedTriangle in computed)
            {
                bool isInExpected = false;
                foreach (TriangleCell2D expectedTriangle in expected)
                {
                    if (AreEqual(expectedTriangle, computedTriangle, tol))
                    {
                        isInExpected = true;
                        break;
                    }
                }
                if (!isInExpected) return false;
            }
            return true;
        }

        internal static bool AreEqual(IList<TetrahedronCell3D> expected, IList<TetrahedronCell3D> computed, double tol)
        {
            if (expected.Count != computed.Count) return false;
            foreach (TetrahedronCell3D computedTetra in computed)
            {
                bool isInExpected = false;
                foreach (TetrahedronCell3D expectedTetra in expected)
                {
                    if (AreEqual(expectedTetra, computedTetra, tol))
                    {
                        isInExpected = true;
                        break;
                    }
                }
                if (!isInExpected) return false;
            }
            return true;
        }

        internal static double CalcPolygonArea<TPoint>(List<TPoint> points) where TPoint : IPoint
        {
            double sum = 0.0;
            for (int vertexIdx = 0; vertexIdx < points.Count; ++vertexIdx)
            {
                TPoint vertex1 = points[vertexIdx];
                TPoint vertex2 = points[(vertexIdx + 1) % points.Count];
                sum += vertex1.X1 * vertex2.X2 - vertex2.X1 * vertex1.X2;
            }
            return Math.Abs(0.5 * sum); // area would be negative if vertices were in counter-clockwise order
        }
    }
}
