using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.FEM.Entities;
using ISAAR.MSolve.LinearAlgebra.Iterative.Termination;
using ISAAR.MSolve.LinearAlgebra.Reordering;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Logging.DomainDecomposition;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.CornerNodes;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.StiffnessMatrices;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP3d;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP3d.Augmentation;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP3d.StiffnessMatrices;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.LagrangeMultipliers;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.Pcg;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.Preconditioning;
using ISAAR.MSolve.Solvers.Logging;
using Xunit;

namespace ISAAR.MSolve.Solvers.Tests.DomainDecomposition.Dual.FetiDP3d.IntegrationTests
{
    public static class Rve3DFetiDP3dTests
    {
        public enum Augmentation { None, Midpoints }
        public enum BoundaryConditions { Cantilever, RVE }
        public enum Crosspoints { Minimum, FullyRedundant }
        public enum Mesh { e4s2, e8s2 }

        [Theory]
        //[InlineData(Mesh.e4s2, BoundaryConditions.Cantilever, Crosspoints.FullyRedundant, Augmentation.None)]
        //[InlineData(Mesh.e4s2, BoundaryConditions.Cantilever, Crosspoints.FullyRedundant, Augmentation.Midpoints)]
        //[InlineData(Mesh.e4s2, BoundaryConditions.Cantilever, Crosspoints.Minimum, Augmentation.None)]
        //[InlineData(Mesh.e4s2, BoundaryConditions.Cantilever, Crosspoints.Minimum, Augmentation.Midpoints)]
        //[InlineData(Mesh.e8s2, BoundaryConditions.Cantilever, Crosspoints.FullyRedundant, Augmentation.None)]
        //[InlineData(Mesh.e8s2, BoundaryConditions.Cantilever, Crosspoints.FullyRedundant, Augmentation.Midpoints)]
        //[InlineData(Mesh.e8s2, BoundaryConditions.Cantilever, Crosspoints.Minimum, Augmentation.None)]
        //[InlineData(Mesh.e8s2, BoundaryConditions.Cantilever, Crosspoints.Minimum, Augmentation.Midpoints)]
        [InlineData(Mesh.e8s2, BoundaryConditions.RVE, Crosspoints.FullyRedundant, Augmentation.None)]
        [InlineData(Mesh.e8s2, BoundaryConditions.RVE, Crosspoints.FullyRedundant, Augmentation.Midpoints)]
        [InlineData(Mesh.e8s2, BoundaryConditions.RVE, Crosspoints.Minimum, Augmentation.None)]
        [InlineData(Mesh.e8s2, BoundaryConditions.RVE, Crosspoints.Minimum, Augmentation.Midpoints)]
        public static void Run(Mesh mesh, BoundaryConditions bc, Crosspoints crosspoints, Augmentation augmentation)
        {
            double pcgConvergenceTol = 1E-5;
            IVectorView directDisplacements = Utilities.AnalyzeSingleSubdomainModel(CreateModel(mesh, bc));
            (IVectorView ddDisplacements, ISolverLogger logger) =
                SolveModelWithSubdomains(CreateModel(mesh, bc), crosspoints, augmentation, pcgConvergenceTol);
            double normalizedError = directDisplacements.Subtract(ddDisplacements).Norm2() / directDisplacements.Norm2();

            // The error is provided in the reference solution the, but it is almost impossible for two different codes run on 
            // different machines to achieve the exact same accuracy.
            Assert.Equal(0.0, normalizedError, 4);
        }

        internal static Model CreateModel(Mesh mesh, BoundaryConditions bc)
        {
            double E0 = 2.1E0;
            double totalLoad = -10000;

            var builder = new Uniform3DModelBuilder();
            builder.DomainLengthX = 8.0;
            builder.DomainLengthY = 8.0;
            builder.DomainLengthZ = 8.0;
            builder.YoungModulus = E0;
            //builder.YoungModuliOfSubdomains = new double[,] { { E1, E0, E0, E0 }, { E1, E0, E0, E0 } };

            if (mesh == Mesh.e4s2)
            {
                builder.NumTotalElementsX = 4;
                builder.NumTotalElementsY = 4;
                builder.NumTotalElementsZ = 4;
                builder.NumSubdomainsX = 2;
                builder.NumSubdomainsY = 2;
                builder.NumSubdomainsZ = 2;
            }
            if (mesh == Mesh.e8s2)
            {
                builder.NumTotalElementsX = 8;
                builder.NumTotalElementsY = 8;
                builder.NumTotalElementsZ = 8;
                builder.NumSubdomainsX = 2;
                builder.NumSubdomainsY = 2;
                builder.NumSubdomainsZ = 2;
            }
            else throw new ArgumentException();
            

            if (bc == BoundaryConditions.Cantilever)
            {
                builder.PrescribeDisplacement(Uniform3DModelBuilder.BoundaryRegion.MinX, StructuralDof.TranslationX, 0.0);
                builder.PrescribeDisplacement(Uniform3DModelBuilder.BoundaryRegion.MinX, StructuralDof.TranslationY, 0.0);
                builder.PrescribeDisplacement(Uniform3DModelBuilder.BoundaryRegion.MinX, StructuralDof.TranslationZ, 0.0);

                builder.DistributeLoadAtNodes(Uniform3DModelBuilder.BoundaryRegion.MaxX, StructuralDof.TranslationY, totalLoad);
            }
            else if (bc == BoundaryConditions.RVE)
            {
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


                builder.DistributeLoadAtNodes(Uniform3DModelBuilder.BoundaryRegion.Centroid, StructuralDof.TranslationY, totalLoad);
                #endregion
            }
            else throw new ArgumentException();

            return builder.BuildModel();
        }

