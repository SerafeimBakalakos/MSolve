using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.LinearAlgebra;
using ISAAR.MSolve.LinearAlgebra.Iterative;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using MGroup.Solvers.Distributed.Environments;
using MGroup.Solvers.Distributed.LinearAlgebra;
using MGroup.Solvers.Distributed.Topologies;
using Xunit;

namespace MGroup.Solvers.Tests.Distributed.LinearAlgebra
{
    public static class Hexagon1DTopologyTests
    {
        [Fact]
        public static void TestAxpy()
        {
            var example = new Hexagon1DTopology();
            ComputeNodeTopology topology = example.CreateNodeTopology();
            var environment = new DistributedLocalEnvironment();
            environment.NodeTopology = topology;
            Dictionary<ComputeNode, DistributedIndexer> indexers = example.CreateIndexers(topology);
            var localToGlobalMaps = example.CreateLocalToGlobalMaps(topology);

            double[] globalX = { 0.0, 1.0, 2.0, 3.0, 4.0, 5.0 };
            Dictionary<ComputeNode, Vector> localX = Utilities.GlobalToLocalVectors(globalX, localToGlobalMaps);
            var distributedX = new DistributedOverlappingVector(environment, indexers, localX);

            double[] globalY = { 10.0, 11.0, 12.0, 13.0, 14.0, 15.0 };
            Dictionary<ComputeNode, Vector> localY = Utilities.GlobalToLocalVectors(globalY, localToGlobalMaps);
            var distributedY = new DistributedOverlappingVector(environment, indexers, localY);

            double[] globalZExpected = { 20.0, 23.0, 26.0, 29.0, 32.0, 35.0 };
            Dictionary<ComputeNode, Vector> localZExpected = Utilities.GlobalToLocalVectors(globalZExpected, localToGlobalMaps);
            var distributedZExpected = new DistributedOverlappingVector(environment, indexers, localZExpected);

            DistributedOverlappingVector distributedZ = distributedX.Copy();
            distributedZ.AxpyIntoThis(distributedY, 2.0);

            double tol = 1E-13;
            Assert.True(distributedZExpected.Equals(distributedZ, tol));
        }

        [Fact]
        public static void TestDotProduct()
        {
            var example = new Hexagon1DTopology();
            ComputeNodeTopology topology = example.CreateNodeTopology();
            var environment = new DistributedLocalEnvironment();
            environment.NodeTopology = topology;
            Dictionary<ComputeNode, DistributedIndexer> indexers = example.CreateIndexers(topology);
            var localToGlobalMaps = example.CreateLocalToGlobalMaps(topology);

            double[] globalX = { 0.0, 1.0, 2.0, 3.0, 4.0, 5.0 };
            Dictionary<ComputeNode, Vector> localX = Utilities.GlobalToLocalVectors(globalX, localToGlobalMaps);
            var distributedX = new DistributedOverlappingVector(environment, indexers, localX);

            double[] globalY = { 10.0, 11.0, 12.0, 13.0, 14.0, 15.0 };
            Dictionary<ComputeNode, Vector> localY = Utilities.GlobalToLocalVectors(globalY, localToGlobalMaps);
            var distributedY = new DistributedOverlappingVector(environment, indexers, localY);

            double dotExpected = globalX.DotProduct(globalY);
            double dot = distributedX.DotProduct(distributedY);

            int precision = 8;
            Assert.Equal(dotExpected, dot, precision);
        }

