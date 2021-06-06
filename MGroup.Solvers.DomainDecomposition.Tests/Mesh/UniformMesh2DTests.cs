using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MGroup.Solvers.DomainDecomposition.Partitioning;
using Xunit;

namespace MGroup.Solvers.DomainDecomposition.Tests.Mesh
{
    public static class UniformMesh2DTests
    {
        //HERE move these to a dedicated MeshTests class. Then allow user to permute nodes in Quad4, Hexa8 with defaults
        [Fact]
        public static void PlotMesh()
        {
            var writer = new VtkMeshWriter();

            string path = @"C:\Users\Serafeim\Desktop\PFETIDP\meshes\mesh2D.vtk";
            double[] minCoords = { 0, 0 };
            double[] maxCoords = { 12, 12 };
            int[] numElements = { 3, 4 };
            var mesh = UniformMesh2D.Create(minCoords, maxCoords, numElements, 1);
            //var mesh = UniformMesh2D.Create(minCoords, maxCoords, numElements);
            writer.WriteMesh2D(path, mesh);


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
