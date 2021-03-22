using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Output;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Materials;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Enrichment.Enrichers;
using MGroup.XFEM.Entities;
using MGroup.XFEM.Geometry.Primitives;
using MGroup.XFEM.Materials;
using MGroup.XFEM.Output;
using MGroup.XFEM.Output.Writers;
using MGroup.XFEM.Phases;
using MGroup.XFEM.Tests.Utilities;
using Xunit;

//TODO: Is the intagration order enough for ridge enrichment?
namespace MGroup.XFEM.Tests.MultiphaseStructural
{
    public class CoherentInclusions3DTests
    {
        private static readonly string outputDirectory = Path.Combine(
            Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName, "Resources", "structural_coherent_3D_temp");
        private static readonly string expectedDirectory = Path.Combine(
            Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName, "Resources", "structural_coherent_3D");

        private const int dim = 3;
        private static readonly double[] minCoords = { -1.0, -1.0, -1.0 };
        private static readonly double[] maxCoords = { +1.0, +1.0, +1.0 };
        private const double thickness = 1.0;
        private static readonly int[] numElements = { 19, 19, 19 };
        private const int bulkIntegrationOrder = 2, boundaryIntegrationOrder = 2;
        private static readonly int[] numBalls = { 2, 1, 1 };
        private const double ballRadius = 0.3;

        private const int defaultPhaseID = 0;

        private const double matrixE = 1, inclusionE = 2, v = 0.3;

        [Fact]
        public static void TestModel()
        {
            try
            {
                if (!Directory.Exists(outputDirectory))
                {
                    Directory.CreateDirectory(outputDirectory);
                }

                // Create model and LSM
                XModel<IXMultiphaseElement> model = CreateModel();
                model.FindConformingSubcells = true;
                PhaseGeometryModel geometryModel = CreatePhases(model);

                // Plot level sets
                geometryModel.GeometryObservers.Add(new PhaseLevelSetPlotter(outputDirectory, model, geometryModel));

                // Plot phases of nodes
                geometryModel.InteractionObservers.Add(new NodalPhasesPlotter(outputDirectory, model));

                // Plot element - phase boundaries interactions
                geometryModel.InteractionObservers.Add(new LsmElementIntersectionsPlotter(outputDirectory, model));

                // Plot element subcells
                model.ModelObservers.Add(new ConformingMeshPlotter(outputDirectory, model));

                // Plot phases of each element subcell
                model.ModelObservers.Add(new ElementPhasePlotter(outputDirectory, model, geometryModel, defaultPhaseID));

                // Write the size of each phase
                model.ModelObservers.Add(new PhasesSizeWriter(outputDirectory, model, geometryModel));

                // Plot bulk and boundary integration points of each element
                model.ModelObservers.Add(new IntegrationPointsPlotter(outputDirectory, model));

                // Plot enrichments
                double elementSize = (maxCoords[0] - minCoords[0]) / numElements[0];
                model.RegisterEnrichmentObserver(new PhaseEnrichmentPlotter(outputDirectory, model, elementSize, 2));

                // Initialize model state so that everything described above can be tracked
                model.Initialize();

                // Compare output
                var computedFiles = new List<string>();
                computedFiles.Add(Path.Combine(outputDirectory, "level_set1_t0.vtk"));
                computedFiles.Add(Path.Combine(outputDirectory, "level_set2_t0.vtk"));
                computedFiles.Add(Path.Combine(outputDirectory, "nodal_phases_t0.vtk"));
                computedFiles.Add(Path.Combine(outputDirectory, "intersections_t0.vtk"));
                computedFiles.Add(Path.Combine(outputDirectory, "conforming_mesh_t0.vtk"));
                computedFiles.Add(Path.Combine(outputDirectory, "element_phases_t0.vtk"));
                computedFiles.Add(Path.Combine(outputDirectory, "phase_sizes_t0.txt"));
                computedFiles.Add(Path.Combine(outputDirectory, "gauss_points_bulk_t0.vtk"));
                computedFiles.Add(Path.Combine(outputDirectory, "enriched_nodes_heaviside_t0.vtk"));

                var expectedFiles = new List<string>();
                expectedFiles.Add(Path.Combine(expectedDirectory, "level_set1_t0.vtk"));
                expectedFiles.Add(Path.Combine(expectedDirectory, "level_set2_t0.vtk"));
                expectedFiles.Add(Path.Combine(expectedDirectory, "nodal_phases_t0.vtk"));
                expectedFiles.Add(Path.Combine(expectedDirectory, "intersections_t0.vtk"));
                expectedFiles.Add(Path.Combine(expectedDirectory, "conforming_mesh_t0.vtk"));
                expectedFiles.Add(Path.Combine(expectedDirectory, "element_phases_t0.vtk"));
                expectedFiles.Add(Path.Combine(expectedDirectory, "phase_sizes_t0.txt"));
                expectedFiles.Add(Path.Combine(expectedDirectory, "gauss_points_bulk_t0.vtk"));
                expectedFiles.Add(Path.Combine(expectedDirectory, "enriched_nodes_heaviside_t0.vtk"));

                double tolerance = 1E-6;
                for (int i = 0; i < expectedFiles.Count; ++i)
                {
                    Assert.True(IOUtilities.AreDoubleValueFilesEquivalent(expectedFiles[i], computedFiles[i], tolerance));
                }
            }
            finally
            {
                if (Directory.Exists(outputDirectory))
                {
                    DirectoryInfo di = new DirectoryInfo(outputDirectory);
                    di.Delete(true);//true means delete subdirectories and files
                }
            }
        }

