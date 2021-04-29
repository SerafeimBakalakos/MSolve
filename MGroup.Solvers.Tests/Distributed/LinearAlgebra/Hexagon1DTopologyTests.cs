using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.LinearAlgebra;
using ISAAR.MSolve.LinearAlgebra.Iterative;
using ISAAR.MSolve.LinearAlgebra.Iterative.Termination;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using MGroup.Solvers.Distributed.Environments;
using MGroup.Solvers.Distributed.LinearAlgebra;
using MGroup.Solvers.Distributed.LinearAlgebra.IterativeMethods;
using MGroup.Solvers.Distributed.LinearAlgebra.IterativeMethods.Preconditioning;
using MGroup.Solvers.Distributed.Topologies;
using Xunit;

namespace MGroup.Solvers.Tests.Distributed.LinearAlgebra
{
    public static class Hexagon1DTopologyTests
    {
        public static IEnumerable<object[]> GetEnvironments()
        {
            yield return new object[] { new DistributedLocalEnvironment(true) };
        }

        [Theory]
        [MemberData(nameof(GetEnvironments))]
        public static void TestAxpyVectors(IComputeEnvironment environment)
        {
            var example = new Hexagon1DTopology();
            ComputeNodeTopology topology = example.CreateNodeTopology();
            environment.NodeTopology = topology;
            Dictionary<ComputeNode, DistributedIndexer> indexers = example.CreateIndexers(environment, topology);
            var localToGlobalMaps = example.CreateLocalToGlobalMaps(environment, topology);

            double[] globalX = { 0.0, 1.0, 2.0, 3.0, 4.0, 5.0 };
            Dictionary<ComputeNode, Vector> localX = Utilities.GlobalToLocalVectors(globalX, localToGlobalMaps);
            Utilities.FilterNodeData(environment, localX);
            var distributedX = new DistributedOverlappingVector(environment, indexers, localX);

            double[] globalY = { 10.0, 11.0, 12.0, 13.0, 14.0, 15.0 };
            Dictionary<ComputeNode, Vector> localY = Utilities.GlobalToLocalVectors(globalY, localToGlobalMaps);
            Utilities.FilterNodeData(environment, localY);
            var distributedY = new DistributedOverlappingVector(environment, indexers, localY);

            double[] globalZExpected = { 20.0, 23.0, 26.0, 29.0, 32.0, 35.0 };
            Dictionary<ComputeNode, Vector> localZExpected = Utilities.GlobalToLocalVectors(globalZExpected, localToGlobalMaps);
            Utilities.FilterNodeData(environment, localZExpected);
            var distributedZExpected = new DistributedOverlappingVector(environment, indexers, localZExpected);

            DistributedOverlappingVector distributedZ = distributedX.Copy();
            distributedZ.AxpyIntoThis(distributedY, 2.0);

            double tol = 1E-13;
            Assert.True(distributedZExpected.Equals(distributedZ, tol));
        }

        [Theory]
        [MemberData(nameof(GetEnvironments))]
        public static void TestDotProduct(IComputeEnvironment environment)
        {
            var example = new Hexagon1DTopology();
            ComputeNodeTopology topology = example.CreateNodeTopology();
            environment.NodeTopology = topology;
            Dictionary<ComputeNode, DistributedIndexer> indexers = example.CreateIndexers(environment, topology);
            var localToGlobalMaps = example.CreateLocalToGlobalMaps(environment, topology);

            double[] globalX = { 0.0, 1.0, 2.0, 3.0, 4.0, 5.0 };
            Dictionary<ComputeNode, Vector> localX = Utilities.GlobalToLocalVectors(globalX, localToGlobalMaps);
            Utilities.FilterNodeData(environment, localX);
            var distributedX = new DistributedOverlappingVector(environment, indexers, localX);

            double[] globalY = { 10.0, 11.0, 12.0, 13.0, 14.0, 15.0 };
            Dictionary<ComputeNode, Vector> localY = Utilities.GlobalToLocalVectors(globalY, localToGlobalMaps);
            Utilities.FilterNodeData(environment, localY);
            var distributedY = new DistributedOverlappingVector(environment, indexers, localY);

            double dotExpected = 205/*globalX.DotProduct(globalY)*/;
            double dot = distributedX.DotProduct(distributedY);

            int precision = 8;
            Assert.Equal(dotExpected, dot, precision);
        }

