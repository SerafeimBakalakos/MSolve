using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.Discretization.Transfer;
using ISAAR.MSolve.FEM.Entities;
using ISAAR.MSolve.FEM.Transfer;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Matrices.Operators;
using ISAAR.MSolve.Solvers.Direct;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.LagrangeMultipliers;
using ISAAR.MSolve.Solvers.Ordering;
using ISAAR.MSolve.Solvers.Ordering.Reordering;
using MPI;
using Xunit;

//TODO: Remove redundancies between the methods that define corner nodes
namespace ISAAR.MSolve.Solvers.Tests.DomainDecomposition.Dual.FetiDP
{
    public static class Quads4x4MappingMatricesTests
    {
        public static Model CreateModel()
        {
            //                                    Λ P
            //                                    | 
            //                                     
            // |> 20 ---- 21 ---- 22 ---- 23 ---- 24
            //    |  (12) |  (13) |  (14) |  (15) |
            //    |       |       |       |       |
            // |> 15 ---- 16 ---- 17 ---- 18 ---- 19
            //    |  (8)  |  (9)  |  (10) |  (11) |
            //    |       |       |       |       |
            // |> 10 ---- 11 ---- 12 ---- 13 ---- 14
            //    |  (4)  |  (5)  |  (6)  |  (7)  |
            //    |       |       |       |       |
            // |> 5 ----- 6 ----- 7 ----- 8 ----- 9
            //    |  (0)  |  (1)  |  (2)  |  (3)  |
            //    |       |       |       |       |
            // |> 0 ----- 1 ----- 2 ----- 3 ----- 4


            var builder = new Uniform2DModelBuilder();
            builder.DomainLengthX = 4.0;
            builder.DomainLengthY = 4.0;
            builder.NumSubdomainsX = 2;
            builder.NumSubdomainsY = 2;
            builder.NumTotalElementsX = 4;
            builder.NumTotalElementsY = 4;
            builder.YoungModulus = 1.0;
            builder.PrescribeDisplacement(Uniform2DModelBuilder.BoundaryRegion.LeftSide, StructuralDof.TranslationX, 0.0);
            builder.PrescribeDisplacement(Uniform2DModelBuilder.BoundaryRegion.LeftSide, StructuralDof.TranslationY, 0.0);
            builder.DistributeLoadAtNodes(Uniform2DModelBuilder.BoundaryRegion.UpperRightCorner, StructuralDof.TranslationY, 10.0);

            return builder.BuildModel();
        }

        public static Dictionary<int, HashSet<INode>> DefineCornerNodes(Model model)
        {
            // subdomain 2         subdomain 3                      
            // 20 ---- 21 ---- 22  22---- 23 ---- 24
            // |  (12) |  (13) |   | (14) |  (15) |
            // |       |       |   |      |       |
            // 15 ---- 16 ---- 17  17---- 18 ---- 19
            // |  (8)  |  (9)  |   | (10) |  (11) |
            // |       |       |   |      |       |
            // 10 ---- 11 ---- 12  12---- 13 ---- 14

            // subdomain 0         subdomain 1
            // 10 ---- 11 ---- 12  12---- 13 ---- 14
            // |  (4)  |  (5)  |   | (6)  |  (7)  |
            // |       |       |   |      |       |
            // 5 ----- 6 ----- 7   7 ---- 8 ----- 9
            // |  (0)  |  (1)  |   | (2)  |  (3)  |
            // |       |       |   |      |       |
            // 0 ----- 1 ----- 2   2 ---- 3 ----- 4

            var cornerNodes = new Dictionary<int, HashSet<INode>>();
            cornerNodes[0] = new HashSet<INode>(new INode[] { model.NodesDictionary[2], model.NodesDictionary[12] });
            cornerNodes[1] = new HashSet<INode>(new INode[] { model.NodesDictionary[2], model.NodesDictionary[12], model.NodesDictionary[14] });
            cornerNodes[2] = new HashSet<INode>(new INode[] { model.NodesDictionary[12], model.NodesDictionary[22] });
            cornerNodes[3] = new HashSet<INode>(new INode[] { model.NodesDictionary[12], model.NodesDictionary[14], model.NodesDictionary[22] });
            return cornerNodes;
        }