        [Fact]
        public static void TestSolution()
        {
            try
            {
                if (!Directory.Exists(outputDirectory))
                {
                    Directory.CreateDirectory(outputDirectory);
                }

                // Create model and LSM
                XModel<IXMultiphaseElement> model = CreateModel();
                model.FindConformingSubcells = true;
                PhaseGeometryModel geometryModel = CreatePhases(model);

                // Run analysis
                model.Initialize();
                IVectorView solution = Analysis.RunStructuralStaticAnalysis(model);

                // Plot displacements, strains, stresses
                var computedFiles = new List<string>();
                computedFiles.Add(Path.Combine(outputDirectory, "displacement_nodes_t0.vtk"));
                computedFiles.Add(Path.Combine(outputDirectory, "displacement_gauss_points_t0.vtk"));
                computedFiles.Add(Path.Combine(outputDirectory, "strains_gauss_points_t0.vtk"));
                computedFiles.Add(Path.Combine(outputDirectory, "stresses_gauss_points_t0.vtk"));
                computedFiles.Add(Path.Combine(outputDirectory, "displacement_strain_stress_field_t0.vtk"));
                //Utilities.Plotting.PlotDisplacements(model, solution, computedFiles[0], computedFiles[1]);
                Utilities.Plotting.PlotStrainsStressesAtGaussPoints(model, solution, computedFiles[2], computedFiles[3]);
                Utilities.Plotting.PlotDisplacementStrainStressFields(model, solution, computedFiles[4]);

                // Compare output
                var expectedFiles = new List<string>();
                //expectedFiles.Add(Path.Combine(expectedDirectory, "temperature_nodes_t0.vtk"));
                //expectedFiles.Add(Path.Combine(expectedDirectory, "temperature_gauss_points_t0.vtk"));
                //expectedFiles.Add(Path.Combine(expectedDirectory, "temperature_field_t0.vtk"));
                //expectedFiles.Add(Path.Combine(expectedDirectory, "heat_flux_gauss_points_t0.vtk"));

                double tolerance = 1E-6;
                for (int i = 0; i < expectedFiles.Count; ++i)
                {
                    Assert.True(IOUtilities.AreDoubleValueFilesEquivalent(expectedFiles[i], computedFiles[i], tolerance));
                }
            }
            finally
            {
                if (Directory.Exists(outputDirectory))
                {
                    DirectoryInfo di = new DirectoryInfo(outputDirectory);
                    di.Delete(true);//true means delete subdirectories and files
                }
            }
        }

        [Fact]
        public static void TestHomogenization()
        {
            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            // Create model and LSM
            XModel<IXMultiphaseElement> model = CreateModel();
            model.FindConformingSubcells = true;
            PhaseGeometryModel geometryModel = CreatePhases(model);

            // Run analysis
            model.Initialize();
            IMatrix elasticity = Analysis.RunHomogenizationAnalysisStructural3D(model, minCoords, maxCoords);

            // Print results
            var matrixWriter = new FullMatrixWriter();
            string pathResults = outputDirectory + "\\equivalent_elasticity.txt";
            using (var writer = new StreamWriter(pathResults, true))
            {
                writer.WriteLine();
                writer.WriteLine("#################################################################");
                writer.WriteLine("Date = " + DateTime.Now);
                writer.WriteLine("elasticity = ");
            }
            matrixWriter.WriteToFile(elasticity, pathResults, true);

            // Print the constitutive matrix of the matrix material for comparison
            using (var writer = new StreamWriter(pathResults, true))
            {
                writer.WriteLine();
                writer.WriteLine("elasticity of matrix = ");
            }
            var matrixMaterial = new ElasticMaterial3D() { YoungModulus = matrixE, PoissonRatio = v };
            matrixWriter.WriteToFile(matrixMaterial.ConstitutiveMatrix, pathResults, true);
        }

        private static XModel<IXMultiphaseElement> CreateModel()
        {
            // Materials
            var materialMatrix = new ElasticMaterial3D() { YoungModulus = matrixE, PoissonRatio = v };
            var materialInclusion = new ElasticMaterial3D() { YoungModulus = inclusionE, PoissonRatio = v };
            CohesiveInterfaceMaterial interfaceMaterial = null;
            var materialField = new MatrixInclusionsStructuralMaterialField(
                materialMatrix, materialInclusion, interfaceMaterial, 0);

            // Setup model
            XModel<IXMultiphaseElement> model = Models.CreateHexa8Model(minCoords, maxCoords, thickness, numElements,
                bulkIntegrationOrder, boundaryIntegrationOrder, materialField, false);
            Models.ApplyBCsCantileverTension(model);

            return model;
        }

        private static PhaseGeometryModel CreatePhases(XModel<IXMultiphaseElement> model)
        {
            List<ISurface3D> balls = Utilities.Phases.CreateBallsStructured3D(minCoords, maxCoords, numBalls, ballRadius, 1.0);
            PhaseGeometryModel geometryModel = Utilities.Phases.CreateLsmPhases3D(model, balls);
            geometryModel.Enricher = NodeEnricherMultiphaseNoJunctions.CreateStructuralRidge(geometryModel, dim);
            return geometryModel;
        }
    }
}
