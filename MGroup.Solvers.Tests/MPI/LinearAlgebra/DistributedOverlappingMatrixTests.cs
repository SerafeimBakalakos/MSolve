using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.LinearAlgebra;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using MGroup.Solvers.MPI.Environments;
using MGroup.Solvers.MPI.LinearAlgebra;
using MGroup.Solvers.MPI.Topologies;
using Xunit;

namespace MGroup.Solvers.Tests.MPI.LinearAlgebra
{
    public static class DistributedOverlappingMatrixTests
    {
        [Fact]
        public static void TestMatrixVectorMultiplication()
        {
            var example = new Hexagon1DTopology();
            ComputeNodeTopology topology = example.CreateNodeTopology();
            var environment = new DistributedLocalEnvironment();
            environment.NodeTopology = topology;
            Dictionary<ComputeNode, DistributedIndexer> indexers = example.CreateIndexers(topology);
            var localToGlobalMaps = example.CreateLocalToGlobalMaps(topology);

            double[,] globalA =
            {
                { 25, 10,  0,  0,  0, 15 },
                { 10, 21, 11,  0,  0,  0 },
                {  0, 11, 23, 12,  0,  0 },
                {  0,  0, 12, 25, 13,  0 },
                {  0,  0,  0, 13, 27, 14 },
                { 15,  0,  0,  0, 14, 29 }
            };
            var localA = new Dictionary<ComputeNode, Matrix>();
            localA[topology.Nodes[0]] = Matrix.CreateFromArray(new double[,]
            {
                { 10, 10,  0 },
                { 10, 21, 11 },
                {  0, 11, 11 }
            });
            localA[topology.Nodes[1]] = Matrix.CreateFromArray(new double[,]
            {
                { 12, 12,  0 },
                { 12, 25, 13 },
                {  0, 13, 13 }
            });
            localA[topology.Nodes[2]] = Matrix.CreateFromArray(new double[,]
            {
                { 14, 14,  0 },
                { 14, 29, 15 },
                {  0, 15, 15 }
            });
            var distributedA = new DistributedOverlappingMatrix(environment, indexers, localA);

            double[] globalX = { 0.0, 1.0, 2.0, 3.0, 4.0, 5.0 };
            Dictionary<ComputeNode, Vector> localX = Utilities.GlobalToLocalVectors(globalX, localToGlobalMaps);
            var distributedX = new DistributedOverlappingVector(environment, indexers, localX);

            double[] globalYExpected = { 85.0, 43.0, 93.0, 151.0, 217.0, 201.0 };
            Dictionary<ComputeNode, Vector> localYExpected = Utilities.GlobalToLocalVectors(globalYExpected, localToGlobalMaps);
            var distributedYExpected = new DistributedOverlappingVector(environment, indexers, localYExpected);

            var distributedY = new DistributedOverlappingVector(environment, indexers);
            distributedA.MultiplyVector(distributedX, distributedY);

            double tol = 1E-13;
            Assert.True(distributedYExpected.Equals(distributedY, tol));
        }
        
    }
}
