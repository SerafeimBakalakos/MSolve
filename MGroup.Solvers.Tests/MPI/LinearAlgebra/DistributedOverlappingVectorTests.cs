using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.LinearAlgebra;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using MGroup.Solvers.MPI.Environments;
using MGroup.Solvers.MPI.LinearAlgebra;
using MGroup.Solvers.MPI.Topologies;
using Xunit;

namespace MGroup.Solvers.Tests.MPI.LinearAlgebra
{
    public static class DistributedOverlappingVectorTests
    {
        [Fact]
        public static void TestAxpy()
        {
            ComputeNodeTopology topology = CreateNodeTopology();
            var environment = new DistributedLocalEnvironment();
            environment.NodeTopology = topology;
            Dictionary<ComputeNode, DistributedIndexer> indexers = CreateIndexers(topology);
            var localToGlobalMaps = CreateLocalToGlobalMaps(environment);

            double[] globalX = { 0.0, 1.0, 2.0, 3.0, 4.0, 5.0 };
            Dictionary<ComputeNode, Vector> localX = Utilities.GlobalToLocalVectors(globalX, localToGlobalMaps);
            var distributedX = DistributedOverlappingVector.CreateLhsVector(environment, indexers, localX);

            double[] globalY = { 10.0, 11.0, 12.0, 13.0, 14.0, 15.0 };
            Dictionary<ComputeNode, Vector> localY = Utilities.GlobalToLocalVectors(globalY, localToGlobalMaps);
            var distributedY = DistributedOverlappingVector.CreateLhsVector(environment, indexers, localY);

            double[] globalZExpected = { 20.0, 23.0, 26.0, 29.0, 32.0, 35.0 };
            Dictionary<ComputeNode, Vector> localZExpected = Utilities.GlobalToLocalVectors(globalZExpected, localToGlobalMaps);
            var distributedZExpected = DistributedOverlappingVector.CreateLhsVector(environment, indexers, localZExpected);

            DistributedOverlappingVector distributedZ = distributedX.Copy();
            distributedZ.AxpyIntoThis(distributedY, 2.0);

            double tol = 1E-13;
            Assert.True(distributedZExpected.Equals(distributedZ, tol));
        }

        [Fact]
        public static void TestDotProduct()
        {
            ComputeNodeTopology topology = CreateNodeTopology();
            var environment = new DistributedLocalEnvironment();
            environment.NodeTopology = topology;
            Dictionary<ComputeNode, DistributedIndexer> indexers = CreateIndexers(topology);
            var localToGlobalMaps = CreateLocalToGlobalMaps(environment);

            double[] globalX = { 0.0, 1.0, 2.0, 3.0, 4.0, 5.0 };
            Dictionary<ComputeNode, Vector> localX = Utilities.GlobalToLocalVectors(globalX, localToGlobalMaps);
            var distributedX = DistributedOverlappingVector.CreateLhsVector(environment, indexers, localX);

            double[] globalY = { 10.0, 11.0, 12.0, 13.0, 14.0, 15.0 };
            Dictionary<ComputeNode, Vector> localY = Utilities.GlobalToLocalVectors(globalY, localToGlobalMaps);
            var distributedY = DistributedOverlappingVector.CreateLhsVector(environment, indexers, localY);

            double dotExpected = globalX.DotProduct(globalY);
            double dot = distributedX.DotProduct(distributedY);

            int precision = 8;
            Assert.Equal(dotExpected, dot, precision);
        }

