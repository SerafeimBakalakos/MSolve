using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.LinearAlgebra;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using MGroup.Solvers.Distributed.Environments;
using MGroup.Solvers.Distributed.LinearAlgebra;
using MGroup.Solvers.Distributed.Topologies;
using Xunit;

//TODOMPI: merge it with the test class for other environments
namespace MGroup.Solvers.Tests.Distributed.LinearAlgebra
{
    public static class DistributedOverlappingVectorTestsMpi
    {
        public static void TestAxpy()
        {
            var example = new Hexagon1DTopology();
            ComputeNodeTopology topology = example.CreateNodeTopology();

            using (var environment = new MpiEnvironment(3))
            {
                environment.NodeTopology = topology;
                Dictionary<ComputeNode, DistributedIndexer> indexers = example.CreateIndexers(topology);
                var localToGlobalMaps = example.CreateLocalToGlobalMaps(topology);
                Utilities.FilterDictionary(localToGlobalMaps, environment.SingleNode);

                double[] globalX = { 0.0, 1.0, 2.0, 3.0, 4.0, 5.0 };
                Dictionary<ComputeNode, Vector> localX = Utilities.GlobalToLocalVectors(globalX, localToGlobalMaps);
                Utilities.FilterDictionary(localX, environment.SingleNode);
                var distributedX = new DistributedOverlappingVector(environment, indexers, localX);

                double[] globalY = { 10.0, 11.0, 12.0, 13.0, 14.0, 15.0 };
                Dictionary<ComputeNode, Vector> localY = Utilities.GlobalToLocalVectors(globalY, localToGlobalMaps);
                Utilities.FilterDictionary(localY, environment.SingleNode);
                var distributedY = new DistributedOverlappingVector(environment, indexers, localY);

                double[] globalZExpected = { 20.0, 23.0, 26.0, 29.0, 32.0, 35.0 };
                Dictionary<ComputeNode, Vector> localZExpected = Utilities.GlobalToLocalVectors(globalZExpected, localToGlobalMaps);
                Utilities.FilterDictionary(localZExpected, environment.SingleNode);
                var distributedZExpected = new DistributedOverlappingVector(environment, indexers, localZExpected);

                DistributedOverlappingVector distributedZ = distributedX.Copy();
                distributedZ.AxpyIntoThis(distributedY, 2.0);

                //var stringBuilder = new StringBuilder($"Compute node {environment.SingleNode.ID}:");
                //stringBuilder.AppendLine();
                //Vector temp = distributedZExpected.LocalVectors[environment.SingleNode];
                //stringBuilder.AppendLine($"Expected result = [ {temp[0]}, {temp[1]}, {temp[2]} ]");
                //temp = distributedZ.LocalVectors[environment.SingleNode];
                //stringBuilder.AppendLine($"Computed result = [ {temp[0]}, {temp[1]}, {temp[2]} ]");
                //stringBuilder.AppendLine();
                //Console.Write(stringBuilder.ToString());

                double tol = 1E-13;
                Assert.True(distributedZExpected.Equals(distributedZ, tol));
            }
        }

        public static void TestDotProduct()
        {
            var example = new Hexagon1DTopology();
            ComputeNodeTopology topology = example.CreateNodeTopology();

            using (var environment = new MpiEnvironment(3))
            {
                MpiUtilities.AssistDebuggerAttachment();
                environment.NodeTopology = topology;
                Dictionary<ComputeNode, DistributedIndexer> indexers = example.CreateIndexers(topology);
                var localToGlobalMaps = example.CreateLocalToGlobalMaps(topology);
                Utilities.FilterDictionary(localToGlobalMaps, environment.SingleNode);

                double[] globalX = { 0.0, 1.0, 2.0, 3.0, 4.0, 5.0 };
                Dictionary<ComputeNode, Vector> localX = Utilities.GlobalToLocalVectors(globalX, localToGlobalMaps);
                Utilities.FilterDictionary(localX, environment.SingleNode);
                var distributedX = new DistributedOverlappingVector(environment, indexers, localX);

                double[] globalY = { 10.0, 11.0, 12.0, 13.0, 14.0, 15.0 };
                Dictionary<ComputeNode, Vector> localY = Utilities.GlobalToLocalVectors(globalY, localToGlobalMaps);
                Utilities.FilterDictionary(localY, environment.SingleNode);
                var distributedY = new DistributedOverlappingVector(environment, indexers, localY);

                double dotExpected = globalX.DotProduct(globalY);
                double dot = distributedX.DotProduct(distributedY);

                int precision = 8;
                Assert.Equal(dotExpected, dot, precision);
            }
        }

