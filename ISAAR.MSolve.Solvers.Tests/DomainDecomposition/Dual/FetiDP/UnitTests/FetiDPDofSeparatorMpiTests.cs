using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Entities;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.Discretization.Transfer;
using ISAAR.MSolve.FEM.Entities;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Matrices.Operators;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.CornerNodes;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.DofSeparation;
using ISAAR.MSolve.Solvers.Ordering;
using ISAAR.MSolve.Solvers.Ordering.Reordering;
using ISAAR.MSolve.Solvers.Tests.DomainDecomposition.Dual.FetiDP.UnitTests.Mocks;
using ISAAR.MSolve.Solvers.Tests.Utilities;
using MPI;
using Xunit;

//TODO: Mock all other classes.
//TODO: I should call the private methods that create the dof indices and the ones that create the corner boolean matrices,
//      instead of calling the public method SeparateDofs() that does everything.
namespace ISAAR.MSolve.Solvers.Tests.DomainDecomposition.Dual.FetiDP.UnitTests
{
    public static class FetiDPDofSeparatorMpiTests
    {
        public static void TestDofSeparation()
        {
            (ProcessDistribution procs, IModel model, FetiDPDofSeparatorMpi dofSeparator) = CreateModelAndDofSeparator();
            ISubdomain subdomain = model.GetSubdomain(procs.OwnSubdomainID);

            // Check dof separation 
            (int[] cornerDofs, int[] remainderDofs, int[] boundaryRemainderDofs, int[] internalRemainderDofs) =
                Example4x4QuadsHomogeneous.GetDofSeparation(subdomain.ID);
            ArrayChecking.CheckEqualMpi(procs, cornerDofs, dofSeparator.GetCornerDofIndices(subdomain));
            ArrayChecking.CheckEqualMpi(procs, remainderDofs, dofSeparator.GetRemainderDofIndices(subdomain));
            ArrayChecking.CheckEqualMpi(procs, boundaryRemainderDofs, dofSeparator.GetBoundaryDofIndices(subdomain));
            ArrayChecking.CheckEqualMpi(procs, internalRemainderDofs, dofSeparator.GetInternalDofIndices(subdomain));
        }

        public static void TestCornerBooleanMatrices()
        {
            (ProcessDistribution procs, IModel model, FetiDPDofSeparatorMpi dofSeparator) = CreateModelAndDofSeparator();
            ISubdomain subdomain = model.GetSubdomain(procs.OwnSubdomainID);

            // Check corner boolean matrices
            UnsignedBooleanMatrix Bc = dofSeparator.GetCornerBooleanMatrix(subdomain);
            Matrix expectedBc = Example4x4QuadsHomogeneous.GetMatrixBc(subdomain.ID);
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
                    Matrix expectedGlobalLc = Example4x4QuadsHomogeneous.GetMatrixBc(sub.ID);
                    Assert.True(expectedGlobalLc.Equals(globalLc, tolerance));
                }
            }
        }

        internal static (ProcessDistribution, IModel, FetiDPDofSeparatorMpi) CreateModelAndDofSeparator()
        {
            int master = 0;
            int[] processesToClusters = { 0, 1, 2, 3 };
            int[] processesToSubdomains = { 0, 1, 2, 3 };
            var procs = new ProcessDistribution(Communicator.world, master, processesToClusters, processesToSubdomains);
            //Console.WriteLine($"(process {procs.OwnRank}) Hello World!"); // Run this to check if MPI works correctly.

            // Output
            string outputDirectory = @"C:\Users\Serafeim\Desktop\MPI\Tests";
            string outputFile = outputDirectory + $"\\MappingMatricesTests_process{procs.OwnRank}.txt";
            //File.Create(outputFile);
            //var writer = new FullMatrixWriter();

            // Create the model in master process
            var model = new ModelMpi(procs, Example4x4QuadsHomogeneous.CreateModel);
            if (procs.IsMasterProcess)
            {
                for (int s = 0; s < 4; ++s)
                {
                    model.Clusters[s] = new Cluster(s);
                    model.Clusters[s].Subdomains.Add(model.GetSubdomain(s));
                }
            }
            model.ConnectDataStructures();

            // Scatter subdomain data to each process
            model.ScatterSubdomains();
            ISubdomain subdomain = model.GetSubdomain(procs.OwnSubdomainID);
            //Console.WriteLine($"(process {procs.OwnRank}) Subdomain {model.GetSubdomain(procs.OwnSubdomainID).ID}");

            // Order dofs
            var dofOrderer = new DofOrdererMpi(procs, new NodeMajorDofOrderingStrategy(), new NullReordering());
            dofOrderer.OrderFreeDofs(model);

            // Separate dofs and corner boolean matrices
            var reordering = new MockSeparatedDofReordering();
            ICornerNodeSelection cornerNodes = Example4x4QuadsHomogeneous.DefineCornerNodeSelectionMpi(procs, model);
            var dofSeparator = new FetiDPDofSeparatorMpi(procs, model, cornerNodes);
            dofSeparator.SeparateDofs(reordering);

            return (procs, model, dofSeparator);
        }
    }
}