        [Fact]
        public static void TestEquals()
        {
            ComputeNodeTopology topology = CreateNodeTopology();
            var environment = new DistributedLocalEnvironment();
            environment.NodeTopology = topology;
            Dictionary<ComputeNode, DistributedIndexer> indexers = CreateIndexers(topology);
            var localToGlobalMaps = CreateLocalToGlobalMaps(environment);

            var localVectors1 = new Dictionary<ComputeNode, Vector>();
            localVectors1[environment.NodeTopology.Nodes[0]] = Vector.CreateFromArray(new double[] { 0.0, 1.0, 2.0 });
            localVectors1[environment.NodeTopology.Nodes[1]] = Vector.CreateFromArray(new double[] { 2.0, 3.0, 4.0 });
            localVectors1[environment.NodeTopology.Nodes[2]] = Vector.CreateFromArray(new double[] { 4.0, 5.0, 0.0 });
            var distributedVector1 = DistributedOverlappingVector.CreateLhsVector(environment, indexers, localVectors1);

            double[] globalVector = { 0.0, 1.0, 2.0, 3.0, 4.0, 5.0 };
            Dictionary<ComputeNode, Vector> localVectors2 = Utilities.GlobalToLocalVectors(globalVector, localToGlobalMaps);
            var distributedVector2 = DistributedOverlappingVector.CreateLhsVector(environment, indexers, localVectors2);

            double tol = 1E-13;
            Assert.True(distributedVector1.Equals(distributedVector2, tol));
        }

        [Fact]
        public static void TestLinearCombination()
        {
            ComputeNodeTopology topology = CreateNodeTopology();
            var environment = new DistributedLocalEnvironment();
            environment.NodeTopology = topology;
            Dictionary<ComputeNode, DistributedIndexer> indexers = CreateIndexers(topology);
            var localToGlobalMaps = CreateLocalToGlobalMaps(environment);

            double[] globalX = { 0.0, 1.0, 2.0, 3.0, 4.0, 5.0 };
            Dictionary<ComputeNode, Vector> localX = Utilities.GlobalToLocalVectors(globalX, localToGlobalMaps);
            var distributedX = DistributedOverlappingVector.CreateLhsVector(environment, indexers, localX);

            double[] globalY = { 10.0, 11.0, 12.0, 13.0, 14.0, 15.0 };
            Dictionary<ComputeNode, Vector> localY = Utilities.GlobalToLocalVectors(globalY, localToGlobalMaps);
            var distributedY = DistributedOverlappingVector.CreateLhsVector(environment, indexers, localY);

            double[] globalZExpected = { 30.0, 35.0, 40.0, 45.0, 50.0, 55.0 };
            Dictionary<ComputeNode, Vector> localZExpected = Utilities.GlobalToLocalVectors(globalZExpected, localToGlobalMaps);
            var distributedZExpected = DistributedOverlappingVector.CreateLhsVector(environment, indexers, localZExpected);

            DistributedOverlappingVector distributedZ = distributedX.Copy();
            distributedZ.LinearCombinationIntoThis(2.0, distributedY, 3.0);

            double tol = 1E-13;
            Assert.True(distributedZExpected.Equals(distributedZ, tol));
        }

        [Fact]
        public static void TestRhsVectorConvertion()
        {
            ComputeNodeTopology topology = CreateNodeTopology();
            var environment = new DistributedLocalEnvironment();
            environment.NodeTopology = topology;
            Dictionary<ComputeNode, DistributedIndexer> indexers = CreateIndexers(topology);
            var localToGlobalMaps = CreateLocalToGlobalMaps(environment);

            double[] globalExpected = { 28.0, 11.0, 25.0, 14.0, 31.0, 17.0 };
            Dictionary<ComputeNode, Vector> localExpected = Utilities.GlobalToLocalVectors(globalExpected, localToGlobalMaps);
            var distributedExpected = DistributedOverlappingVector.CreateLhsVector(environment, indexers, localExpected);

            var localRhs = new Dictionary<ComputeNode, Vector>();
            localRhs[environment.NodeTopology.Nodes[0]] = Vector.CreateFromArray(new double[] { 10.0, 11.0, 12.0 });
            localRhs[environment.NodeTopology.Nodes[1]] = Vector.CreateFromArray(new double[] { 13.0, 14.0, 15.0 });
            localRhs[environment.NodeTopology.Nodes[2]] = Vector.CreateFromArray(new double[] { 16.0, 17.0, 18.0 });
            var distributedComputed = DistributedOverlappingVector.CreateRhsVector(environment, indexers, localRhs);

            double tol = 1E-13;
            Assert.True(distributedExpected.Equals(distributedComputed, tol));
        }