        public static void TestEquals()
        {
            var example = new Hexagon1DTopology();
            ComputeNodeTopology topology = example.CreateNodeTopology();

            using (var environment = new MpiEnvironment(3))
            {
                MpiUtilities.AssistDebuggerAttachment();
                environment.NodeTopology = topology;
                Dictionary<ComputeNode, DistributedIndexer> indexers = example.CreateIndexers(topology);
                var localToGlobalMaps = example.CreateLocalToGlobalMaps(topology);
                Utilities.FilterDictionary(localToGlobalMaps, environment.SingleNode);

                var localVectors1 = new Dictionary<ComputeNode, Vector>();
                localVectors1[environment.NodeTopology.Nodes[0]] = Vector.CreateFromArray(new double[] { 0.0, 1.0, 2.0 });
                localVectors1[environment.NodeTopology.Nodes[1]] = Vector.CreateFromArray(new double[] { 2.0, 3.0, 4.0 });
                localVectors1[environment.NodeTopology.Nodes[2]] = Vector.CreateFromArray(new double[] { 4.0, 5.0, 0.0 });
                Utilities.FilterDictionary(localVectors1, environment.SingleNode);
                var distributedVector1 = new DistributedOverlappingVector(environment, indexers, localVectors1);

                double[] globalVector = { 0.0, 1.0, 2.0, 3.0, 4.0, 5.0 };
                Dictionary<ComputeNode, Vector> localVectors2 = Utilities.GlobalToLocalVectors(globalVector, localToGlobalMaps);
                Utilities.FilterDictionary(localVectors2, environment.SingleNode);
                var distributedVector2 = new DistributedOverlappingVector(environment, indexers, localVectors2);

                double tol = 1E-13;
                Assert.True(distributedVector1.Equals(distributedVector2, tol));
            }
        }
        
        //[Fact]
        //public static void TestLinearCombination()
        //{
        //    var example = new Hexagon1DTopology();
        //    ComputeNodeTopology topology = example.CreateNodeTopology();
        //    var environment = new DistributedLocalEnvironment();
        //    environment.NodeTopology = topology;
        //    Dictionary<ComputeNode, DistributedIndexer> indexers = example.CreateIndexers(topology);
        //    var localToGlobalMaps = example.CreateLocalToGlobalMaps(topology);

        //    double[] globalX = { 0.0, 1.0, 2.0, 3.0, 4.0, 5.0 };
        //    Dictionary<ComputeNode, Vector> localX = Utilities.GlobalToLocalVectors(globalX, localToGlobalMaps);
        //    var distributedX = new DistributedOverlappingVector(environment, indexers, localX);

        //    double[] globalY = { 10.0, 11.0, 12.0, 13.0, 14.0, 15.0 };
        //    Dictionary<ComputeNode, Vector> localY = Utilities.GlobalToLocalVectors(globalY, localToGlobalMaps);
        //    var distributedY = new DistributedOverlappingVector(environment, indexers, localY);

        //    double[] globalZExpected = { 30.0, 35.0, 40.0, 45.0, 50.0, 55.0 };
        //    Dictionary<ComputeNode, Vector> localZExpected = Utilities.GlobalToLocalVectors(globalZExpected, localToGlobalMaps);
        //    var distributedZExpected = new DistributedOverlappingVector(environment, indexers, localZExpected);

        //    DistributedOverlappingVector distributedZ = distributedX.Copy();
        //    distributedZ.LinearCombinationIntoThis(2.0, distributedY, 3.0);

