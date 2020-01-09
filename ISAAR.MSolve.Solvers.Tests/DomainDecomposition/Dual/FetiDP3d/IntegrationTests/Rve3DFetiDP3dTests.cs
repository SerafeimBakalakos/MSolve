using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ISAAR.MSolve.Analyzers;
using ISAAR.MSolve.Analyzers.Loading;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.Discretization.Providers;
using ISAAR.MSolve.FEM.Entities;
using ISAAR.MSolve.LinearAlgebra.Reordering;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Logging.DomainDecomposition;
using ISAAR.MSolve.Problems;
using ISAAR.MSolve.Solvers.Direct;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.CornerNodes;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.InterfaceProblem;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.StiffnessMatrices;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP3d;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP3d.Augmentation;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP3d.StiffnessMatrices;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.LagrangeMultipliers;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.Pcg;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.Preconditioning;
using ISAAR.MSolve.Solvers.LinearSystems;
using ISAAR.MSolve.Solvers.Logging;
using Xunit;

namespace ISAAR.MSolve.Solvers.Tests.DomainDecomposition.Dual.FetiDP3d.IntegrationTests
{
    public static class Rve3DFetiDP3dTests
    {
        public enum Crosspoints { Minimum, FullyRedundant }
        public enum Precond { Dirichlet, DirichletDiagonal, Lumped }

        private const int singleSubdomainID = 0;

        [Theory]
        //[InlineData(Precond.Dirichlet, Crosspoints.FullyRedundant)]
        //[InlineData(Precond.DirichletDiagonal, Crosspoints.FullyRedundant)]
        //[InlineData(Precond.Lumped, Crosspoints.FullyRedundant)]
        [InlineData(Precond.Dirichlet, Crosspoints.Minimum)]
        //[InlineData(Precond.DirichletDiagonal, Crosspoints.Minimum)]
        //[InlineData(Precond.Lumped, Crosspoints.Minimum)]
        public static void Run(Precond precond, Crosspoints crosspoints)
        {
            double pcgConvergenceTol = 1E-5;
            IVectorView directDisplacements = SolveModelWithoutSubdomains(1.0);
            (IVectorView ddDisplacements, ISolverLogger logger) =
                SolveModelWithSubdomains(1.0, precond, crosspoints, pcgConvergenceTol);
            double normalizedError = directDisplacements.Subtract(ddDisplacements).Norm2() / directDisplacements.Norm2();

            // The error is provided in the reference solution the, but it is almost impossible for two different codes run on 
            // different machines to achieve the exact same accuracy.
            Assert.Equal(0.0, normalizedError, 6);
        }