        public static HashSet<INode> DefineGlobalCornerNodes(Model model)
        {
            return new HashSet<INode>(new INode[] 
            {
                model.NodesDictionary[2], model.NodesDictionary[12], model.NodesDictionary[14], model.NodesDictionary[22]
            });
        }

        public static HashSet<INode> DefineSubdomainCornerNodes(ISubdomain subdomain)
        {
            if (subdomain.ID == 0)
            {
                return new HashSet<INode>(new INode[] { subdomain.GetNode(2), subdomain.GetNode(12) });
            }
            else if (subdomain.ID == 1)
            {
                return new HashSet<INode>(new INode[] { subdomain.GetNode(2), subdomain.GetNode(12), subdomain.GetNode(14) });
            }
            else if (subdomain.ID == 2)
            {
                return new HashSet<INode>(new INode[] { subdomain.GetNode(12), subdomain.GetNode(22) });
            }
            else if (subdomain.ID == 3)
            {
                return new HashSet<INode>(new INode[] { subdomain.GetNode(12), subdomain.GetNode(14), subdomain.GetNode(22) });
            }
            else throw new ArgumentException("Subdomain ID must be 0, 1, 2 or 3");
        }

        public static Matrix GetExpectedCornerBooleanMatrix(int subdomainID)
        {
            if (subdomainID == 0)
            {
                var Lc = Matrix.CreateZero(4, 8);
                Lc[0, 0] = 1;
                Lc[1, 1] = 1;
                Lc[2, 2] = 1;
                Lc[3, 3] = 1;
                return Lc;
            }
            else if (subdomainID == 1)
            {
                var Lc = Matrix.CreateZero(6, 8);
                Lc[0, 0] = 1;
                Lc[1, 1] = 1;
                Lc[2, 2] = 1;
                Lc[3, 3] = 1;
                Lc[4, 4] = 1;
                Lc[5, 5] = 1;
                return Lc;
            }
            else if (subdomainID == 2)
            {
                var Lc = Matrix.CreateZero(4, 8);
                Lc[0, 2] = 1;
                Lc[1, 3] = 1;
                Lc[2, 6] = 1;
                Lc[3, 7] = 1;
                return Lc;
            }
            else if (subdomainID == 3)
            {
                var Lc = Matrix.CreateZero(6, 8);
                Lc[0, 2] = 1;
                Lc[1, 3] = 1;
                Lc[2, 4] = 1;
                Lc[3, 5] = 1;
                Lc[4, 6] = 1;
                Lc[5, 7] = 1;
                return Lc;
            }
            else throw new ArgumentException("Subdomain ID must be 0, 1, 2 or 3");
        }

        public static Matrix GetExpectedLagrangeBooleanMatrix(int subdomainID)
        {
            if (subdomainID == 0)
            {
                var Br = Matrix.CreateZero(8, 8);
                Br[0, 4] = +1;
                Br[1, 5] = +1;
                Br[2, 6] = +1;
                Br[3, 7] = +1;
                return Br;
            }
            else if (subdomainID == 1)
            {
                var Br = Matrix.CreateZero(8, 12);
                Br[0, 4] = -1;
                Br[1, 5] = -1;
                Br[4, 10] = +1;
                Br[5, 11] = +1;
                return Br;
            }
            else if (subdomainID == 2)
            {
                var Br = Matrix.CreateZero(8, 8);
                Br[2, 0] = -1;
                Br[3, 1] = -1;
                Br[6, 4] = +1;
                Br[7, 5] = +1;
                return Br;
            }
            else if (subdomainID == 3)
            {
                var Br = Matrix.CreateZero(8, 12);
                Br[4, 0] = -1;
                Br[5, 1] = -1;
                Br[6, 2] = -1;
                Br[7, 3] = -1;
                return Br;
            }
            else throw new ArgumentException("Subdomain ID must be 0, 1, 2 or 3");
        }

