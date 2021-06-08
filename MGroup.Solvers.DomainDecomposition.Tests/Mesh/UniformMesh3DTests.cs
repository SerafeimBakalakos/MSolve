using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MGroup.Solvers.DomainDecomposition.Partitioning;
using Xunit;

namespace MGroup.Solvers.DomainDecomposition.Tests.Mesh
{
    public static class UniformMesh3DTests
    {
        [Fact]
        public static void PlotMesh()
        {
            var writer = new VtkMeshWriter();

            string path = @"C:\Users\Serafeim\Desktop\PFETIDP\meshes\mesh3D.vtk";
            double[] minCoords = { 0, 0, 0 };
            double[] maxCoords = { 60, 60, 60 };
            int[] numElements = { 2, 3, 4 };
            //var mesh = new UniformMesh3D.Builder(minCoords, maxCoords, numElements).SetMajorMinorAxis(2, 0).BuildMesh();
            var mesh = new UniformMesh3D.Builder(minCoords, maxCoords, numElements).BuildMesh();
            writer.WriteMesh3D(path, mesh);

            int[] nodeIDs = mesh.EnumerateNodes().Select(pair => pair.nodeID).ToArray();
            for (int i = 0; i < nodeIDs.Length - 1; ++i)
            {
                Assert.Equal(nodeIDs[i] + 1, nodeIDs[i + 1]);
            }
            int[] elemIDs = mesh.EnumerateElements().Select(pair => pair.elementID).ToArray();
            for (int i = 0; i < elemIDs.Length - 1; ++i)
            {
                Assert.Equal(elemIDs[i] + 1, elemIDs[i + 1]);
            }
        }
    }
}