        [Theory]
        [MemberData(nameof(GetEnvironments))]
        public static void TestEqualVectors(IComputeEnvironment environment)
        {
            var example = new Hexagon1DTopology();
            ComputeNodeTopology topology = example.CreateNodeTopology();
            environment.NodeTopology = topology;
            Dictionary<ComputeNode, DistributedIndexer> indexers = example.CreateIndexers(environment, topology);
            var localToGlobalMaps = example.CreateLocalToGlobalMaps(environment, topology);

            var localVectors1 = new Dictionary<ComputeNode, Vector>();
            localVectors1[environment.NodeTopology.Nodes[0]] = Vector.CreateFromArray(new double[] { 0.0, 1.0, 2.0 });
            localVectors1[environment.NodeTopology.Nodes[1]] = Vector.CreateFromArray(new double[] { 2.0, 3.0, 4.0 });
            localVectors1[environment.NodeTopology.Nodes[2]] = Vector.CreateFromArray(new double[] { 4.0, 5.0, 0.0 });
            Utilities.FilterNodeData(environment, localVectors1);
            var distributedVector1 = new DistributedOverlappingVector(environment, indexers, localVectors1);

            double[] globalVector = { 0.0, 1.0, 2.0, 3.0, 4.0, 5.0 };
            Dictionary<ComputeNode, Vector> localVectors2 = Utilities.GlobalToLocalVectors(globalVector, localToGlobalMaps);
            Utilities.FilterNodeData(environment, localVectors2);
            var distributedVector2 = new DistributedOverlappingVector(environment, indexers, localVectors2);

            double tol = 1E-13;
            Assert.True(distributedVector1.Equals(distributedVector2, tol));
        }

        [Theory]
        [MemberData(nameof(GetEnvironments))]
        public static void TestLinearCombinationVectors(IComputeEnvironment environment)
        {
            var example = new Hexagon1DTopology();
            ComputeNodeTopology topology = example.CreateNodeTopology();
            environment.NodeTopology = topology;
            Dictionary<ComputeNode, DistributedIndexer> indexers = example.CreateIndexers(environment, topology);
            var localToGlobalMaps = example.CreateLocalToGlobalMaps(environment, topology);

            double[] globalX = { 0.0, 1.0, 2.0, 3.0, 4.0, 5.0 };
            Dictionary<ComputeNode, Vector> localX = Utilities.GlobalToLocalVectors(globalX, localToGlobalMaps);
            Utilities.FilterNodeData(environment, localX);
            var distributedX = new DistributedOverlappingVector(environment, indexers, localX);

            double[] globalY = { 10.0, 11.0, 12.0, 13.0, 14.0, 15.0 };
            Dictionary<ComputeNode, Vector> localY = Utilities.GlobalToLocalVectors(globalY, localToGlobalMaps);
            Utilities.FilterNodeData(environment, localY);
            var distributedY = new DistributedOverlappingVector(environment, indexers, localY);

            double[] globalZExpected = { 30.0, 35.0, 40.0, 45.0, 50.0, 55.0 };
            Dictionary<ComputeNode, Vector> localZExpected = Utilities.GlobalToLocalVectors(globalZExpected, localToGlobalMaps);
            Utilities.FilterNodeData(environment, localZExpected);
            var distributedZExpected = new DistributedOverlappingVector(environment, indexers, localZExpected);

            DistributedOverlappingVector distributedZ = distributedX.Copy();
            distributedZ.LinearCombinationIntoThis(2.0, distributedY, 3.0);

            double tol = 1E-13;
            Assert.True(distributedZExpected.Equals(distributedZ, tol));
        }

        [Theory]
        [MemberData(nameof(GetEnvironments))]
        public static void TestMatrixVectorMultiplication(IComputeEnvironment environment)
        {
            var example = new Hexagon1DTopology();
            ComputeNodeTopology topology = example.CreateNodeTopology();
            environment.NodeTopology = topology;
            Dictionary<ComputeNode, DistributedIndexer> indexers = example.CreateIndexers(environment, topology);
            var localToGlobalMaps = example.CreateLocalToGlobalMaps(environment, topology);

            double[,] globalA =
            {
                { 25, 10,  0,  0,  0, 15 },
                { 10, 21, 11,  0,  0,  0 },
                {  0, 11, 23, 12,  0,  0 },
                {  0,  0, 12, 25, 13,  0 },
                {  0,  0,  0, 13, 27, 14 },
                { 15,  0,  0,  0, 14, 100 }
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
                { 14, 100, 15 },
                {  0, 15, 15 }
            }));
            Utilities.FilterNodeData(environment, localA);
            var distributedA = new DistributedOverlappingMatrix(environment, indexers, localA);

            double[] globalX = { 0.0, 1.0, 2.0, 3.0, 4.0, 5.0 };
            Dictionary<ComputeNode, Vector> localX = Utilities.GlobalToLocalVectors(globalX, localToGlobalMaps);
            Utilities.FilterNodeData(environment, localX);
            var distributedX = new DistributedOverlappingVector(environment, indexers, localX);

