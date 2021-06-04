using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using ISAAR.MSolve.FEM.Entities;
using MGroup.Environments;
using MGroup.Environments.Mpi;
using MGroup.LinearAlgebra.Distributed.Overlapping;
using MGroup.Solvers.DomainDecomposition.PSM.Dofs;
using MGroup.Solvers.DomainDecomposition.Tests.ExampleModels;
using Xunit;

namespace MGroup.Solvers.DomainDecomposition.Tests.PSM
{
    public class PsmDofSeparatorTests
    {
        public static void RunMpiTests()
        {
            // Launch 4 processes
            using (var mpiEnvironment = new MpiEnvironment())
            {
                MpiDebugUtilities.AssistDebuggerAttachment();

                TestDofSeparator(mpiEnvironment);

                MpiDebugUtilities.DoSerially(MPI.Communicator.world,
                    () => Console.WriteLine($"Process {MPI.Communicator.world.Rank}: All tests passed"));
            }
        }

        [Theory]
        [InlineData(EnvironmentChoice.SequentialSharedEnvironment)]
        [InlineData(EnvironmentChoice.TplSharedEnvironment)]
        public static void TestDofSeparatorManaged(EnvironmentChoice environmentChoice) 
            => TestDofSeparator(Utilities.CreateEnvironment(environmentChoice));

        internal static void TestDofSeparator(IComputeEnvironment environment)
        {
            ComputeNodeTopology nodeTopology = Line1DExample.CreateNodeTopology(environment);
            environment.Initialize(nodeTopology);

            Model model = Line1DExample.CreateMultiSubdomainModel();
            model.ConnectDataStructures();
            var subdomainTopology = new SubdomainTopology(environment, model);
            ModelUtilities.OrderDofs(model);

            var dofSeparator = new PsmDofSeparator(environment, model, subdomainTopology);
            dofSeparator.SeparateSubdomainDofsIntoBoundaryInternal();
            dofSeparator.FindCommonDofsBetweenSubdomains();
            DistributedOverlappingIndexer indexer = dofSeparator.CreateDistributedVectorIndexer();

            // Check
            CheckIndexer(environment, nodeTopology, indexer);
        }

        //TODOMPI: It would be better if I could have a mock indexer object which knows how to compare itself with the actual one.
        private static void CheckIndexer(IComputeEnvironment environment, ComputeNodeTopology nodeTopology,
            DistributedOverlappingIndexer indexer)
        {
            Action<int> checkIndexer = nodeID =>
            {
                int[] multiplicitiesExpected; // Remember that only boundary dofs go into the distributed vectors 
                var commonEntriesExpected = new Dictionary<int, int[]>();
                if (nodeID == 0)
                {
                    multiplicitiesExpected = new int[] { 2 };
                    commonEntriesExpected[1] = new int[] { 0 };
                }
                else if (nodeID == 1)
                {
                    multiplicitiesExpected = new int[] { 2, 2 };
                    commonEntriesExpected[0] = new int[] { 0 };
                    commonEntriesExpected[2] = new int[] { 1 };
                }
                else if (nodeID == 2)
                {
                    multiplicitiesExpected = new int[] { 2, 2 };
                    commonEntriesExpected[1] = new int[] { 0 };
                    commonEntriesExpected[3] = new int[] { 1 };
                }
                else if (nodeID == 3)
                {
                    multiplicitiesExpected = new int[] { 2, 2 };
                    commonEntriesExpected[2] = new int[] { 0 };
                    commonEntriesExpected[4] = new int[] { 1 };
                }
                else if (nodeID == 4)
                {
                    multiplicitiesExpected = new int[] { 2, 2 };
                    commonEntriesExpected[3] = new int[] { 0 };
                    commonEntriesExpected[5] = new int[] { 1 };
                }
                else if (nodeID == 5)
                {
                    multiplicitiesExpected = new int[] { 2, 2 };
                    commonEntriesExpected[4] = new int[] { 0 };
                    commonEntriesExpected[6] = new int[] { 1 };
                }
                else if (nodeID == 6)
                {
                    multiplicitiesExpected = new int[] { 2, 2 };
                    commonEntriesExpected[5] = new int[] { 0 };
                    commonEntriesExpected[7] = new int[] { 1 };
                }
                else
                {
                    Debug.Assert(nodeID == 7);
                    multiplicitiesExpected = new int[] { 2 };
                    commonEntriesExpected[6] = new int[] { 0 };
                }

                int[] multiplicitiesComputed = indexer.GetLocalComponent(nodeID).Multiplicities;
                Assert.True(Utilities.AreEqual(multiplicitiesExpected, multiplicitiesComputed));
                foreach (int neighborID in commonEntriesExpected.Keys)
                {
                    int[] expected = commonEntriesExpected[neighborID];
                    int[] computed = indexer.GetLocalComponent(nodeID).GetCommonEntriesWithNeighbor(neighborID);
                    Assert.True(Utilities.AreEqual(expected, computed));
                }
            };
            environment.DoPerNode(checkIndexer);
        }
    }
}