        public static (int[] cornerDofs, int[] remainderDofs, int[] boundaryRemainderDofs, int[] internalRemainderDofs) 
            GetExpectedDofSeparation(int subdomainID)
        {
            if (subdomainID == 0)
            {
                var cornerDofs = new int[] { 2, 3, 10, 11 };
                var remainderDofs = new int[] { 0, 1, 4, 5, 6, 7, 8, 9 };
                var boundaryDofs = new int[] { 4, 5, 6, 7 };
                var internalDofs = new int[] { 0, 1, 2, 3 };
                return (cornerDofs, remainderDofs, boundaryDofs, internalDofs);
            }
            else if (subdomainID == 1)
            {
                var cornerDofs = new int[] { 0, 1, 12, 13, 16, 17 };
                var remainderDofs = new int[] { 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 14, 15 };
                var boundaryDofs = new int[] { 4, 5, 10, 11 };
                var internalDofs = new int[] { 0, 1, 2, 3, 6, 7, 8, 9 };
                return (cornerDofs, remainderDofs, boundaryDofs, internalDofs);
            }
            else if (subdomainID == 2)
            {
                var cornerDofs = new int[] { 2, 3, 10, 11 };
                var remainderDofs = new int[] { 0, 1, 4, 5, 6, 7, 8, 9 };
                var boundaryDofs = new int[] { 0, 1, 4, 5 };
                var internalDofs = new int[] { 2, 3, 6, 7 };
                return (cornerDofs, remainderDofs, boundaryDofs, internalDofs);
            }
            else if (subdomainID == 3)
            {
                var cornerDofs = new int[] { 0, 1, 4, 5, 12, 13 };
                var remainderDofs = new int[] { 2, 3, 6, 7, 8, 9, 10, 11, 14, 15, 16, 17 };
                var boundaryDofs = new int[] { 0, 1, 2, 3 };
                var internalDofs = new int[] { 4, 5, 6, 7, 8, 9, 10, 11 };
                return (cornerDofs, remainderDofs, boundaryDofs, internalDofs);
            }
            else throw new ArgumentException("Subdomain ID must be 0, 1, 2 or 3");
        }

