using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.LinearAlgebra;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using MGroup.Solvers.MPI.Environment;
using MGroup.Solvers.MPI.LinearAlgebra;
using Xunit;

namespace MGroup.Solvers.Tests.MPI.LinearAlgebra
{
    public static class DistributedOverlappingVectorTests
    {
        [Fact]
        public static void TestAxpy()
        {
            DistributedLocalEnvironment environment = CreateEnvironment();
            Dictionary<ComputeNode, DistributedIndexer> indexers = CreateIndexers(environment);
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
            DistributedLocalEnvironment environment = CreateEnvironment();
            Dictionary<ComputeNode, DistributedIndexer> indexers = CreateIndexers(environment);
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
            DistributedLocalEnvironment environment = CreateEnvironment();
            Dictionary<ComputeNode, DistributedIndexer> indexers = CreateIndexers(environment);
            var localToGlobalMaps = CreateLocalToGlobalMaps(environment);

            var localVectors1 = new Dictionary<ComputeNode, Vector>();
            localVectors1[environment.ComputeNodes[0]] = Vector.CreateFromArray(new double[] { 0.0, 1.0, 2.0 });
            localVectors1[environment.ComputeNodes[1]] = Vector.CreateFromArray(new double[] { 2.0, 3.0, 4.0 });
            localVectors1[environment.ComputeNodes[2]] = Vector.CreateFromArray(new double[] { 4.0, 5.0, 0.0 });
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
            DistributedLocalEnvironment environment = CreateEnvironment();
            Dictionary<ComputeNode, DistributedIndexer> indexers = CreateIndexers(environment);
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
            DistributedLocalEnvironment environment = CreateEnvironment();
            Dictionary<ComputeNode, DistributedIndexer> indexers = CreateIndexers(environment);
            var localToGlobalMaps = CreateLocalToGlobalMaps(environment);

            double[] globalExpected = { 28.0, 11.0, 25.0, 14.0, 31.0, 17.0 };
            Dictionary<ComputeNode, Vector> localExpected = Utilities.GlobalToLocalVectors(globalExpected, localToGlobalMaps);
            var distributedExpected = DistributedOverlappingVector.CreateLhsVector(environment, indexers, localExpected);

            var localRhs = new Dictionary<ComputeNode, Vector>();
            localRhs[environment.ComputeNodes[0]] = Vector.CreateFromArray(new double[] { 10.0, 11.0, 12.0 });
            localRhs[environment.ComputeNodes[1]] = Vector.CreateFromArray(new double[] { 13.0, 14.0, 15.0 });
            localRhs[environment.ComputeNodes[2]] = Vector.CreateFromArray(new double[] { 16.0, 17.0, 18.0 });
            var distributedComputed = DistributedOverlappingVector.CreateRhsVector(environment, indexers, localRhs);

            double tol = 1E-13;
            Assert.True(distributedExpected.Equals(distributedComputed, tol));
        }

