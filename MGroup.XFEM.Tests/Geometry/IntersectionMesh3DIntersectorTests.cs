using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Mesh;
using ISAAR.MSolve.LinearAlgebra;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using MGroup.XFEM.Geometry;
using MGroup.XFEM.Geometry.LSM.Utilities;
using Xunit;

//TODO: Remove duplication
namespace MGroup.XFEM.Tests.Geometry
{
    public static class IntersectionMesh3DIntersectorTests
    {
        [Fact]
        public static void TestSingleTriangleMesh0()
        {
            IntersectionMesh3D oldMesh = CreateSingleTriangleMesh();

            var psiLevelSets = new double[oldMesh.Vertices.Count];
            for (int v = 0; v < psiLevelSets.Length; ++v)
            {
                psiLevelSets[v] = oldMesh.Vertices[v][0] - 0.5;
            }

            var intersector = new IntersectionMesh3DIntersector(oldMesh, psiLevelSets);
            IntersectionMesh3D newMeshComputed = intersector.IntersectMesh();

            var newMeshExpected = new IntersectionMesh3D();
            newMeshExpected.Vertices.Add(new double[] { 0, 0, 0 });
            newMeshExpected.Vertices.Add(new double[] { 0, 1, 0 });
            newMeshExpected.Vertices.Add(new double[] { 0.5, 0.5, 0 });
            newMeshExpected.Vertices.Add(new double[] { 0.5, 0, 0 });
            newMeshExpected.Cells.Add((CellType.Tri3, new int[] { 0, 3, 2 }));
            newMeshExpected.Cells.Add((CellType.Tri3, new int[] { 0, 1, 2 }));

            double tolerance = 1E-3;
            var comparer = new IntersectedMesh3DComparer(tolerance);
            Assert.True(comparer.AreEqual(newMeshExpected, newMeshComputed));
        }

