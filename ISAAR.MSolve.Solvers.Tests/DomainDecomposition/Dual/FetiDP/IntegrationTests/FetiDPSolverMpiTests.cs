using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Analyzers.Loading;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.Discretization.Providers;
using ISAAR.MSolve.Discretization.Transfer;
using ISAAR.MSolve.FEM.Entities;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.CornerNodes;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.DofSeparation;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.StiffnessMatrices;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.LagrangeMultipliers;
using ISAAR.MSolve.Solvers.LinearSystems;
using ISAAR.MSolve.Solvers.Tests.DomainDecomposition.Dual.FetiDP.Utilities;
using MPI;
using Xunit;

//TODO: Perhaps I should also check intermediate steps by pulling the solver's compenent using reflection and check their state
//      and operations.
namespace ISAAR.MSolve.Solvers.Tests.DomainDecomposition.Dual.FetiDP.IntegrationTests
{
    public static class FetiDPSolverMpiTests
    {
        public static void TestSolutionGlobalDisplacements(MatrixFormat format)
        {
            (ProcessDistribution procs, IModel model, FetiDPSolverMpi solver) = CreateModelAndSolver(format);
            RunAnalysis(procs, model, solver);
            Vector globalU = solver.GatherGlobalDisplacements();

            // Check solution
            if (procs.IsMasterProcess)
            {
                double tol = 1E-8;
                Assert.True(Example4x4QuadsHomogeneous.SolutionGlobalDisplacements.Equals(globalU, tol));
            }
        }

        public static void TestSolutionSubdomainDisplacements(MatrixFormat format)
        {
            (ProcessDistribution procs, IModel model, FetiDPSolverMpi solver) = CreateModelAndSolver(format);
            RunAnalysis(procs, model, solver);

            // Check solution
            double tol = 1E-6;
            ISubdomain subdomain = model.GetSubdomain(procs.OwnSubdomainID);
            IVectorView ufComputed = solver.GetLinearSystem(subdomain).Solution;
            Vector ufExpected = Example4x4QuadsHomogeneous.GetSolutionFreeDisplacements(subdomain.ID);
            Assert.True(ufExpected.Equals(ufComputed, tol));
        }

        internal static void RunAnalysis(ProcessDistribution procs, IModel model, ISolverMpi solver)
        {
            // Run the analysis
            solver.OrderDofs(false);
            ISubdomain subdomain = model.GetSubdomain(procs.OwnSubdomainID);
            ILinearSystemMpi linearSystem = solver.GetLinearSystem(subdomain);
            linearSystem.Reset(); // Necessary to define the linear system's size 
            linearSystem.Subdomain.Forces = Vector.CreateZero(linearSystem.Size);
            linearSystem.RhsVector = linearSystem.Subdomain.Forces;

            solver.BuildGlobalMatrix(new ElementStructuralStiffnessProvider());
            model.ApplyLoads();
            LoadingUtilities.ApplyNodalLoadsMpi(procs, model, solver);
            solver.Solve();
        }

        private static (ProcessDistribution, IModel, FetiDPSolverMpi) CreateModelAndSolver(MatrixFormat format)
        {
            int master = 0;
            var procs = new ProcessDistribution(Communicator.world, master, new int[] { 0, 1, 2, 3 });

            // Prepare solver
            var model = new ModelMpi(procs, Example4x4QuadsHomogeneous.CreateModel);
            model.ConnectDataStructures();
            model.ScatterSubdomains();
            ICornerNodeSelection cornerNodes = Example4x4QuadsHomogeneous.DefineCornerNodeSelectionMpi(procs, model);
            IFetiDPMatrixManagerFactory fetiMatrices = MatrixFormatSelection.DefineMatrixManagerFactory(format);
            var solverBuilder = new FetiDPSolverMpi.Builder(procs, fetiMatrices);
            FetiDPSolverMpi solver = solverBuilder.Build(model, cornerNodes);

            return (procs, model, solver);
        }
    }
}