        [Fact]
        public static void TestScale()
        {
            DistributedLocalEnvironment environment = CreateEnvironment();
            Dictionary<ComputeNode, DistributedIndexer> indexers = CreateIndexers(environment);
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

        private static DistributedLocalEnvironment CreateEnvironment()
        {
            //TODOMPI: automate this process as much as possible (e.g. just specify neighbors)

            // Environment
            var environment = new DistributedLocalEnvironment();

            // Compute nodes
            environment.ComputeNodes.Add(new ComputeNode(0));
            environment.ComputeNodes.Add(new ComputeNode(1));
            environment.ComputeNodes.Add(new ComputeNode(2));

            // Compute node neighbors
            environment.ComputeNodes[0].Neighbors.Add(environment.ComputeNodes[2]);
            environment.ComputeNodes[0].Neighbors.Add(environment.ComputeNodes[1]);

            environment.ComputeNodes[1].Neighbors.Add(environment.ComputeNodes[0]);
            environment.ComputeNodes[1].Neighbors.Add(environment.ComputeNodes[2]);

            environment.ComputeNodes[2].Neighbors.Add(environment.ComputeNodes[1]);
            environment.ComputeNodes[2].Neighbors.Add(environment.ComputeNodes[0]);

            // Boundaries between compute nodes.
            var boundary0 = new ComputeNodeBoundary();
            boundary0.Nodes.Add(environment.ComputeNodes[2]);
            boundary0.Nodes.Add(environment.ComputeNodes[0]);

            var boundary1 = new ComputeNodeBoundary();
            boundary1.Nodes.Add(environment.ComputeNodes[0]);
            boundary1.Nodes.Add(environment.ComputeNodes[1]);

            var boundary2 = new ComputeNodeBoundary();
            boundary2.Nodes.Add(environment.ComputeNodes[1]);
            boundary2.Nodes.Add(environment.ComputeNodes[2]);

            // Inform each compute node about its boundaries.
            environment.ComputeNodes[0].Boundaries.Add(boundary0);
            environment.ComputeNodes[0].Boundaries.Add(boundary1);

            environment.ComputeNodes[1].Boundaries.Add(boundary1);
            environment.ComputeNodes[1].Boundaries.Add(boundary2);

            environment.ComputeNodes[2].Boundaries.Add(boundary2);
            environment.ComputeNodes[2].Boundaries.Add(boundary0);

            return environment;
        }

        private static Dictionary<ComputeNode, DistributedIndexer> CreateIndexers(DistributedLocalEnvironment environment)
        {
            var indexers = new Dictionary<ComputeNode, DistributedIndexer>();

            var indexer0 = new DistributedIndexer();
            indexer0.ComputeNode = environment.ComputeNodes[0];
            indexer0.InternalEntries = new int[] { 1 };
            indexer0.BoundaryEntries = new List<int[]>();
            indexer0.BoundaryEntries.Add(new int[] { 0 });
            indexer0.BoundaryEntries.Add(new int[] { 2 });
            indexer0.NeighborCommonEntries = new List<int[]>();
            indexer0.NeighborCommonEntries.Add(new int[] { 0 });
            indexer0.NeighborCommonEntries.Add(new int[] { 2 });
            indexers[indexer0.ComputeNode] = indexer0;

            var indexer1 = new DistributedIndexer();
            indexer1.ComputeNode = environment.ComputeNodes[1];
            indexer1.InternalEntries = new int[] { 1 };
            indexer1.BoundaryEntries = new List<int[]>();
            indexer1.BoundaryEntries.Add(new int[] { 0 });
            indexer1.BoundaryEntries.Add(new int[] { 2 });
            indexer1.NeighborCommonEntries = new List<int[]>();
            indexer1.NeighborCommonEntries.Add(new int[] { 0 });
            indexer1.NeighborCommonEntries.Add(new int[] { 2 });
            indexers[indexer1.ComputeNode] = indexer1;

            var indexer2 = new DistributedIndexer();
            indexer2.ComputeNode = environment.ComputeNodes[2];
            indexer2.InternalEntries = new int[] { 1 };
            indexer2.BoundaryEntries = new List<int[]>();
            indexer2.BoundaryEntries.Add(new int[] { 0 });
            indexer2.BoundaryEntries.Add(new int[] { 2 });
            indexer2.NeighborCommonEntries = new List<int[]>();
            indexer2.NeighborCommonEntries.Add(new int[] { 0 });
            indexer2.NeighborCommonEntries.Add(new int[] { 2 });
            indexers[indexer2.ComputeNode] = indexer2;

            return indexers;
        }

        private static Dictionary<ComputeNode, int[]> CreateLocalToGlobalMaps(DistributedLocalEnvironment environment)
        {
            var localToGlobalMaps = new Dictionary<ComputeNode, int[]>();
            localToGlobalMaps[environment.ComputeNodes[0]] = new int[] { 0, 1, 2 };
            localToGlobalMaps[environment.ComputeNodes[1]] = new int[] { 2, 3, 4 };
            localToGlobalMaps[environment.ComputeNodes[2]] = new int[] { 4, 5, 0 };
            return localToGlobalMaps;
        }
    }
}