        //    double tol = 1E-13;
        //    Assert.True(distributedZExpected.Equals(distributedZ, tol));
        //}
        
        public static void TestRhsVectorConvertion()
        {
            var example = new Hexagon1DTopology();
            ComputeNodeTopology topology = example.CreateNodeTopology();

            using (var environment = new MpiEnvironment(3))
            {
                MpiUtilities.AssistDebuggerAttachment();
                environment.NodeTopology = topology;
                Dictionary<ComputeNode, DistributedIndexer> indexers = example.CreateIndexers(topology);
                var localToGlobalMaps = example.CreateLocalToGlobalMaps(topology);
                Utilities.FilterDictionary(localToGlobalMaps, environment.SingleNode);

                double[] globalExpected = { 28.0, 11.0, 25.0, 14.0, 31.0, 17.0 };
                Dictionary<ComputeNode, Vector> localExpected = Utilities.GlobalToLocalVectors(globalExpected, localToGlobalMaps);
                Utilities.FilterDictionary(localExpected, environment.SingleNode);
                var distributedExpected = new DistributedOverlappingVector(environment, indexers, localExpected);

                var localRhs = new Dictionary<ComputeNode, Vector>();
                localRhs[environment.NodeTopology.Nodes[0]] = Vector.CreateFromArray(new double[] { 10.0, 11.0, 12.0 });
                localRhs[environment.NodeTopology.Nodes[1]] = Vector.CreateFromArray(new double[] { 13.0, 14.0, 15.0 });
                localRhs[environment.NodeTopology.Nodes[2]] = Vector.CreateFromArray(new double[] { 16.0, 17.0, 18.0 });
                Utilities.FilterDictionary(localRhs, environment.SingleNode);
                var distributedComputed = new DistributedOverlappingVector(environment, indexers, localRhs);
                distributedComputed.ConvertRhsToLhsVector();

                var stringBuilder = new StringBuilder($"Compute node {environment.SingleNode.ID}:");
                stringBuilder.AppendLine();
                Vector temp = distributedExpected.LocalVectors[environment.SingleNode];
                stringBuilder.AppendLine($"Expected result = [ {temp[0]}, {temp[1]}, {temp[2]} ]");
                temp = distributedComputed.LocalVectors[environment.SingleNode];
                stringBuilder.AppendLine($"Computed result = [ {temp[0]}, {temp[1]}, {temp[2]} ]");
                stringBuilder.AppendLine();
                Console.Write(stringBuilder.ToString());

                double tol = 1E-13;
                //Assert.True(distributedExpected.Equals(distributedComputed, tol));
            }
        }

        //[Fact]
        //public static void TestScale()
        //{
        //    var example = new Hexagon1DTopology();
        //    ComputeNodeTopology topology = example.CreateNodeTopology();
        //    var environment = new DistributedLocalEnvironment();
        //    environment.NodeTopology = topology;
        //    Dictionary<ComputeNode, DistributedIndexer> indexers = example.CreateIndexers(topology);
        //    var localToGlobalMaps = example.CreateLocalToGlobalMaps(topology);

        //    double[] globalX = { 10.0, 11.0, 12.0, 13.0, 14.0, 15.0 };
        //    Dictionary<ComputeNode, Vector> localX = Utilities.GlobalToLocalVectors(globalX, localToGlobalMaps);
        //    var distributedX = new DistributedOverlappingVector(environment, indexers, localX);

        //    double[] globalZExpected = { -30.0, -33.0, -36.0, -39.0, -42.0, -45.0 };
        //    Dictionary<ComputeNode, Vector> localZExpected = Utilities.GlobalToLocalVectors(globalZExpected, localToGlobalMaps);
        //    var distributedZExpected = new DistributedOverlappingVector(environment, indexers, localZExpected);

        //    DistributedOverlappingVector distributedZ = distributedX.Copy();
        //    distributedZ.ScaleIntoThis(-3.0);

        //    double tol = 1E-13;
        //    Assert.True(distributedZExpected.Equals(distributedZ, tol));
        //}
    }
}