        private static (IVectorView globalDisplacements, ISolverLogger logger) SolveModelWithSubdomains(Model model, 
            Crosspoints crosspoints, Augmentation augmentation, double pcgConvergenceTolerance)
        {
            model.ConnectDataStructures();

            // Corner, midside nodes
            ICornerNodeSelection cornerNodeSelection = DefineCornerNodes(model);
            IMidsideNodesSelection midsideNodesSelection = DefineMidsideNodes(model);

            // Plot for debugging
            string path = @"C:\Users\Serafeim\Desktop\FETI-DP\Plots";
            var logger = new DomainDecompositionLoggerFetiDP(path, cornerNodeSelection, midsideNodesSelection, true);
            logger.PlotSubdomains(model);

            // Crosspoint strategy
            ICrosspointStrategy crosspointStrategy = null;
            if (crosspoints == Crosspoints.FullyRedundant) crosspointStrategy = new FullyRedundantConstraints();
            else if (crosspoints == Crosspoints.Minimum) crosspointStrategy = new MinimumConstraints();
            else throw new ArgumentException();

            // Specify PCG settings
            var pcgSettings = new PcgSettings()
            {
                ConvergenceTolerance = pcgConvergenceTolerance,
                MaxIterationsProvider = new FixedMaxIterationsProvider(100)
            };

            // Solver
            if (augmentation == Augmentation.None)
            {
                var fetiMatrices = new FetiDPMatrixManagerFactorySkyline(new OrderingAmdSuiteSparse());
                var solverBuilder = new FetiDPSolverSerial.Builder(fetiMatrices);
                solverBuilder.ProblemIsHomogeneous = true;
                solverBuilder.Preconditioning = new DirichletPreconditioning();
                solverBuilder.CrosspointStrategy = crosspointStrategy;
                solverBuilder.PcgSettings = pcgSettings;
                FetiDPSolverSerial fetiSolver = solverBuilder.Build(model, cornerNodeSelection);

                // Run the analysis
                Utilities.AnalyzeMultiSubdomainModel(model, fetiSolver);

                // Gather the global displacements
                Vector globalDisplacements = fetiSolver.GatherGlobalDisplacements();
                return (globalDisplacements, fetiSolver.Logger);
            }
            else
            {
                var fetiMatrices = new FetiDP3dMatrixManagerFactoryDense();
                var solverBuilder = new FetiDP3dSolverSerial.Builder(fetiMatrices);
                solverBuilder.ProblemIsHomogeneous = true;
                solverBuilder.Preconditioning = new DirichletPreconditioning();
                solverBuilder.CrosspointStrategy = crosspointStrategy;
                solverBuilder.PcgSettings = pcgSettings;
                FetiDP3dSolverSerial fetiSolver = solverBuilder.Build(model, cornerNodeSelection, midsideNodesSelection);

                // Run the analysis
                Utilities.AnalyzeMultiSubdomainModel(model, fetiSolver);

                // Gather the global displacements
                Vector globalDisplacements = fetiSolver.GatherGlobalDisplacements();
                return (globalDisplacements, fetiSolver.Logger);
            }
        }

        private static ICornerNodeSelection DefineCornerNodes(Model model)
        {
            double meshTol = 1E-6;
            var cornerNodesOfEachSubdomain = new Dictionary<ISubdomain, HashSet<INode>>();
            foreach (Subdomain subdomain in model.SubdomainsDictionary.Values)
            {
                subdomain.DefineNodesFromElements(); //TODO: This will also be called by the analyzer.
                INode[] midsides = CornerNodeUtilities.FindCornersOfBrick3D(subdomain);
                var midsideNodes = new HashSet<INode>();
                foreach (INode node in midsides)
                {
                    if (node.Constraints.Count > 0) continue;
                    //if ((Math.Abs(node.X - domainLengthX) <= meshTol) && (Math.Abs(node.Y) <= meshTol)) continue;
                    //if ((Math.Abs(node.X - domainLengthX) <= meshTol) && (Math.Abs(node.Y - domainLengthY) <= meshTol)) continue;
                    midsideNodes.Add(node);
                }
                cornerNodesOfEachSubdomain[subdomain] = midsideNodes;
            }
            return new UsedDefinedCornerNodes(cornerNodesOfEachSubdomain);
        }

        private static IMidsideNodesSelection DefineMidsideNodes(Model model)
        {
            double meshTol = 1E-6;
            var midsideNodesOfEachSubdomain = new Dictionary<ISubdomain, HashSet<INode>>();
            foreach (Subdomain subdomain in model.SubdomainsDictionary.Values)
            {
                //subdomain.DefineNodesFromElements(); //TODO: This will also be called by the analyzer or corners.
                INode[] midsides = MidsideNodesUtilities.FindMidsidesOfBrick3D(subdomain, meshTol);
                var midsideNodes = new HashSet<INode>();
                foreach (INode node in midsides)
                {
                    if ((node.Constraints.Count == 0) && (node.SubdomainsDictionary.Count > 2)) midsideNodes.Add(node);
                }
                midsideNodesOfEachSubdomain[subdomain] = midsideNodes;
            }

            return new UserDefinedMidsideNodes(midsideNodesOfEachSubdomain,
                new IDofType[] { StructuralDof.TranslationX, StructuralDof.TranslationY, StructuralDof.TranslationZ });
        }

        private static IMidsideNodesSelection DefineMidsideNodesHardCoded(Model model)
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