        [Fact]
        public static void TestEquals()
        {
            var example = new Hexagon1DTopology();
            ComputeNodeTopology topology = example.CreateNodeTopology();
            var environment = new DistributedLocalEnvironment();
            environment.NodeTopology = topology;
            Dictionary<ComputeNode, DistributedIndexer> indexers = example.CreateIndexers(topology);
            var localToGlobalMaps = example.CreateLocalToGlobalMaps(topology);

            var localVectors1 = new Dictionary<ComputeNode, Vector>();
            localVectors1[environment.NodeTopology.Nodes[0]] = Vector.CreateFromArray(new double[] { 0.0, 1.0, 2.0 });
            localVectors1[environment.NodeTopology.Nodes[1]] = Vector.CreateFromArray(new double[] { 2.0, 3.0, 4.0 });
            localVectors1[environment.NodeTopology.Nodes[2]] = Vector.CreateFromArray(new double[] { 4.0, 5.0, 0.0 });
            var distributedVector1 = new DistributedOverlappingVector(environment, indexers, localVectors1);

            double[] globalVector = { 0.0, 1.0, 2.0, 3.0, 4.0, 5.0 };
            Dictionary<ComputeNode, Vector> localVectors2 = Utilities.GlobalToLocalVectors(globalVector, localToGlobalMaps);
            var distributedVector2 = new DistributedOverlappingVector(environment, indexers, localVectors2);

            double tol = 1E-13;
            Assert.True(distributedVector1.Equals(distributedVector2, tol));
        }

        [Fact]
        public static void TestLinearCombination()
        {
            var example = new Hexagon1DTopology();
            ComputeNodeTopology topology = example.CreateNodeTopology();
            var environment = new DistributedLocalEnvironment();
            environment.NodeTopology = topology;
            Dictionary<ComputeNode, DistributedIndexer> indexers = example.CreateIndexers(topology);
            var localToGlobalMaps = example.CreateLocalToGlobalMaps(topology);

            double[] globalX = { 0.0, 1.0, 2.0, 3.0, 4.0, 5.0 };
            Dictionary<ComputeNode, Vector> localX = Utilities.GlobalToLocalVectors(globalX, localToGlobalMaps);
            var distributedX = new DistributedOverlappingVector(environment, indexers, localX);

            double[] globalY = { 10.0, 11.0, 12.0, 13.0, 14.0, 15.0 };
            Dictionary<ComputeNode, Vector> localY = Utilities.GlobalToLocalVectors(globalY, localToGlobalMaps);
            var distributedY = new DistributedOverlappingVector(environment, indexers, localY);

            double[] globalZExpected = { 30.0, 35.0, 40.0, 45.0, 50.0, 55.0 };
            Dictionary<ComputeNode, Vector> localZExpected = Utilities.GlobalToLocalVectors(globalZExpected, localToGlobalMaps);
            var distributedZExpected = new DistributedOverlappingVector(environment, indexers, localZExpected);

            DistributedOverlappingVector distributedZ = distributedX.Copy();
            distributedZ.LinearCombinationIntoThis(2.0, distributedY, 3.0);

            double tol = 1E-13;
            Assert.True(distributedZExpected.Equals(distributedZ, tol));
        }



        [Fact]
        public static void TestRhsVectorConvertion()
        {
            var example = new Hexagon1DTopology();
            ComputeNodeTopology topology = example.CreateNodeTopology();
            var environment = new DistributedLocalEnvironment();
            environment.NodeTopology = topology;
            Dictionary<ComputeNode, DistributedIndexer> indexers = example.CreateIndexers(topology);
            var localToGlobalMaps = example.CreateLocalToGlobalMaps(topology);

            double[] globalExpected = { 28.0, 11.0, 25.0, 14.0, 31.0, 17.0 };
            Dictionary<ComputeNode, Vector> localExpected = Utilities.GlobalToLocalVectors(globalExpected, localToGlobalMaps);
            var distributedExpected = new DistributedOverlappingVector(environment, indexers, localExpected);

            var localRhs = new Dictionary<ComputeNode, Vector>();
            localRhs[environment.NodeTopology.Nodes[0]] = Vector.CreateFromArray(new double[] { 10.0, 11.0, 12.0 });
            localRhs[environment.NodeTopology.Nodes[1]] = Vector.CreateFromArray(new double[] { 13.0, 14.0, 15.0 });
            localRhs[environment.NodeTopology.Nodes[2]] = Vector.CreateFromArray(new double[] { 16.0, 17.0, 18.0 });
            var distributedComputed = new DistributedOverlappingVector(environment, indexers, localRhs);
            distributedComputed.ConvertRhsToLhsVector();

            double tol = 1E-13;
            Assert.True(distributedExpected.Equals(distributedComputed, tol));
        }