            double[] globalYExpected = { 85.0, 43.0, 93.0, 151.0, 217.0, 556.0 };
            Dictionary<ComputeNode, Vector> localYExpected = Utilities.GlobalToLocalVectors(globalYExpected, localToGlobalMaps);
            Utilities.FilterNodeData(environment, localYExpected);
            var distributedYExpected = new DistributedOverlappingVector(environment, indexers, localYExpected);

            var distributedY = new DistributedOverlappingVector(environment, indexers);
            distributedA.MultiplyIntoResult(distributedX, distributedY);

            double tol = 1E-13;
            Assert.True(distributedYExpected.Equals(distributedY, tol));
        }

        [Theory]
        [MemberData(nameof(GetEnvironments))]
        public static void TestMatrixVectorMultiplicationWithSubdomains(IComputeEnvironment environment)
        {
            var example = new Hexagon1DTopology();
            ComputeNodeTopology topology = example.CreateNodeTopology();
            environment.NodeTopology = topology;
            Dictionary<ComputeNode, DistributedIndexer> indexers = example.CreateIndexers(environment, topology);
            var localToGlobalMaps = example.CreateLocalToGlobalMaps(environment, topology);

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
            Utilities.FilterNodeData(environment, localA);
            var distributedA = new DistributedOverlappingMatrix(environment, indexers, localA);

            double[] globalX = { 0.0, 1.0, 2.0, 3.0, 4.0, 5.0 };
            Dictionary<ComputeNode, Vector> localX = Utilities.GlobalToLocalVectors(globalX, localToGlobalMaps);
            Utilities.FilterNodeData(environment, localX);
            var distributedX = new DistributedOverlappingVector(environment, indexers, localX);

            double[] globalYExpected = { 85.0, 43.0, 93.0, 151.0, 217.0, 201.0 };
            Dictionary<ComputeNode, Vector> localYExpected = Utilities.GlobalToLocalVectors(globalYExpected, localToGlobalMaps);
            Utilities.FilterNodeData(environment, localYExpected);
            var distributedYExpected = new DistributedOverlappingVector(environment, indexers, localYExpected);

            var distributedY = new DistributedOverlappingVector(environment, indexers);
            distributedA.MultiplyIntoResult(distributedX, distributedY);

            double tol = 1E-13;
            Assert.True(distributedYExpected.Equals(distributedY, tol));
        }

        [Theory]
        [MemberData(nameof(GetEnvironments))]
        public static void TestPcg(IComputeEnvironment environment)
        {
            var example = new Hexagon1DTopology();
            ComputeNodeTopology topology = example.CreateNodeTopology();
            environment.NodeTopology = topology;
            Dictionary<ComputeNode, DistributedIndexer> indexers = example.CreateIndexers(environment, topology);
            var localToGlobalMaps = example.CreateLocalToGlobalMaps(environment, topology);

            double[,] globalA =
            {
                { 25, 10,  0,  0,  0, 15 },
                { 10, 21, 11,  0,  0,  0 },
                {  0, 11, 23, 12,  0,  0 },
                {  0,  0, 12, 25, 13,  0 },
                {  0,  0,  0, 13, 27, 14 },
                { 15,  0,  0,  0, 14, 100 }
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
                { 14, 100, 15 },
                {  0, 15, 15 }
            }));
            Utilities.FilterNodeData(environment, localA);
            var distributedA = new DistributedOverlappingMatrix(environment, indexers, localA);

            double[] globalY = { 85.0, 43.0, 93.0, 151.0, 217.0, 556.0 };
            Dictionary<ComputeNode, Vector> localY = Utilities.GlobalToLocalVectors(globalY, localToGlobalMaps);
            Utilities.FilterNodeData(environment, localY);
            var distributedY = new DistributedOverlappingVector(environment, indexers, localY);

            double[] globalXExpected = { 0.0, 1.0, 2.0, 3.0, 4.0, 5.0 };
            Dictionary<ComputeNode, Vector> localXExpected = Utilities.GlobalToLocalVectors(globalXExpected, localToGlobalMaps);
            Utilities.FilterNodeData(environment, localXExpected);
            var distributedXExpected = new DistributedOverlappingVector(environment, indexers, localXExpected);

            var pcgBuilder = new PcgAlgorithm.Builder();
            int maxIterations = 2 * globalA.GetLength(0);
            pcgBuilder.MaxIterationsProvider = new FixedMaxIterationsProvider(maxIterations);
            pcgBuilder.MaxIterationsProvider = new FixedMaxIterationsProvider(globalA.GetLength(0));
            pcgBuilder.ResidualTolerance = 1E-10;
            PcgAlgorithm pcg = pcgBuilder.Build();
            var distributedX = new DistributedOverlappingVector(environment, indexers);
            IterativeStatistics stats = pcg.Solve(distributedA, new IdentityPreconditioner(), distributedY, distributedX, true);

            double tol = 1E-10;
            Assert.True(stats.HasConverged);
            Assert.True(distributedXExpected.Equals(distributedX, tol));
        }

        [Theory]
        [MemberData(nameof(GetEnvironments))]
        public static void TestScaleVector(IComputeEnvironment environment)
        {
            var example = new Hexagon1DTopology();
            ComputeNodeTopology topology = example.CreateNodeTopology();
            environment.NodeTopology = topology;
            Dictionary<ComputeNode, DistributedIndexer> indexers = example.CreateIndexers(environment, topology);
            var localToGlobalMaps = example.CreateLocalToGlobalMaps(environment, topology);

            double[] globalX = { 10.0, 11.0, 12.0, 13.0, 14.0, 15.0 };
            Dictionary<ComputeNode, Vector> localX = Utilities.GlobalToLocalVectors(globalX, localToGlobalMaps);
            Utilities.FilterNodeData(environment, localX);
            var distributedX = new DistributedOverlappingVector(environment, indexers, localX);

            double[] globalZExpected = { -30.0, -33.0, -36.0, -39.0, -42.0, -45.0 };
            Dictionary<ComputeNode, Vector> localZExpected = Utilities.GlobalToLocalVectors(globalZExpected, localToGlobalMaps);
            Utilities.FilterNodeData(environment, localZExpected);
            var distributedZExpected = new DistributedOverlappingVector(environment, indexers, localZExpected);

            DistributedOverlappingVector distributedZ = distributedX.Copy();
            distributedZ.ScaleIntoThis(-3.0);

            double tol = 1E-13;
            Assert.True(distributedZExpected.Equals(distributedZ, tol));
        }

        [Theory]
        [MemberData(nameof(GetEnvironments))]
        public static void TestSumOverlappingEntries(IComputeEnvironment environment)
        {
            var example = new Hexagon1DTopology();
            ComputeNodeTopology topology = example.CreateNodeTopology();
            environment.NodeTopology = topology;
            Dictionary<ComputeNode, DistributedIndexer> indexers = example.CreateIndexers(environment, topology);
            var localToGlobalMaps = example.CreateLocalToGlobalMaps(environment, topology);

            double[] globalExpected = { 28.0, 11.0, 25.0, 14.0, 31.0, 17.0 };
            Dictionary<ComputeNode, Vector> localExpected = Utilities.GlobalToLocalVectors(globalExpected, localToGlobalMaps);
            Utilities.FilterNodeData(environment, localExpected);
            var distributedExpected = new DistributedOverlappingVector(environment, indexers, localExpected);

            var localRhs = new Dictionary<ComputeNode, Vector>();
            localRhs[environment.NodeTopology.Nodes[0]] = Vector.CreateFromArray(new double[] { 10.0, 11.0, 12.0 });
            localRhs[environment.NodeTopology.Nodes[1]] = Vector.CreateFromArray(new double[] { 13.0, 14.0, 15.0 });
            localRhs[environment.NodeTopology.Nodes[2]] = Vector.CreateFromArray(new double[] { 16.0, 17.0, 18.0 });
            Utilities.FilterNodeData(environment, localRhs);
            var distributedComputed = new DistributedOverlappingVector(environment, indexers, localRhs);
            distributedComputed.SumOverlappingEntries();

            double tol = 1E-13;
            Assert.True(distributedExpected.Equals(distributedComputed, tol));
        }

        internal static void RunMpiTests()
        {
            int numProcesses = 3;
            using (var mpiEnvironment = new MpiEnvironment(numProcesses))
            {
                MpiUtilities.AssistDebuggerAttachment();
                TestAxpyVectors(mpiEnvironment);
                TestDotProduct(mpiEnvironment);
                TestEqualVectors(mpiEnvironment);
                TestLinearCombinationVectors(mpiEnvironment);
                TestMatrixVectorMultiplication(mpiEnvironment);
                TestMatrixVectorMultiplicationWithSubdomains(mpiEnvironment);
                TestPcg(mpiEnvironment);
                TestScaleVector(mpiEnvironment);
                TestSumOverlappingEntries(mpiEnvironment);

                MpiUtilities.DoSerially(MPI.Communicator.world,
                    () => Console.WriteLine($"Process {MPI.Communicator.world.Rank}: All tests passed"));
            }
        }
    }
}