        [Fact]
        public static void TestScale()
        {
            ComputeNodeTopology topology = CreateNodeTopology();
            var environment = new DistributedLocalEnvironment();
            environment.NodeTopology = topology;
            Dictionary<ComputeNode, DistributedIndexer> indexers = CreateIndexers(topology);
            var localToGlobalMaps = CreateLocalToGlobalMaps(environment);

            double[] globalX = { 10.0, 11.0, 12.0, 13.0, 14.0, 15.0 };
            Dictionary<ComputeNode, Vector> localX = Utilities.GlobalToLocalVectors(globalX, localToGlobalMaps);
            var distributedX = DistributedOverlappingVector.CreateLhsVector(environment, indexers, localX);

            double[] globalZExpected = { -30.0, -33.0, -36.0, -39.0, -42.0, -45.0 };
            Dictionary<ComputeNode, Vector> localZExpected = Utilities.GlobalToLocalVectors(globalZExpected, localToGlobalMaps);
            var distributedZExpected = DistributedOverlappingVector.CreateLhsVector(environment, indexers, localZExpected);

            DistributedOverlappingVector distributedZ = distributedX.Copy();
            distributedZ.ScaleIntoThis(-3.0);

            double tol = 1E-13;
            Assert.True(distributedZExpected.Equals(distributedZ, tol));
        }

        private static ComputeNodeTopology CreateNodeTopology()
        {
            // Compute nodes
            var topology = new ComputeNodeTopology();
            topology.Nodes[0] = new ComputeNode(0);
            topology.Nodes[1] = new ComputeNode(1);
            topology.Nodes[2] = new ComputeNode(2);
            topology.Boundaries[0] = new ComputeNodeBoundary(0, new ComputeNode[] { topology.Nodes[2], topology.Nodes[0] });
            topology.Boundaries[1] = new ComputeNodeBoundary(1, new ComputeNode[] { topology.Nodes[0], topology.Nodes[1] });
            topology.Boundaries[2] = new ComputeNodeBoundary(2, new ComputeNode[] { topology.Nodes[1], topology.Nodes[2] });
            topology.ConnectData();

            return topology;
        }

        private static Dictionary<ComputeNode, DistributedIndexer> CreateIndexers(ComputeNodeTopology topology)
        {
            var indexers = new Dictionary<ComputeNode, DistributedIndexer>();

            var indexer0 = new DistributedIndexer();
            indexer0.Node = topology.Nodes[0];
            var boundaryEntries0 = new Dictionary<ComputeNodeBoundary, int[]>();
            boundaryEntries0[topology.Boundaries[0]] = new int[] { 0 };
            boundaryEntries0[topology.Boundaries[1]] = new int[] { 2 };
            indexer0.Initialize(3, boundaryEntries0);
            indexers[indexer0.Node] = indexer0;

            var indexer1 = new DistributedIndexer();
            indexer1.Node = topology.Nodes[1];
            var boundaryEntries1 = new Dictionary<ComputeNodeBoundary, int[]>();
            boundaryEntries1[topology.Boundaries[1]] = new int[] { 0 };
            boundaryEntries1[topology.Boundaries[2]] = new int[] { 2 };
            indexer1.Initialize(3, boundaryEntries1);
            indexers[indexer1.Node] = indexer1;

            var indexer2 = new DistributedIndexer();
            indexer2.Node = topology.Nodes[2];
            var boundaryEntries2 = new Dictionary<ComputeNodeBoundary, int[]>();
            boundaryEntries2[topology.Boundaries[2]] = new int[] { 0 };
            boundaryEntries2[topology.Boundaries[0]] = new int[] { 2 };
            indexer2.Initialize(3, boundaryEntries2);
            indexers[indexer2.Node] = indexer2;

            return indexers;
        }

        private static Dictionary<ComputeNode, int[]> CreateLocalToGlobalMaps(DistributedLocalEnvironment environment)
        {
            var localToGlobalMaps = new Dictionary<ComputeNode, int[]>();
            localToGlobalMaps[environment.NodeTopology.Nodes[0]] = new int[] { 0, 1, 2 };
            localToGlobalMaps[environment.NodeTopology.Nodes[1]] = new int[] { 2, 3, 4 };
            localToGlobalMaps[environment.NodeTopology.Nodes[2]] = new int[] { 4, 5, 0 };
            return localToGlobalMaps;
        }
    }
}