        [Fact]
        public static void TestDofSeparation()
        {
            // Create model
            Model model = CreateModel();
            Dictionary<int, HashSet<INode>> cornerNodes = DefineCornerNodes(model);
            model.ConnectDataStructures();

            // Order free dofs.
            var dofOrderer = new DofOrderer(new NodeMajorDofOrderingStrategy(), new NullReordering());
            dofOrderer.OrderFreeDofs(model);

            // Separate dofs
            var dofSeparator = new FetiDPDofSeparator();
            foreach (ISubdomain subdomain in model.EnumerateSubdomains())
            {
                int s = subdomain.ID;
                dofSeparator.SeparateCornerRemainderDofs(subdomain, cornerNodes[s]);
                dofSeparator.SeparateBoundaryInternalDofs(subdomain, cornerNodes[s]);
            }
            
            // Check
            for (int s = 0; s < 4; ++s)
            {
                (int[] cornerDofs, int[] remainderDofs, int[] boundaryRemainderDofs, int[] internalRemainderDofs) =
                    GetExpectedDofSeparation(s);
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
            Model model = CreateModel();
            Dictionary<int, HashSet<INode>> cornerNodes = DefineCornerNodes(model);
            var cornerNodesGlobal = new HashSet<INode>();
            foreach (IEnumerable<INode> subdomainNodes in cornerNodes.Values) cornerNodesGlobal.UnionWith(subdomainNodes);
            model.ConnectDataStructures();

            // Order free dofs.
            var dofOrderer = new DofOrderer(new NodeMajorDofOrderingStrategy(), new NullReordering());
            dofOrderer.OrderFreeDofs(model);

            // Separate dofs
            var dofSeparator = new FetiDPDofSeparator();
            dofSeparator.DefineGlobalBoundaryDofs(model, cornerNodesGlobal);
            foreach (ISubdomain subdomain in model.EnumerateSubdomains())
            {
                int s = subdomain.ID;
                dofSeparator.SeparateCornerRemainderDofs(subdomain, cornerNodes[s]);
                dofSeparator.SeparateBoundaryInternalDofs(subdomain, cornerNodes[s]);
            }

            // Enumerate lagranges
            var crosspointStrategy = new FullyRedundantConstraints();
            var lagrangeEnumerator = new FetiDPLagrangeMultipliersEnumerator(crosspointStrategy, dofSeparator);
            lagrangeEnumerator.DefineBooleanMatrices(model);

            // Check
            int expectedNumLagrangeMultipliers = 8;
            Assert.Equal(expectedNumLagrangeMultipliers, lagrangeEnumerator.NumLagrangeMultipliers);
            double tolerance = 1E-13;
            for (int s = 0; s < 4; ++s)
            {
                Matrix Br = lagrangeEnumerator.BooleanMatrices[s].CopyToFullMatrix(false);
                Matrix expectedBr = GetExpectedLagrangeBooleanMatrix(s);
                Assert.True(expectedBr.Equals(Br, tolerance));
            }
        }

        [Fact]
        public static void TestUnsignedBooleanMatrices()
        {
            // Create model
            Model model = CreateModel();
            Dictionary<int, HashSet<INode>> cornerNodes = DefineCornerNodes(model);
            var cornerNodesGlobal = new HashSet<INode>();
            foreach (IEnumerable<INode> subdomainNodes in cornerNodes.Values) cornerNodesGlobal.UnionWith(subdomainNodes);
            model.ConnectDataStructures();

            // Order free dofs.
            var dofOrderer = new DofOrderer(new NodeMajorDofOrderingStrategy(), new NullReordering());
            dofOrderer.OrderFreeDofs(model);

            // Separate dofs
            var dofSeparator = new FetiDPDofSeparator();
            dofSeparator.DefineGlobalCornerDofs(model, cornerNodesGlobal);
            foreach (ISubdomain subdomain in model.EnumerateSubdomains())
            {
                int s = subdomain.ID;
                //IEnumerable<INode> remainderAndConstrainedNodes = subdomain.Nodes.Where(node => !cornerNodes[s].Contains(node));
                dofSeparator.SeparateCornerRemainderDofs(subdomain, cornerNodes[s]);
                dofSeparator.SeparateBoundaryInternalDofs(subdomain, cornerNodes[s]);
            }
            dofSeparator.CalcCornerMappingMatrices(model);

            // Check
            int expectedNumCornerDofs = 8;
            Assert.Equal(expectedNumCornerDofs, dofSeparator.NumGlobalCornerDofs);
            double tolerance = 1E-13;
            for (int s = 0; s < 4; ++s)
            {
                UnsignedBooleanMatrix Lc = dofSeparator.CornerBooleanMatrices[s];
                Matrix expectedLc = GetExpectedCornerBooleanMatrix(s);
                Assert.True(expectedLc.Equals(Lc, tolerance));
            }
        }

        public static void TestMPI(string[] args)
        {
            using (new MPI.Environment(ref args))
            {
                int master = 0;
                var procs = new ProcessDistribution(Communicator.world, 0, new int[] { 0, 1, 2, 3 });
                //Console.WriteLine($"(process {procs.OwnRank}) Hello World!"); // Run this to check if MPI works correctly.

                // Create the model in master process
                Model model = null;
                if (procs.IsMasterProcess)
                {
                    model = CreateModel();
                    model.ConnectDataStructures();
                }

                // Scatter subdomain data to each process
                var transfer = new MpiTransfer(new SubdomainSerializer());
                ISubdomain subdomain = transfer.ScatterSubdomains(model, master);
                //Console.WriteLine($"(process { rank}) Subdomain { subdomain.ID}");

                // Order dofs
                subdomain.ConnectDataStructures();
                var dofSerializer = new StandardDofSerializer();
                var dofOrderer = new DofOrdererMpi(procs, new NodeMajorDofOrderingStrategy(), new NullReordering());
                subdomain.FreeDofOrdering = dofOrderer.OrderFreeDofs(subdomain);
                dofOrderer.OrderFreeDofs(model);

                // Separate dofs and corner boolean matrices
                var dofSeparator = new FetiDPDofSeparatorMpi(procs, model);
                if (procs.IsMasterProcess)
                {
                    HashSet<INode> globalCornerNodes = DefineGlobalCornerNodes(model);
                    dofSeparator.DefineGlobalBoundaryDofs(globalCornerNodes);
                    dofSeparator.DefineGlobalCornerDofs(globalCornerNodes);
                }
                HashSet<INode> subdomainCornerNodes = DefineSubdomainCornerNodes(subdomain);
                dofSeparator.SeparateCornerRemainderDofs(subdomainCornerNodes);
                dofSeparator.SeparateBoundaryInternalDofs(subdomainCornerNodes);

                // Check dof separation 
                (int[] cornerDofs, int[] remainderDofs, int[] boundaryRemainderDofs, int[] internalRemainderDofs) =
                    GetExpectedDofSeparation(subdomain.ID);
                Utilities.CheckEqual(cornerDofs, dofSeparator.SubdomainDofs.CornerDofIndices);
                Utilities.CheckEqual(remainderDofs, dofSeparator.SubdomainDofs.RemainderDofIndices);
                Utilities.CheckEqual(boundaryRemainderDofs, dofSeparator.SubdomainDofs.BoundaryDofIndices);
                Utilities.CheckEqual(internalRemainderDofs, dofSeparator.SubdomainDofs.InternalDofIndices);

                // Create and check corner boolean matrices
                dofSeparator.CalcCornerMappingMatrices();
                UnsignedBooleanMatrix Lc = dofSeparator.SubdomainDofs.CornerBooleanMatrix;
                Matrix expectedLc = GetExpectedCornerBooleanMatrix(subdomain.ID);
                double tolerance = 1E-13;
                Assert.True(expectedLc.Equals(Lc, tolerance));
                if (procs.IsMasterProcess)
                {
                    Assert.Equal(8, dofSeparator.GlobalDofs.NumGlobalCornerDofs);
                    for (int s = 0; s < 4; ++s)
                    {
                        // All Lc matrices are also stored in master process
                        UnsignedBooleanMatrix globalLc = dofSeparator.GlobalDofs.CornerBooleanMatrices[s];
                        Matrix expectedGlobalLc = GetExpectedCornerBooleanMatrix(s);
                        Assert.True(expectedGlobalLc.Equals(globalLc, tolerance));
                    }
                }

                // Create and check lagrange boolean matrices
                var crosspointStrategy = new FullyRedundantConstraints();
                var lagrangeEnumerator = new FetiDPLagrangeMultipliersEnumeratorMpi(procs, model, crosspointStrategy, dofSeparator);
                lagrangeEnumerator.CalcBooleanMatrices();

                Assert.Equal(8, lagrangeEnumerator.NumLagrangeMultipliers);
                Matrix Br = lagrangeEnumerator.BooleanMatrix.CopyToFullMatrix(false);
                Matrix expectedBr = GetExpectedLagrangeBooleanMatrix(subdomain.ID);
                Assert.True(expectedBr.Equals(Br, tolerance));
            }
        }
    }
}
