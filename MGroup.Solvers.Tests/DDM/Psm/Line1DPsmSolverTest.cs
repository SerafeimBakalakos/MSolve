using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.Discretization.Providers;
using ISAAR.MSolve.FEM.Entities;
using MGroup.Solvers_OLD.DDM.Environments;
using MGroup.Solvers_OLD.DistributedTry1.DDM;
using MGroup.Solvers_OLD.DistributedTry1.DDM.Psm;
using MGroup.Solvers_OLD.DistributedTry1.Distributed.Environments;
using MGroup.Solvers_OLD.DistributedTry1.Distributed.LinearAlgebra;
using MGroup.Solvers_OLD.DistributedTry1.Distributed.Topologies;
using MGroup.Solvers_OLD.Tests.DDM.ExampleModels;
using Xunit;

namespace MGroup.Solvers_OLD.Tests.DDM.Psm
{
    public static class Line1DPsmSolverTest
    {
        [Fact]
        public static void TestDofSeparator()
        {
            IComputeEnvironment environment = new SequentialSharedEnvironment(4);
            var ddmEnvironment = new ProcessingEnvironment(
                new SubdomainEnvironmentManagedSequential(), new ClusterEnvironmentManagedSequential());
            (Model model, ClusterTopology clusterTopology) = Line1DExample.CreateMultiSubdomainModel(environment);

            var solverBuilder = new PsmSolver_NEW.Builder(environment);
            solverBuilder.DdmEnvironment = ddmEnvironment;
            PsmSolver_NEW solver = solverBuilder.BuildSolver(model, clusterTopology);

            InitializeEnvironment(environment, clusterTopology);
            model.ConnectDataStructures();
            solver.InitializeClusterTopology();
            solver.OrderDofs(false);

            //TODOMPI: In order to number dofs, the Kff of each submatrix must be created, so that Kii can be extracted and 
            // internal dofs can be reordered. I do not like this design after all. It would be better to reorder the internal 
            // dofs at a later stage, when stiffness matrices are created.
            solver.BuildGlobalMatrices(new ElementStructuralStiffnessProvider());

            solver.Initialize();

            // Check
            var indexer = (DistributedIndexer)(
                typeof(PsmSolver_NEW).GetField("indexer", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(solver));
            CheckIndexer(environment, indexer);
        }

        [Fact]
        public static void TestSolver()
        {
            IComputeEnvironment environment = new SequentialSharedEnvironment(4);
            var ddmEnvironment = new ProcessingEnvironment(
                new SubdomainEnvironmentManagedSequential(), new ClusterEnvironmentManagedSequential());
            (Model model, ClusterTopology clusterTopology) = Line1DExample.CreateMultiSubdomainModel(environment);

            var solverBuilder = new PsmSolver_NEW.Builder(environment);
            solverBuilder.DdmEnvironment = ddmEnvironment;
            PsmSolver_NEW solver = solverBuilder.BuildSolver(model, clusterTopology);

            InitializeEnvironment(environment, clusterTopology);
            model.ConnectDataStructures();
            solver.InitializeClusterTopology();
            solver.OrderDofs(false);

            //TODOMPI: In order to number dofs, the Kff of each submatrix must be created, so that Kii can be extracted and 
            // internal dofs can be reordered. I do not like this design after all. It would be better to reorder the internal 
            // dofs at a later stage, when stiffness matrices are created.
            solver.BuildGlobalMatrices(new ElementStructuralStiffnessProvider());

            solver.Initialize();
            solver.Solve();
        }

        //TODOMPI: It would be better if I could have a mock indexer object which knows how to compare itself with the actual one.
        private static void CheckIndexer(IComputeEnvironment environment, DistributedIndexer indexer)
        {
            Action<ComputeNode> checkIndexer = node =>
            {
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

                Utilities.AssertEqual(multiplicitiesExpected, indexer.GetEntryMultiplicities(node));
                foreach (ComputeNode neighbor in commonEntriesExpected.Keys)
                {
                    int[] expected = commonEntriesExpected[neighbor];
                    int[] computed = indexer.GetCommonEntriesOfNodeWithNeighbor(node, neighbor);
                    Utilities.AssertEqual(expected, computed);
                }
            };
            environment.DoPerNode(checkIndexer);
        }

        //TODOMPI: This initial setup, must be done at the beginning of the program, once and not change. Perhaps in the constructor of the environment
        private static void InitializeEnvironment(IComputeEnvironment environment, ClusterTopology clusterTopology)
        {
            var computeNodeTopology = new ComputeNodeTopology();
            foreach (Solvers_OLD.DDM.Cluster cluster in clusterTopology.Clusters.Values)
            {
                var computeNode = new ComputeNode(cluster.ID); ;
                computeNodeTopology.Nodes[cluster.ID] = computeNode;

                foreach (ISubdomain subdomain in cluster.Subdomains)
                {
                    computeNode.Subnodes[subdomain.ID] = new ComputeSubnode(subdomain.ID) { ParentNode = computeNode };
                }

                //TODOMPI: ComputeNode.Neighbors should be a list containing the IDs of neighboring ComputeNodes, not the actual objects.
                //	Then I can finish setting the neighbors in this loop. Actually no: neighbors cannot be determined before 
                //  initialization of the environment completes. Also neighbors depend on Model data, while this method should be 
                //  as barebones and independent as possible
            }
            environment.NodeTopology = computeNodeTopology;
        }
    }
}
