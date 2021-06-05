using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;
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
        [Theory]
        [InlineData(EnvironmentChoice.SequentialSharedEnvironment)]
        [InlineData(EnvironmentChoice.TplSharedEnvironment)]
        public static void TestForLine1D(EnvironmentChoice environmentChoice) 
            => TestForLine1DInternal(Utilities.CreateEnvironment(environmentChoice));

        internal static void TestForLine1DInternal(IComputeEnvironment environment)
        {
            ComputeNodeTopology nodeTopology = Line1DExample.CreateNodeTopology(environment);
            environment.Initialize(nodeTopology);

            IStructuralModel model = Line1DExample.CreateMultiSubdomainModel(environment);
            model.ConnectDataStructures();
            var subdomainTopology = new SubdomainTopology(environment, model);
            ModelUtilities.OrderDofs(model);

            var dofSeparator = new PsmDofSeparator(environment, model, subdomainTopology, true);
            dofSeparator.SeparateSubdomainDofsIntoBoundaryInternal();
            dofSeparator.FindCommonDofsBetweenSubdomains();
            DistributedOverlappingIndexer indexer = dofSeparator.CreateDistributedVectorIndexer();

            // Check
            Line1DExample.CheckDistributedIndexer(environment, nodeTopology, indexer);
        }

        [Theory]
        [InlineData(EnvironmentChoice.SequentialSharedEnvironment)]
        [InlineData(EnvironmentChoice.TplSharedEnvironment)]
        public static void TestForPlane2D(EnvironmentChoice environmentChoice)
            => TestForPlane2DInternal(Utilities.CreateEnvironment(environmentChoice));

        internal static void TestForPlane2DInternal(IComputeEnvironment environment)
        {
            ComputeNodeTopology nodeTopology = Plane2DExample.CreateNodeTopology(environment);
            environment.Initialize(nodeTopology);

            IStructuralModel model = Plane2DExample.CreateMultiSubdomainModel(environment);
            model.ConnectDataStructures();
            var subdomainTopology = new SubdomainTopology(environment, model);
            ModelUtilities.OrderDofs(model);

            var dofSeparator = new PsmDofSeparator(environment, model, subdomainTopology, true);
            dofSeparator.SeparateSubdomainDofsIntoBoundaryInternal();
            dofSeparator.FindCommonDofsBetweenSubdomains();
            DistributedOverlappingIndexer indexer = dofSeparator.CreateDistributedVectorIndexer();

            // Check
            Plane2DExample.CheckDistributedIndexer(environment, nodeTopology, indexer);
        }
    }
}
