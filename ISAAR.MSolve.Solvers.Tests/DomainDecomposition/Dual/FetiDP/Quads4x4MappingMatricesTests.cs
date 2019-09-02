using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.Discretization.Transfer;
using ISAAR.MSolve.FEM.Entities;
using ISAAR.MSolve.FEM.Transfer;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Matrices.Operators;
using ISAAR.MSolve.LinearAlgebra.Output;
using ISAAR.MSolve.Solvers.Direct;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.CornerNodes;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.DofSeparation;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.LagrangeMultipliers;
using ISAAR.MSolve.Solvers.Ordering;
using ISAAR.MSolve.Solvers.Ordering.Reordering;
using MPI;
using Xunit;

//TODO: Remove redundancies between the methods that define corner nodes
//TODO: Have all expected stuff in a separate class that works for homegeneous and heterogeneous. The tests belong to individual 
//      classes.
namespace ISAAR.MSolve.Solvers.Tests.DomainDecomposition.Dual.FetiDP
{
    public static class Quads4x4MappingMatricesTests
    {
        [Fact]
        public static void TestDofSeparation()
        {
            // Create model
            Model model = Example4x4Quads.CreateHomogeneousModel();
            model.ConnectDataStructures();

            // Order free dofs.
            var dofOrderer = new DofOrderer(new NodeMajorDofOrderingStrategy(), new NullReordering());
            dofOrderer.OrderFreeDofs(model);

            // Separate dofs
            var dofSeparator = new FetiDPDofSeparator();
            foreach (ISubdomain subdomain in model.EnumerateSubdomains())
            {
                HashSet<INode> cornerNodes = Example4x4Quads.DefineCornerNodesSubdomain(subdomain);
                dofSeparator.SeparateCornerRemainderDofs(subdomain, cornerNodes);
                dofSeparator.SeparateBoundaryInternalDofs(subdomain, cornerNodes);
            }
            
            // Check
            for (int s = 0; s < 4; ++s)
            {
                (int[] cornerDofs, int[] remainderDofs, int[] boundaryRemainderDofs, int[] internalRemainderDofs) =
                    Example4x4Quads.GetDofSeparation(s);
                Utilities.CheckEqual(cornerDofs, dofSeparator.CornerDofIndices[s]);
                Utilities.CheckEqual(remainderDofs, dofSeparator.RemainderDofIndices[s]);
                Utilities.CheckEqual(boundaryRemainderDofs, dofSeparator.BoundaryDofIndices[s]);
                Utilities.CheckEqual(internalRemainderDofs, dofSeparator.InternalDofIndices[s]);
            }
        }

        [Fact]
        public static void TestSignedBooleanMatrices()
        {
            // Create model
            Model model = Example4x4Quads.CreateHomogeneousModel();
            model.ConnectDataStructures();

            // Order free dofs.
            var dofOrderer = new DofOrderer(new NodeMajorDofOrderingStrategy(), new NullReordering());
            dofOrderer.OrderFreeDofs(model);

            // Separate dofs
            var dofSeparator = new FetiDPDofSeparator();
            dofSeparator.DefineGlobalBoundaryDofs(model, Example4x4Quads.DefineCornerNodesGlobal(model));
            foreach (ISubdomain subdomain in model.EnumerateSubdomains())
            {
                HashSet<INode> cornerNodes = Example4x4Quads.DefineCornerNodesSubdomain(subdomain);
                dofSeparator.SeparateCornerRemainderDofs(subdomain, cornerNodes);
                dofSeparator.SeparateBoundaryInternalDofs(subdomain, cornerNodes);
            }

            // Enumerate lagranges
            var crosspointStrategy = new FullyRedundantConstraints();
            var lagrangeEnumerator = new FetiDPLagrangeMultipliersEnumeratorOLD(crosspointStrategy, dofSeparator);
            lagrangeEnumerator.DefineBooleanMatrices(model);

            // Check
            int expectedNumLagrangeMultipliers = 8;
            Assert.Equal(expectedNumLagrangeMultipliers, lagrangeEnumerator.NumLagrangeMultipliers);
            double tolerance = 1E-13;
            for (int s = 0; s < 4; ++s)
            {
                Matrix Br = lagrangeEnumerator.BooleanMatrices[s].CopyToFullMatrix(false);
                Matrix expectedBr = Example4x4Quads.GetMatrixBr(s);
                Assert.True(expectedBr.Equals(Br, tolerance));
            }
        }

        [Fact]
        public static void TestUnsignedBooleanMatrices()
        {
            // Create model
            Model model = Example4x4Quads.CreateHomogeneousModel();
            model.ConnectDataStructures();

            // Order free dofs.
            var dofOrderer = new DofOrderer(new NodeMajorDofOrderingStrategy(), new NullReordering());
            dofOrderer.OrderFreeDofs(model);

            // Separate dofs
            var dofSeparator = new FetiDPDofSeparator();
            dofSeparator.DefineGlobalCornerDofs(model, Example4x4Quads.DefineCornerNodesGlobal(model));
            foreach (ISubdomain subdomain in model.EnumerateSubdomains())
            {
                HashSet<INode> cornerNodes = Example4x4Quads.DefineCornerNodesSubdomain(subdomain);
                //IEnumerable<INode> remainderAndConstrainedNodes = subdomain.Nodes.Where(node => !cornerNodes[s].Contains(node));
                dofSeparator.SeparateCornerRemainderDofs(subdomain, cornerNodes);
                dofSeparator.SeparateBoundaryInternalDofs(subdomain, cornerNodes);
            }
            dofSeparator.CalcCornerMappingMatrices(model);

            // Check
            int expectedNumCornerDofs = 8;
            Assert.Equal(expectedNumCornerDofs, dofSeparator.NumGlobalCornerDofs);
            double tolerance = 1E-13;
            for (int s = 0; s < 4; ++s)
            {
                UnsignedBooleanMatrix Bc = dofSeparator.CornerBooleanMatrices[s];
                Matrix expectedBc = Example4x4Quads.GetMatrixBc(s);
                Assert.True(expectedBc.Equals(Bc, tolerance));
            }
        }