        [Fact]
        public static void TestScale()
        {
            var example = new Hexagon1DTopology();
            ComputeNodeTopology topology = example.CreateNodeTopology();
            var environment = new DistributedLocalEnvironment();
            environment.NodeTopology = topology;
            Dictionary<ComputeNode, DistributedIndexer> indexers = example.CreateIndexers(topology);
            var localToGlobalMaps = example.CreateLocalToGlobalMaps(topology);

            double[] globalX = { 10.0, 11.0, 12.0, 13.0, 14.0, 15.0 };
            Dictionary<ComputeNode, Vector> localX = Utilities.GlobalToLocalVectors(globalX, localToGlobalMaps);
            var distributedX = new DistributedOverlappingVector(environment, indexers, localX);

            double[] globalZExpected = { -30.0, -33.0, -36.0, -39.0, -42.0, -45.0 };
            Dictionary<ComputeNode, Vector> localZExpected = Utilities.GlobalToLocalVectors(globalZExpected, localToGlobalMaps);
            var distributedZExpected = new DistributedOverlappingVector(environment, indexers, localZExpected);

            DistributedOverlappingVector distributedZ = distributedX.Copy();
            distributedZ.ScaleIntoThis(-3.0);

            double tol = 1E-13;
            Assert.True(distributedZExpected.Equals(distributedZ, tol));
        }

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
            var localA = new Dictionary<ComputeNode, ILinearTransformation>();
            localA[topology.Nodes[0]] = new ExplicitMatrixTransformation(Matrix.CreateFromArray(new double[,]
            {
                { 10, 10,  0 },
                { 10, 21, 11 },
                {  0, 11, 11 }
            }));
            localA[topology.Nodes[1]] = new ExplicitMatrixTransformation(Matrix.CreateFromArray(new double[,]
            {
                { 12, 12,  0 },
                { 12, 25, 13 },
                {  0, 13, 13 }
            }));
            localA[topology.Nodes[2]] = new ExplicitMatrixTransformation(Matrix.CreateFromArray(new double[,]
            {
                { 14, 14,  0 },
                { 14, 29, 15 },
                {  0, 15, 15 }
            }));
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

        [Fact]
        public static void TestMatrixVectorMultiplicationWithSubdomains()
        {
            var example = new Hexagon1DTopology();
            ComputeNodeTopology topology = example.CreateNodeTopology();
            var environment = new DistributedLocalEnvironment();
            environment.NodeTopology = topology;
            Dictionary<ComputeNode, DistributedIndexer> indexers = example.CreateIndexers(topology);
            var localToGlobalMaps = example.CreateLocalToGlobalMaps(topology);

            var localA = new Dictionary<ComputeNode, ILinearTransformation>();
            for (int c = 0; c < 3; ++c)
            {
                var clusterMatrix = new SubdomainClusterMatrix(3);
                int[] subdomainIDs = { 2 * c, 2 * c + 1 };
                clusterMatrix.SubdomainMatrices[subdomainIDs[0]] = Matrix.CreateWithValue(2, 2, 10.0 + subdomainIDs[0]);
                clusterMatrix.SubdomainMatrices[subdomainIDs[1]] = Matrix.CreateWithValue(2, 2, 10.0 + subdomainIDs[1]);
                clusterMatrix.SubdomainToClusterDofs[subdomainIDs[0]] = new int[] { 0, 1 };
                clusterMatrix.SubdomainToClusterDofs[subdomainIDs[1]] = new int[] { 1, 2 };
                localA[topology.Nodes[c]] = clusterMatrix;
            }
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