        internal static Model CreateModel(double stiffnessRatio)
        {
            // Subdomains:

            double E0 = 2.1E0;
            //double E1 = stiffnessRatio * E0;

            var builder = new Uniform3DModelBuilder();
            builder.DomainLengthX = 8.0;
            builder.DomainLengthY = 8.0;
            builder.DomainLengthZ = 8.0;
            builder.NumSubdomainsX = 2;
            builder.NumSubdomainsY = 2;
            builder.NumSubdomainsZ = 2;
            builder.NumTotalElementsX = 8;
            builder.NumTotalElementsY = 8;
            builder.NumTotalElementsZ = 8;
            builder.YoungModulus = E0;
            //builder.YoungModuliOfSubdomains = new double[,] { { E1, E0, E0, E0 }, { E1, E0, E0, E0 } };

            #region minimum BCs
            //builder.PrescribeDisplacement(Uniform3DModelBuilder.BoundaryRegion.MinXMinYMinZ, StructuralDof.TranslationX, 0.0);
            //builder.PrescribeDisplacement(Uniform3DModelBuilder.BoundaryRegion.MinXMinYMinZ, StructuralDof.TranslationY, 0.0);
            //builder.PrescribeDisplacement(Uniform3DModelBuilder.BoundaryRegion.MinXMinYMinZ, StructuralDof.TranslationZ, 0.0);

            //builder.PrescribeDisplacement(Uniform3DModelBuilder.BoundaryRegion.MaxXMinYMinZ, StructuralDof.TranslationX, 0.0);
            //builder.PrescribeDisplacement(Uniform3DModelBuilder.BoundaryRegion.MaxXMinYMinZ, StructuralDof.TranslationY, 0.0);
            //builder.PrescribeDisplacement(Uniform3DModelBuilder.BoundaryRegion.MaxXMinYMinZ, StructuralDof.TranslationZ, 0.0);

            //builder.PrescribeDisplacement(Uniform3DModelBuilder.BoundaryRegion.MinXMaxYMinZ, StructuralDof.TranslationX, 0.0);
            //builder.PrescribeDisplacement(Uniform3DModelBuilder.BoundaryRegion.MinXMaxYMinZ, StructuralDof.TranslationY, 0.0);
            //builder.PrescribeDisplacement(Uniform3DModelBuilder.BoundaryRegion.MinXMaxYMinZ, StructuralDof.TranslationZ, 0.0);

            //builder.DistributeLoadAtNodes(Uniform3DModelBuilder.BoundaryRegion.MaxXMaxYMaxZ, StructuralDof.TranslationZ, 100.0);
            #endregion

            #region linear BCs
            builder.PrescribeDisplacement(Uniform3DModelBuilder.BoundaryRegion.MinX, StructuralDof.TranslationX, 0.0);
            builder.PrescribeDisplacement(Uniform3DModelBuilder.BoundaryRegion.MinX, StructuralDof.TranslationY, 0.0);
            builder.PrescribeDisplacement(Uniform3DModelBuilder.BoundaryRegion.MinX, StructuralDof.TranslationZ, 0.0);

            builder.PrescribeDisplacement(Uniform3DModelBuilder.BoundaryRegion.MinY, StructuralDof.TranslationX, 0.0);
            builder.PrescribeDisplacement(Uniform3DModelBuilder.BoundaryRegion.MinY, StructuralDof.TranslationY, 0.0);
            builder.PrescribeDisplacement(Uniform3DModelBuilder.BoundaryRegion.MinY, StructuralDof.TranslationZ, 0.0);

            builder.PrescribeDisplacement(Uniform3DModelBuilder.BoundaryRegion.MinZ, StructuralDof.TranslationX, 0.0);
            builder.PrescribeDisplacement(Uniform3DModelBuilder.BoundaryRegion.MinZ, StructuralDof.TranslationY, 0.0);
            builder.PrescribeDisplacement(Uniform3DModelBuilder.BoundaryRegion.MinZ, StructuralDof.TranslationZ, 0.0);

            builder.PrescribeDisplacement(Uniform3DModelBuilder.BoundaryRegion.MaxX, StructuralDof.TranslationX, 0.0);
            builder.PrescribeDisplacement(Uniform3DModelBuilder.BoundaryRegion.MaxX, StructuralDof.TranslationY, 0.0);
            builder.PrescribeDisplacement(Uniform3DModelBuilder.BoundaryRegion.MaxX, StructuralDof.TranslationZ, 0.0);

            builder.PrescribeDisplacement(Uniform3DModelBuilder.BoundaryRegion.MaxY, StructuralDof.TranslationX, 0.0);
            builder.PrescribeDisplacement(Uniform3DModelBuilder.BoundaryRegion.MaxY, StructuralDof.TranslationY, 0.0);
            builder.PrescribeDisplacement(Uniform3DModelBuilder.BoundaryRegion.MaxY, StructuralDof.TranslationZ, 0.0);

            builder.PrescribeDisplacement(Uniform3DModelBuilder.BoundaryRegion.MaxZ, StructuralDof.TranslationX, 0.0);
            builder.PrescribeDisplacement(Uniform3DModelBuilder.BoundaryRegion.MaxZ, StructuralDof.TranslationY, 0.0);
            builder.PrescribeDisplacement(Uniform3DModelBuilder.BoundaryRegion.MaxZ, StructuralDof.TranslationZ, 0.0);

            //builder.DistributeLoadAtNodes(Uniform3DModelBuilder.BoundaryRegion.MaxXMaxYMaxZ, StructuralDof.TranslationZ, 100.0);
            #endregion

            return builder.BuildModel();
        }

        internal static IVectorView SolveModelWithoutSubdomains(double stiffnessRatio)
        {
            Model model = CreateSingleSubdomainModel(stiffnessRatio);

            // Solver
            SkylineSolver solver = (new SkylineSolver.Builder()).BuildSolver(model);

            // Structural problem provider
            var provider = new ProblemStructural(model, solver);

            // Linear static analysis
            var childAnalyzer = new LinearAnalyzer(model, solver, provider);
            var parentAnalyzer = new StaticAnalyzer(model, solver, provider, childAnalyzer);

            // Run the analysis
            parentAnalyzer.Initialize();
            parentAnalyzer.Solve();

            return solver.LinearSystems[singleSubdomainID].Solution;
        }

        private static Model CreateSingleSubdomainModel(double stiffnessRatio)
        {
            // Replace the existing subdomains with a single one 
            Model model = CreateModel(stiffnessRatio);
            model.SubdomainsDictionary.Clear();
            var subdomain = new Subdomain(singleSubdomainID);
            model.SubdomainsDictionary.Add(singleSubdomainID, subdomain);
            foreach (Element element in model.ElementsDictionary.Values) subdomain.Elements.Add(element.ID, element);
            return model;
        }

