using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.Discretization.Providers;
using ISAAR.MSolve.FEM.Entities;
using MGroup.Environments;
using MGroup.LinearAlgebra.Distributed.Overlapping;
using MGroup.Solvers.DomainDecomposition.PSM.Dofs;
using MGroup.Solvers.DomainDecomposition.Tests.ExampleModels;
using Xunit;

namespace MGroup.Solvers.DomainDecomposition.Tests.PSM
{
    public static class Line1DPsmSolverTest
    {
        [Fact]
        public static void TestDofSeparator()
        {
            IComputeEnvironment environment = new SequentialSharedEnvironment();
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

        //[Fact]
        public static void TestSolver()
        {
            //IComputeEnvironment environment = new SequentialSharedEnvironment(4);
            //var ddmEnvironment = new ProcessingEnvironment(
            //    new SubdomainEnvironmentManagedSequential(), new ClusterEnvironmentManagedSequential());
            //(Model model, ClusterTopology clusterTopology) = Line1DExample.CreateMultiSubdomainModel(environment);

            //var solverBuilder = new PsmSolver.Builder(environment);
            //solverBuilder.DdmEnvironment = ddmEnvironment;
            //PsmSolver solver = solverBuilder.BuildSolver(model, clusterTopology);

            //InitializeEnvironment(environment, clusterTopology);
            //model.ConnectDataStructures();
            //solver.InitializeClusterTopology();
            //solver.OrderDofs(false);

            ////TODOMPI: In order to number dofs, the Kff of each submatrix must be created, so that Kii can be extracted and 
            //// internal dofs can be reordered. I do not like this design after all. It would be better to reorder the internal 
            //// dofs at a later stage, when stiffness matrices are created.
            //solver.BuildGlobalMatrices(new ElementStructuralStiffnessProvider());

            //solver.Initialize();
            //solver.Solve();
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
