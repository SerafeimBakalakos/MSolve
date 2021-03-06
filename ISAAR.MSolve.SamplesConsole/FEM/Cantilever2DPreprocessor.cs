﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.FEM.Elements;
using ISAAR.MSolve.FEM.Entities;
using ISAAR.MSolve.Geometry.Shapes;
using ISAAR.MSolve.Logging.VTK;
using ISAAR.MSolve.Materials;
using ISAAR.MSolve.Preprocessor.Meshes;
using ISAAR.MSolve.Preprocessor.Meshes.Custom;
using ISAAR.MSolve.Preprocessor.Meshes.GMSH;
using ISAAR.MSolve.Preprocessor.UI;

namespace ISAAR.MSolve.SamplesConsole.FEM
{
    /// <summary>
    /// A 2D cantilever beam modeled with continuum finite elements.
    /// Authors: Serafeim Bakalakos
    /// </summary>
    public class Cantilever2DPreprocessor
    {
        private const double length = 4.0;
        private const double height = 20.0;
        private const double thickness = 0.1;
        private const double youngModulus = 2E6;
        private const double poissonRatio = 0.3;
        private const double density = 78.5;
        private const double horizontalLoad = 1000.0; // TODO: this should be triangular

        private const string workingDirectory = @"C:\Users\Serafeim\Desktop\Presentation";
        private static readonly string projectDirectory =
            Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName + @"\Resources\GMSH";

        public static void Run()
        {
            bool dynamic = true;

            /// Choose one of the mesh files bundled with the project
            //string meshPath = projectDirectory + "\\cantilever_quad4.msh";
            //string meshPath = projectDirectory + "\\cantilever_quad8.msh";
            //string meshPath = projectDirectory + "\\cantilever_quad9.msh";
            //string meshPath = projectDirectory + "\\cantilever_tri3.msh";
            string meshPath = projectDirectory + "\\cantilever_tri6.msh";

            /// Or set a path on your machine
            //string meshPath = @"C:\Users\Serafeim\Desktop\Presentation\cantilever.msh";

            (IReadOnlyList<Node2D> nodes, IReadOnlyList<CellConnectivity2D> elements) = GenerateMeshFromGmsh(meshPath);
            //(IReadOnlyList<Node2D> nodes, IReadOnlyList<CellConnectivity2D> elements) = GenerateUniformMesh();
            //(IReadOnlyList<Node2D> nodes, IReadOnlyList<CellConnectivity2D> elements) = GenerateMeshManually();

            PreprocessorModel model = CreateModel(nodes, elements);
            if (dynamic) ApplyLoadsDynamic(model);
            else ApplyLoadsStatic(model);
            OutputRequests output = DefineOutput();
            Solve(model, output, dynamic);
        }

        private static void ApplyLoadsStatic(PreprocessorModel model)
        {
            // Only upper left corner
            double tol = 1E-10;
            model.ApplyNodalLoads(
                node => (Math.Abs(node.Y - height) <= tol) && ((Math.Abs(node.X) <= tol)), 
                DOFType.X, horizontalLoad);
        }

        private static void ApplyLoadsDynamic(PreprocessorModel model)
        {
            string accelerogramPath = workingDirectory + "\\elcentro_NS.dat";
            Dictionary<DOFType, double> magnifications = new Dictionary<DOFType, double>
            {
                { DOFType.X, 1.0 }
            };
            model.SetGroundMotion(accelerogramPath, magnifications, 0.02, 53.74);
        }

        private static PreprocessorModel CreateModel(IReadOnlyList<Node2D> nodes, IReadOnlyList<CellConnectivity2D> elements)
        {
            PreprocessorModel model = PreprocessorModel.Create2DPlaneStress(thickness);

            // Materials properties
            ElasticMaterial2D material = new ElasticMaterial2D(StressState2D.PlaneStress)
            {
                YoungModulus = youngModulus,
                PoissonRatio = poissonRatio
            };
            DynamicMaterial dynamicProperties = new DynamicMaterial(density, 0.05, 0.05);

            // Mesh
            model.AddMesh2D(nodes, elements, material, dynamicProperties);

            // Prescribed displacements: all nodes at the bottom
            double tol = 1E-10;
            IEnumerable<Node2D> constrainedNodes = nodes.Where(node => Math.Abs(node.Y) <= tol);
            model.ApplyPrescribedDisplacements(constrainedNodes, DOFType.X, 0.0);
            model.ApplyPrescribedDisplacements(constrainedNodes, DOFType.Y, 0.0);



            return model;
        }

        private static OutputRequests DefineOutput()
        {
            OutputRequests output = new OutputRequests(workingDirectory + "\\Plots");
            output.Displacements = true;
            output.Strains = true;
            output.Stresses = true;
            output.StressesVonMises = true;

            return output;
        }

        private static (IReadOnlyList<Node2D> nodes, IReadOnlyList<CellConnectivity2D> elements) GenerateMeshManually()
        {
            Node2D[] nodes =
            {
                new Node2D(0, 0.0, 0.0),
                new Node2D(1, length, 0.0),
                new Node2D(2, 0.0, 0.25 * height),
                new Node2D(3, length, 0.25 * height),
                new Node2D(4, 0.0, 0.50 * height),
                new Node2D(5, length, 0.50 * height),
                new Node2D(6, 0.0, 0.75 * height),
                new Node2D(7, length, 0.75 * height),
                new Node2D(8, 0.0, height),
                new Node2D(9, length, height)
            };

            CellType2D[] cellTypes = { CellType2D.Quad4, CellType2D.Quad4, CellType2D.Quad4, CellType2D.Quad4 };

            CellConnectivity2D[] elements =
            {
                new CellConnectivity2D(CellType2D.Quad4, new Node2D[] { nodes[0], nodes[1], nodes[3], nodes[2]}),
                new CellConnectivity2D(CellType2D.Quad4, new Node2D[] { nodes[2], nodes[3], nodes[5], nodes[4]}),
                new CellConnectivity2D(CellType2D.Quad4, new Node2D[] { nodes[4], nodes[5], nodes[7], nodes[6]}),
                new CellConnectivity2D(CellType2D.Quad4, new Node2D[] { nodes[6], nodes[7], nodes[9], nodes[8]})
            };

            return (nodes, elements);
        }

        private static (IReadOnlyList<Node2D> nodes, IReadOnlyList<CellConnectivity2D> elements) GenerateMeshFromGmsh(string path)
        {
            using (var reader = new GmshReader2D(path))
            {
                return reader.CreateMesh();
            }
        }

        private static (IReadOnlyList<Node2D> nodes, IReadOnlyList<CellConnectivity2D> elements) GenerateUniformMesh()
        {
            var meshGen = new UniformMeshGenerator(0.0, 0.0, length, height, 4, 20);
            return meshGen.CreateMesh();
        }

        private static void PrintMeshOnly(Model model)
        {
            var mesh = new VtkMesh2D(model);
            using (var writer = new VtkFileWriter(workingDirectory + "\\mesh.vtk"))
            {
                writer.WriteMesh(mesh.Points, mesh.Cells);
            }
        }

        private static void Solve(PreprocessorModel model, OutputRequests output, bool dynamic)
        {
            // Set up the simulation procedure
            Job job = new Job(model);
            if (dynamic) job.Procedure = Job.ProcedureOptions.DynamicImplicit;
            else job.Procedure = Job.ProcedureOptions.Static;
            job.Integrator = Job.IntegratorOptions.Linear;
            job.Solver = Job.SolverOptions.DirectSkyline;
            job.FieldOutputRequests = output;

            // Run the simulation
            job.Submit();
        }
    }
}