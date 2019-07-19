using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.Discretization.Providers;
using ISAAR.MSolve.FEM.Entities;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Solvers.Assemblers;
using ISAAR.MSolve.Solvers.LinearSystems;
using ISAAR.MSolve.Solvers.Ordering;
using ISAAR.MSolve.Solvers.Ordering.Reordering;
using ISAAR.MSolve.Solvers.Tests.DomainDecomposition;
using MPI;

namespace ISAAR.MSolve.SamplesConsole.MPI
{
    public class ModelMPI
    {
        public static void RunSerial()
        {
            Model model = CreateModel();

            model.ConnectDataStructures();

            // Order dofs
            var dofOrderer = new DofOrderer(new NodeMajorDofOrderingStrategy(), new NullReordering());
            IGlobalFreeDofOrdering globalOrdering = dofOrderer.OrderFreeDofs(model);
            model.GlobalDofOrdering = globalOrdering;
            foreach (ISubdomain subdomain in model.Subdomains)
            {
                subdomain.FreeDofOrdering = globalOrdering.SubdomainDofOrderings[subdomain];
            }

            // Create linear systems
            var linearSystems = new List<SingleSubdomainSystem<SkylineMatrix>>();
            foreach (ISubdomain subdomain in model.Subdomains)
            {
                var ls = new SingleSubdomainSystem<SkylineMatrix>(subdomain);
                linearSystems.Add(ls);
                ls.Reset();
                subdomain.Forces = Vector.CreateZero(ls.Size);
            }

            // Create the stiffness matrices
            var provider = new ElementStructuralStiffnessProvider();
            foreach (ILinearSystem ls in linearSystems)
            {
                ISubdomain subdomain = ls.Subdomain;
                var assembler = new SkylineAssembler();
                SkylineMatrix stiffness = assembler.BuildGlobalMatrix(subdomain.FreeDofOrdering, subdomain.Elements, provider);
                ls.Matrix = stiffness;
            }

            // Print the trace of each stiffness matrix
            foreach (ILinearSystem ls in linearSystems)
            {
                double trace = Trace(ls.Matrix);
                Console.WriteLine($"(serial code) Subdomain {ls.Subdomain.ID}: trace(stiffnessMatrix) = {trace}");
            }
        }

        public static void RunParallel(string[] args)
        {

        }

        private static Model CreateModel()
        {
            double E = 2.1E-7;
            var builder = new Uniform2DModelBuilder();
            builder.DomainLengthX = 2.0;
            builder.DomainLengthY = 2.0;
            builder.NumSubdomainsX = 2;
            builder.NumSubdomainsY = 2;
            builder.NumTotalElementsX = 8;
            builder.NumTotalElementsY = 8;
            builder.YoungModuliOfSubdomains = new double[,] { { E, 1.25 * E }, { 1.5 * E, 1.75 * E } };
            //builder.YoungModulus = E;
            builder.PrescribeDisplacement(Uniform2DModelBuilder.BoundaryRegion.LowerLeftCorner, StructuralDof.TranslationX, 0.0);
            builder.PrescribeDisplacement(Uniform2DModelBuilder.BoundaryRegion.LowerLeftCorner, StructuralDof.TranslationY, 0.0);
            builder.PrescribeDisplacement(Uniform2DModelBuilder.BoundaryRegion.LowerRightCorner, StructuralDof.TranslationY, 0.0);
            builder.DistributeLoadAtNodes(Uniform2DModelBuilder.BoundaryRegion.RightSide, StructuralDof.TranslationX, 100.0);

            return builder.BuildModel();
        }

        private static double Trace(IMatrixView matrix)
        {
            double trace = 0.0;
            for (int i = 0; i < matrix.NumRows; ++i) trace += matrix[i, i];
            return trace;
        }
    }
}