        private static (IVectorView globalDisplacements, ISolverLogger logger) SolveModelWithSubdomains(double stiffnessRatio,
            Precond precond, Crosspoints crosspoints, double pcgConvergenceTolerance)
        {
            // Model
            Model model = CreateModel(stiffnessRatio);
            model.ConnectDataStructures();

            // Corner, midside nodes
            double meshTol = 1E-6;
            var cornerNodesOfEachSubdomain = new Dictionary<ISubdomain, HashSet<INode>>();
            foreach (Subdomain subdomain in model.SubdomainsDictionary.Values)
            {
                subdomain.DefineNodesFromElements(); //TODO: This will also be called by the analyzer.
                INode[] corners = CornerNodeUtilities.FindCornersOfBrick3D(subdomain);
                var cornerNodes = new HashSet<INode>();
                foreach (INode node in corners)
                {
                    if (node.Constraints.Count > 0) continue;
                    //if ((Math.Abs(node.X - domainLengthX) <= meshTol) && (Math.Abs(node.Y) <= meshTol)) continue;
                    //if ((Math.Abs(node.X - domainLengthX) <= meshTol) && (Math.Abs(node.Y - domainLengthY) <= meshTol)) continue;
                    cornerNodes.Add(node);
                }
                cornerNodesOfEachSubdomain[subdomain] = cornerNodes;
            }
            var cornerNodeSelection = new UsedDefinedCornerNodes(cornerNodesOfEachSubdomain);
            IMidsideNodesSelection midsideNodesSelection = DefineMidsideNodes(model);

            // Solver
            var fetiMatrices = new FetiDP3dMatrixManagerFactoryDense();
            var solverBuilder = new FetiDP3dSolverSerial.Builder(fetiMatrices);
            solverBuilder.ProblemIsHomogeneous = stiffnessRatio == 1.0;

            // Preconditioner
            if (precond == Precond.Lumped) solverBuilder.Preconditioning = new LumpedPreconditioning();
            else if (precond == Precond.Dirichlet) solverBuilder.Preconditioning = new DirichletPreconditioning();
            else solverBuilder.Preconditioning = new DiagonalDirichletPreconditioning();

            // Crosspoint strategy
            if (crosspoints == Crosspoints.FullyRedundant) solverBuilder.CrosspointStrategy = new FullyRedundantConstraints();
            else if (crosspoints == Crosspoints.Minimum) solverBuilder.CrosspointStrategy = new MinimumConstraints();
            else throw new ArgumentException();

            // Specify PCG settings
            solverBuilder.PcgSettings = new PcgSettings() { ConvergenceTolerance = pcgConvergenceTolerance };

            FetiDP3dSolverSerial fetiSolver = solverBuilder.Build(model, cornerNodeSelection, midsideNodesSelection);
            //if (residualIsExact) exactResidualConvergence.FetiSolver = fetiSolver;

            // Plot for debugging
            string path = @"C:\Users\Serafeim\Desktop\FETI-DP\Plots";
            var logger = new DomainDecompositionLoggerFetiDP(cornerNodeSelection, path, true);
            logger.PlotSubdomains(model);

            // Run the analysis
            RunAnalysis(model, fetiSolver); //check dof separator

            // Gather the global displacements
            Vector globalDisplacements = fetiSolver.GatherGlobalDisplacements();

            return (globalDisplacements, fetiSolver.Logger);
        }

        private static void RunAnalysis(IModel model, ISolverMpi solver)
        {
            // Run the analysis
            solver.OrderDofs(false);
            foreach (ISubdomain subdomain in model.EnumerateSubdomains())
            {
                ILinearSystem linearSystem = solver.GetLinearSystem(subdomain);
                linearSystem.Reset(); // Necessary to define the linear system's size 
                linearSystem.Subdomain.Forces = Vector.CreateZero(linearSystem.Size);
                linearSystem.RhsVector = linearSystem.Subdomain.Forces;
            }
            solver.BuildGlobalMatrix(new ElementStructuralStiffnessProvider());
            model.ApplyLoads();
            LoadingUtilities.ApplyNodalLoads(model, solver);
            solver.Solve();
        }

        private static IMidsideNodesSelection DefineMidsideNodes(Model model)
        {
            // Midside nodes
            double meshTol = 1E-6;
            var nodes = new List<Node>();
            nodes.Add(FindNode(4, 2, 4, model, meshTol));
            nodes.Add(FindNode(4, 6, 4, model, meshTol));
            nodes.Add(FindNode(2, 4, 4, model, meshTol));
            nodes.Add(FindNode(6, 4, 4, model, meshTol));
            nodes.Add(FindNode(4, 4, 2, model, meshTol));
            nodes.Add(FindNode(4, 4, 6, model, meshTol));

            // Midside nodes
            var midsideNodesPerSubdomain = new Dictionary<ISubdomain, HashSet<INode>>();
            foreach (Subdomain subdomain in model.SubdomainsDictionary.Values)
            {
                midsideNodesPerSubdomain[subdomain] = new HashSet<INode>();
            }
            foreach (Node node in nodes)
            {
                foreach (Subdomain subdomain in node.SubdomainsDictionary.Values)
                {
                    midsideNodesPerSubdomain[subdomain].Add(node);
                }
            }
            return new UserDefinedMidsideNodes(midsideNodesPerSubdomain,
                new IDofType[] { StructuralDof.TranslationX, StructuralDof.TranslationY, StructuralDof.TranslationZ });
        }

        private static Node FindNode(double x, double y, double z, Model model, double tol)
        {
            return model.NodesDictionary.Values.Where(
                n => (Math.Abs(n.X - x) <= tol) && (Math.Abs(n.Y - y) <= tol) && (Math.Abs(n.Z - z) <= tol)).First();
        }
    }
}