        public static void TestMPI(string[] args)
        {
            using (new MPI.Environment(ref args))
            {
                int master = 0;
                var procs = new ProcessDistribution(Communicator.world, master, new int[] { 0, 1, 2, 3 });
                //Console.WriteLine($"(process {procs.OwnRank}) Hello World!"); // Run this to check if MPI works correctly.

                // Output
                string outputDirectory = @"C:\Users\Serafeim\Desktop\MPI\Tests";
                string outputFile = outputDirectory + $"\\MappingMatricesTests_process{procs.OwnRank}.txt";
                //File.Create(outputFile);
                //var writer = new FullMatrixWriter();

                // Create the model in master process
                var model = new ModelMpi(procs, Example4x4Quads.CreateHomogeneousModel);
                model.ConnectDataStructures();

                // Scatter subdomain data to each process
                model.ScatterSubdomains();
                ISubdomain subdomain = model.GetSubdomain(procs.OwnSubdomainID);
                //Console.WriteLine($"(process {procs.OwnRank}) Subdomain {model.GetSubdomain(procs.OwnSubdomainID).ID}");

                // Order dofs
                var dofOrderer = new DofOrdererMpi(procs, new NodeMajorDofOrderingStrategy(), new NullReordering());
                dofOrderer.OrderFreeDofs(model);

                // Separate dofs and corner boolean matrices
                var dofSeparator = new FetiDPDofSeparatorMpi(procs, model);
                var reordering = new MockReordering();
                ICornerNodeSelection cornerNodes = Example4x4Quads.DefineCornerNodeSelectionMpi(procs, model);
                dofSeparator.SeparateDofs(cornerNodes, reordering);

                #region old code where I called each method separately
                //if (procs.IsMasterProcess)
                //{
                //    HashSet<INode> globalCornerNodes = DefineGlobalCornerNodes(model);
                //    dofSeparator.DefineGlobalBoundaryDofs(globalCornerNodes);
                //    dofSeparator.DefineGlobalCornerDofs(globalCornerNodes);
                //}
                //HashSet<INode> subdomainCornerNodes = DefineSubdomainCornerNodes(subdomain);
                //dofSeparator.SeparateCornerRemainderDofs(subdomainCornerNodes);
                //dofSeparator.SeparateBoundaryInternalDofs(subdomainCornerNodes);
                //dofSeparator.CalcCornerMappingMatrices();
                #endregion

                // Check dof separation 
                (int[] cornerDofs, int[] remainderDofs, int[] boundaryRemainderDofs, int[] internalRemainderDofs) =
                    Example4x4Quads.GetDofSeparation(subdomain.ID);
                Utilities.CheckEqualMpi(procs, cornerDofs, dofSeparator.GetCornerDofIndices(subdomain));
                Utilities.CheckEqualMpi(procs, remainderDofs, dofSeparator.GetRemainderDofIndices(subdomain));
                Utilities.CheckEqualMpi(procs, boundaryRemainderDofs, dofSeparator.GetBoundaryDofIndices(subdomain));
                Utilities.CheckEqualMpi(procs, internalRemainderDofs, dofSeparator.GetInternalDofIndices(subdomain));

                // Check corner boolean matrices
                UnsignedBooleanMatrix Bc = dofSeparator.GetCornerBooleanMatrix(subdomain);
                Matrix expectedBc = Example4x4Quads.GetMatrixBc(subdomain.ID);
                double tolerance = 1E-13;
                //writer.WriteToFile(Bc, outputFile, true);
                Assert.True(expectedBc.Equals(Bc, tolerance));
                if (procs.IsMasterProcess)
                {
                    Assert.Equal(8, dofSeparator.NumGlobalCornerDofs);
                    foreach (ISubdomain sub in model.EnumerateSubdomains())
                    {
                        // All Bc matrices are also stored in master process
                        UnsignedBooleanMatrix globalLc = dofSeparator.GetCornerBooleanMatrix(sub);
                        Matrix expectedGlobalLc = Example4x4Quads.GetMatrixBc(sub.ID);
                        Assert.True(expectedGlobalLc.Equals(globalLc, tolerance));
                    }
                }

                // Check lagrange boolean matrices
                var crosspointStrategy = new FullyRedundantConstraints();
                var lagrangeEnumerator = new LagrangeMultipliersEnumeratorMpi(procs, model, crosspointStrategy, dofSeparator);
                lagrangeEnumerator.CalcBooleanMatrices(dofSeparator.GetRemainderDofOrdering);

                Assert.Equal(8, lagrangeEnumerator.NumLagrangeMultipliers);
                Matrix Br = lagrangeEnumerator.GetBooleanMatrix(subdomain).CopyToFullMatrix(false);
                Matrix expectedBr = Example4x4Quads.GetMatrixBr(subdomain.ID);
                Assert.True(expectedBr.Equals(Br, tolerance));
            }
        }

        internal class MockReordering : IFetiDPSeparatedDofReordering
        {
            public DofPermutation ReorderGlobalCornerDofs() 
                => DofPermutation.CreateNoPermutation();

            public DofPermutation ReorderSubdomainInternalDofs(ISubdomain subdomain)
            => DofPermutation.CreateNoPermutation();

            public DofPermutation ReorderSubdomainRemainderDofs(ISubdomain subdomain)
                => DofPermutation.CreateNoPermutation();
        }
    }
}
