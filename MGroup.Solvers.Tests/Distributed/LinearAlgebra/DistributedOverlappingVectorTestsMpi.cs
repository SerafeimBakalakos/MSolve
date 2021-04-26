using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.LinearAlgebra;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using MGroup.Solvers.Distributed.Environments;
using MGroup.Solvers.Distributed.LinearAlgebra;
using MGroup.Solvers.Distributed.Topologies;
using Xunit;

//HERE: merge it with the test class for other environments. The client code must almost identical and use  utility methods to 
//      prepare MPI stuff. These utilities must be as lightweight as possible. This will be a good indication about the ease of
//      using this design in DDMs.
namespace MGroup.Solvers.Tests.Distributed.LinearAlgebra
{
    public static class DistributedOverlappingVectorTestsMpi
    {
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