        [Fact]
        public static void TestSingleTriangleMesh1()
        {
            IntersectionMesh3D oldMesh = CreateSingleTriangleMesh();

            var psiLevelSets = new double[oldMesh.Vertices.Count];
            for (int v = 0; v < psiLevelSets.Length; ++v)
            {
                psiLevelSets[v] = 0.5 - oldMesh.Vertices[v][0];
            }

            var intersector = new IntersectionMesh3DIntersector(oldMesh, psiLevelSets);
            IntersectionMesh3D newMeshComputed = intersector.IntersectMesh();

            var newMeshExpected = new IntersectionMesh3D();
            newMeshExpected.Vertices.Add(new double[] { 1, 0, 0 });
            newMeshExpected.Vertices.Add(new double[] { 0.5, 0, 0 });
            newMeshExpected.Vertices.Add(new double[] { 0.5, 0.5, 0 });
            newMeshExpected.Cells.Add((CellType.Tri3, new int[] { 0, 1, 2 }));

            double tolerance = 1E-3;
            var comparer = new IntersectedMesh3DComparer(tolerance);
            Assert.True(comparer.AreEqual(newMeshExpected, newMeshComputed));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void TestSingleTriangleMesh0Rotated(bool rotation)
        {
            IntersectionMesh3D oldMesh = CreateSingleTriangleMesh();

            var psiLevelSets = new double[oldMesh.Vertices.Count];
            for (int v = 0; v < psiLevelSets.Length; ++v)
            {
                psiLevelSets[v] = 0.5 - oldMesh.Vertices[v][0];
            }

            if (rotation)
            {
                RotateMesh(oldMesh, new double[] { 0, 0, 1 }, new double[] { 1, 1, 1 });
            }

            var intersector = new IntersectionMesh3DIntersector(oldMesh, psiLevelSets);
            IntersectionMesh3D newMeshComputed = intersector.IntersectMesh();

            var newMeshExpected = new IntersectionMesh3D();
            newMeshExpected.Vertices.Add(new double[] { 1, 0, 0 });
            newMeshExpected.Vertices.Add(new double[] { 0.5, 0, 0 });
            newMeshExpected.Vertices.Add(new double[] { 0.5, 0.5, 0 });
            newMeshExpected.Cells.Add((CellType.Tri3, new int[] { 0, 1, 2 }));
            if (rotation)
            {
                RotateMesh(newMeshExpected, new double[] { 0, 0, 1 }, new double[] { 1, 1, 1 });
            }

            double tolerance = 1E-3;
            var comparer = new IntersectedMesh3DComparer(tolerance);
            Assert.True(comparer.AreEqual(newMeshExpected, newMeshComputed));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void TestSingleTriangleMesh1Rotated(bool rotation)
        {
            IntersectionMesh3D oldMesh = CreateSingleTriangleMesh();

            var psiLevelSets = new double[oldMesh.Vertices.Count];
            for (int v = 0; v < psiLevelSets.Length; ++v)
            {
                psiLevelSets[v] = oldMesh.Vertices[v][0] - 0.5;
            }

            if (rotation)
            {
                RotateMesh(oldMesh, new double[] { 0, 0, 1 }, new double[] { 1, 1, 1 });
            }

            var intersector = new IntersectionMesh3DIntersector(oldMesh, psiLevelSets);
            IntersectionMesh3D newMeshComputed = intersector.IntersectMesh();

            var newMeshExpected = new IntersectionMesh3D();
            newMeshExpected.Vertices.Add(new double[] { 0, 0, 0 });
            newMeshExpected.Vertices.Add(new double[] { 0, 1, 0 });
            newMeshExpected.Vertices.Add(new double[] { 0.5, 0.5, 0 });
            newMeshExpected.Vertices.Add(new double[] { 0.5, 0, 0 });
            newMeshExpected.Cells.Add((CellType.Tri3, new int[] { 0, 3, 2 }));
            newMeshExpected.Cells.Add((CellType.Tri3, new int[] { 0, 1, 2 }));
            if (rotation)
            {
                RotateMesh(newMeshExpected, new double[] { 0, 0, 1 }, new double[] { 1, 1, 1 });
            }

            double tolerance = 1E-3;
            var comparer = new IntersectedMesh3DComparer(tolerance);
            Assert.True(comparer.AreEqual(newMeshExpected, newMeshComputed));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void TestMultiTriangleMesh0(bool rotation)
        {
            IntersectionMesh3D oldMesh = CreateMultiTriangleMesh();

            var psiLevelSets = new double[oldMesh.Vertices.Count];
            for (int v = 0; v < psiLevelSets.Length; ++v)
            {
                psiLevelSets[v] = oldMesh.Vertices[v][0] - 0.5;
            }
            if (rotation)
            {
                RotateMesh(oldMesh, new double[] { 0, 0, 1 }, new double[] { 1, 1, 1 });
            }

            var intersector = new IntersectionMesh3DIntersector(oldMesh, psiLevelSets);
            IntersectionMesh3D newMeshComputed = intersector.IntersectMesh();


            //    1 __|4 (0.5,1)
            //     |\ |
            //     | \|3 (0.5,0.5)
            //     | /|
            //     |/_|        
            //    0   |2 (0.5,0)
            //         
            var newMeshExpected = new IntersectionMesh3D();
            newMeshExpected.Vertices.Add(new double[] { 0, 0, 0 });
            newMeshExpected.Vertices.Add(new double[] { 0, 1, 0 });
            newMeshExpected.Vertices.Add(new double[] { 0.5, 0, 0 });
            newMeshExpected.Vertices.Add(new double[] { 0.5, 0.5, 0 });
            newMeshExpected.Vertices.Add(new double[] { 0.5, 1, 0 });
            newMeshExpected.Cells.Add((CellType.Tri3, new int[] { 0, 2, 3 }));
            newMeshExpected.Cells.Add((CellType.Tri3, new int[] { 0, 3, 1 }));
            newMeshExpected.Cells.Add((CellType.Tri3, new int[] { 1, 3, 4 }));
            if (rotation)
            {
                RotateMesh(newMeshExpected, new double[] { 0, 0, 1 }, new double[] { 1, 1, 1 });
            }

            double tolerance = 1E-3;
            var comparer = new IntersectedMesh3DComparer(tolerance);
            Assert.True(comparer.AreEqual(newMeshExpected, newMeshComputed));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void TestMultiTriangleMesh1(bool rotation)
        {
            IntersectionMesh3D oldMesh = CreateMultiTriangleMesh();

            var psiLevelSets = new double[oldMesh.Vertices.Count];
            for (int v = 0; v < psiLevelSets.Length; ++v)
            {
                psiLevelSets[v] = oldMesh.Vertices[v][0] - 1;
            }

            if (rotation)
            {
                RotateMesh(oldMesh, new double[] { 0, 0, 1 }, new double[] { 1, 1, 1 });
            }

            var intersector = new IntersectionMesh3DIntersector(oldMesh, psiLevelSets);
            IntersectionMesh3D newMeshComputed = intersector.IntersectMesh();


            //    2 ___|3
            //     |\  |
            //     | \ |
            //     |__\|      
            //    0    |1
            //       x=1          
            var newMeshExpected = new IntersectionMesh3D();
            newMeshExpected.Vertices.Add(new double[] { 0, 0, 0 });
            newMeshExpected.Vertices.Add(new double[] { 0, 1, 0 });
            newMeshExpected.Vertices.Add(new double[] { 1, 0, 0 });
            newMeshExpected.Vertices.Add(new double[] { 1, 1, 0 });
            newMeshExpected.Cells.Add((CellType.Tri3, new int[] { 0, 1, 2 }));
            newMeshExpected.Cells.Add((CellType.Tri3, new int[] { 1, 3, 2 }));
            if (rotation)
            {
                RotateMesh(newMeshExpected, new double[] { 0, 0, 1 }, new double[] { 1, 1, 1 });
            }

            double tolerance = 1E-3;
            var comparer = new IntersectedMesh3DComparer(tolerance);
            Assert.True(comparer.AreEqual(newMeshExpected, newMeshComputed));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void TestMultiTriangleMesh2(bool rotation)
        {
            IntersectionMesh3D oldMesh = CreateMultiTriangleMesh();

            var psiLevelSets = new double[oldMesh.Vertices.Count];
            for (int v = 0; v < psiLevelSets.Length; ++v)
            {
                psiLevelSets[v] = 0.5 - oldMesh.Vertices[v][0];
            }

            if (rotation)
            {
                RotateMesh(oldMesh, new double[] { 0, 0, 1 }, new double[] { 1, 1, 1 });
            }

            var intersector = new IntersectionMesh3DIntersector(oldMesh, psiLevelSets);
            IntersectionMesh3D newMeshComputed = intersector.IntersectMesh();

            //            
            // 6|__2______3              
            //  | /|\     |         
            // 5|/ |  \   |          
            //  |\ |    \ |          
            // 4|_\|_____\|                
            //  |  0      1
            // x=0.5
            var newMeshExpected = new IntersectionMesh3D();
            newMeshExpected.Vertices.Add(new double[] { 1, 0, 0 });
            newMeshExpected.Vertices.Add(new double[] { 2, 0, 0 });
            newMeshExpected.Vertices.Add(new double[] { 1, 1, 0 });
            newMeshExpected.Vertices.Add(new double[] { 2, 1, 0 });
            newMeshExpected.Vertices.Add(new double[] { 0.5, 0, 0 });
            newMeshExpected.Vertices.Add(new double[] { 0.5, 0.5, 0 });
            newMeshExpected.Vertices.Add(new double[] { 0.5, 1, 0 });
            newMeshExpected.Cells.Add((CellType.Tri3, new int[] { 0, 1, 2 }));
            newMeshExpected.Cells.Add((CellType.Tri3, new int[] { 3, 2, 1 }));
            newMeshExpected.Cells.Add((CellType.Tri3, new int[] { 4, 0, 5 }));
            newMeshExpected.Cells.Add((CellType.Tri3, new int[] { 5, 0, 2 }));
            newMeshExpected.Cells.Add((CellType.Tri3, new int[] { 5, 2, 6 }));
            if (rotation)
            {
                RotateMesh(newMeshExpected, new double[] { 0, 0, 1 }, new double[] { 1, 1, 1 });
            }

            double tolerance = 1E-3;
            var comparer = new IntersectedMesh3DComparer(tolerance);
            Assert.True(comparer.AreEqual(newMeshExpected, newMeshComputed));
        }

        private static IntersectionMesh3D CreateSingleTriangleMesh()
        {
            //    1  |
            //     |\|
            //     | |\ 
            //     |_|_\          
            //    0  |  1
            //       0.5  
            // psi<0    psi>0
            //      OR
            // psi>0    psi<0

            var oldMesh = new IntersectionMesh3D();
            oldMesh.Vertices.Add(new double[] { 0, 0, 0 });
            oldMesh.Vertices.Add(new double[] { 1, 0, 0 });
            oldMesh.Vertices.Add(new double[] { 0, 1, 0 });
            oldMesh.Cells.Add((CellType.Tri3, new int[] { 0, 1, 2 }));

            return oldMesh;
        }

        private static IntersectionMesh3D CreateMultiTriangleMesh()
        {
            //    1 _|__ ___
            //     |\|  |\  |
            //     | |\ |  \|
            //     |_|_\ ___\         
            //    0  |  1   2
            //       0.5  

            var oldMesh = new IntersectionMesh3D();
            oldMesh.Vertices.Add(new double[] { 0, 0, 0 });
            oldMesh.Vertices.Add(new double[] { 1, 0, 0 });
            oldMesh.Vertices.Add(new double[] { 2, 0, 0 });
            oldMesh.Vertices.Add(new double[] { 0, 1, 0 });
            oldMesh.Vertices.Add(new double[] { 1, 1, 0 });
            oldMesh.Vertices.Add(new double[] { 2, 1, 0 });
            oldMesh.Cells.Add((CellType.Tri3, new int[] { 0, 1, 3 }));
            oldMesh.Cells.Add((CellType.Tri3, new int[] { 4, 3, 1 }));
            oldMesh.Cells.Add((CellType.Tri3, new int[] { 1, 2, 4 }));
            oldMesh.Cells.Add((CellType.Tri3, new int[] { 5, 4, 2 }));

            return oldMesh;
        }

        /// <summary>
        /// See https://stackoverflow.com/questions/9423621/3d-rotations-of-a-plane
        /// </summary>
        /// <param name="oldNormalVector"></param>
        /// <param name="newNormalVector"></param>
        private static void RotateMesh(IntersectionMesh3D mesh, double[] oldNormalVector, double[] newNormalVector)
        {
            // Calc rotation axis
            double[] m = oldNormalVector;
            double[] n = newNormalVector;
            double cosTheta = m.DotProduct(n) / (m.Norm2() * n.Norm2());
            double[] cross = m.CrossProduct(n);
            double[] axis = cross.Scale(1 / cross.Norm2());

            // Calc rotation matrix
            double c = cosTheta;
            double s = Math.Sqrt(1 - c * c);
            double C = 1 - c;
            double x = axis[0];
            double y = axis[1];
            double z = axis[2];
            var rmat = Matrix.CreateFromArray(new double[,]
            {
                { x*x*C+c,   x*y*C-z*s, x*z*C+y*s},
                { y*x*C+z*s, y*y*C+c,   y*z*C-x*s},
                { z*x*C-y*s, z*y*C+x*s, z*z*C+c}
            });

            // Rotate each vertex
            for (int v = 0; v < mesh.Vertices.Count; ++v)
            {
                mesh.Vertices[v] = rmat.Multiply(mesh.Vertices[v]);
            }
        }
    }
}
