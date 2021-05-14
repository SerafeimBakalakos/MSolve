using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using ISAAR.MSolve.Discretization.Providers;
using ISAAR.MSolve.FEM.Entities;
using MGroup.Solvers.DDM;
using MGroup.Solvers.DDM.Environments;
using MGroup.Solvers.DDM.Psm;
using MGroup.Solvers.DDM.Psm.Dofs;
using MGroup.Solvers.Distributed.Environments;
using MGroup.Solvers.Distributed.LinearAlgebra;
using MGroup.Solvers.Distributed.Topologies;
using MGroup.Solvers.Tests.DDM.ExampleModels;
using Xunit;

namespace MGroup.Solvers.Tests.DDM.Psm
{
    public static class Line1DPsmSolverTest
    {
        [Fact]
        public static void Run()
        {
            IComputeEnvironment environment = new DistributedLocalEnvironment(4);
            var ddmEnvironment = new ProcessingEnvironment(
                new SubdomainEnvironmentManagedSequential(), new ClusterEnvironmentManagedSequential());
            ddmEnvironment.ComputeEnvironment = environment;
            (Model model, ClusterTopology clusterTopology) = Line1DExample.CreateMultiSubdomainModel(ddmEnvironment);

            var solverBuilder = new PsmSolver_NEW.Builder();
            solverBuilder.ComputingEnvironment = ddmEnvironment;
            PsmSolver_NEW solver = solverBuilder.BuildSolver(model, clusterTopology);

            InitializeEnvironment(ddmEnvironment, clusterTopology);
            model.ConnectDataStructures();
            solver.InitializeClusterTopology();
            solver.OrderDofs(false);

            //TODOMPI: In order to number dofs, the Kff of each submatrix must be created, so that Kii can be extracted and 
            // internal dofs can be reordered. I do not like this design after all. It would be better to reorder the internal 
            // dofs at a later stage, when stiffness matrices are created.
            solver.BuildGlobalMatrices(new ElementStructuralStiffnessProvider());

            solver.Initialize();

            // Check
            var indexers = (Dictionary<ComputeNode, DistributedIndexer>)(
                typeof(PsmSolver_NEW).GetField("indexers", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(solver));
            CheckIndexer(environment, indexers);
        }

        //TODOMPI: It would be better if I could have a mock indexer object which knows how to compare itself with the actual one.
        private static void CheckIndexer(IComputeEnvironment environment, Dictionary<ComputeNode, DistributedIndexer> indexers)
        {
            foreach (ComputeNode node in indexers.Keys)
            {
                DistributedIndexer indexer = indexers[node];

                int[] multiplicitiesExpected;
                var commonEntriesExpected = new Dictionary<ComputeNode, int[]>();
                if (node.ID == 0)
                {
                    multiplicitiesExpected = new int[] { 1, 2 };
                    commonEntriesExpected[environment.NodeTopology.Nodes[1]] = new int[] { 1 };
                }
                else if (node.ID == 1)
                {
                    multiplicitiesExpected = new int[] { 2, 1, 2 };
                    commonEntriesExpected[environment.NodeTopology.Nodes[0]] = new int[] { 0 };
                    commonEntriesExpected[environment.NodeTopology.Nodes[2]] = new int[] { 2 };
                }
                else if (node.ID == 2)
                {
                    multiplicitiesExpected = new int[] { 2, 1, 2 };
                    commonEntriesExpected[environment.NodeTopology.Nodes[1]] = new int[] { 0 };
                    commonEntriesExpected[environment.NodeTopology.Nodes[3]] = new int[] { 2 };
                }
                else
                {
                    Debug.Assert(node.ID == 3);
                    multiplicitiesExpected = new int[] { 2, 1 };
                    commonEntriesExpected[environment.NodeTopology.Nodes[2]] = new int[] { 0 };
                }

                Utilities.AssertEqual(multiplicitiesExpected, indexer.Multiplicities);
                foreach (ComputeNode neighbor in commonEntriesExpected.Keys)
                {
                    int[] expected = commonEntriesExpected[neighbor];
                    int[] computed = indexer.GetCommonEntriesWithNeighbor(neighbor);
                    Utilities.AssertEqual(expected, computed);
                }
            }
        }

        //TODOMPI: This initial setup, must be done at the beginning of the program, once and not change. Perhaps in the constructor of the environment
        private static void InitializeEnvironment(IDdmEnvironment environment, ClusterTopology clusterTopology)
        {
            var computeNodeTopology = new ComputeNodeTopology();
            foreach (Solvers.DDM.Cluster cluster in clusterTopology.Clusters.Values)
            {
                computeNodeTopology.Nodes[cluster.ID] = new ComputeNode(cluster.ID);
                //TODOMPI: ComputeNode.Neighbors should be a list containing the IDs of neighboring ComputeNodes, not the actual objects.
                //	Then I can finish setting the neighbors in this loop. Actually no: neighbors cannot be determined before 
                //  initialization of the environment completes. Also neighbors depend on Model data, while this method should be 
                //  as barebones and independent as possible
            }
            environment.ComputeEnvironment.NodeTopology = computeNodeTopology;
        }
    }
}
