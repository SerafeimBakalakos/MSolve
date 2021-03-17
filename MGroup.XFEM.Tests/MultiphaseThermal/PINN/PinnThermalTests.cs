using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Enrichment.Enrichers;
using MGroup.XFEM.Enrichment.SingularityResolution;
using MGroup.XFEM.Entities;
using MGroup.XFEM.Geometry.LSM;
using MGroup.XFEM.Geometry.Primitives;
using MGroup.XFEM.Materials;
using MGroup.XFEM.Phases;
using MGroup.XFEM.Output;
using MGroup.XFEM.Output.Writers;
using MGroup.XFEM.Tests.Utilities;
using Xunit;
using ISAAR.MSolve.LinearAlgebra.Matrices;

namespace MGroup.XFEM.Tests.MultiphaseThermal.PINN
{
    public static class PinnThermalTests
    {
        private static readonly string outputDirectory = @"C:\Users\Serafeim\Desktop\HEAT\PINN\thermal_example_3d";

        private static readonly double[] minCoords = { 0.0, 0.0, 0.0 };
        private static readonly double[] maxCoords = { 1E0, 1E0, 1E0 }; //μm
        private static readonly int[] numElements = { 35, 35, 35 };
        private const int bulkIntegrationOrder = 2, boundaryIntegrationOrder = 2;
        
        private const double ballRadius = 5E-2; // μm
        private static readonly int[] numBalls = { 7, 7, 7 };

        private const int defaultPhaseID = 0;

        private const bool cohesiveInterfaces = true;
        private const double conductMatrix = 0.25, conductInclusion = 429; //Wμ/K
        private const double conductBoundaryMatrixInclusion = 1E15, conductBoundaryInclusionInclusion = 0;
        private const double specialHeatCoeff = 1.0;

        //[Fact]
        public static void TestModel()
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
            //geometryModel.GeometryObservers.Add(new PhaseLevelSetPlotter(outputDirectory, model, geometryModel));

            // Plot phases of nodes
            //geometryModel.InteractionObservers.Add(new NodalPhasesPlotter(outputDirectory, model));

            // Plot element - phase boundaries interactions
            geometryModel.InteractionObservers.Add(new LsmElementIntersectionsPlotter(outputDirectory, model));

            // Plot element subcells
            model.ModelObservers.Add(new ConformingMeshPlotter(outputDirectory, model));

            // Plot phases of each element subcell
            model.ModelObservers.Add(new ElementPhasePlotter(outputDirectory, model, geometryModel, defaultPhaseID));

            // Write the size of each phase
            //model.ModelObservers.Add(new PhasesSizeWriter(outputDirectory, model, geometryModel));

            // Plot bulk and boundary integration points of each element
            //model.ModelObservers.Add(new IntegrationPointsPlotter(outputDirectory, model));

            // Plot enrichments
            //double elementSize = (maxCoords[0] - minCoords[0]) / numElements[0];
            //model.RegisterEnrichmentObserver(new PhaseEnrichmentPlotter(outputDirectory, model, elementSize, 3));

            // Initialize model state so that everything described above can be tracked
            model.Initialize();
        }

        //[Fact]
        public static void TestSolution()
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
            IVectorView solution = Analysis.RunThermalStaticAnalysis(model);

            // Plot temperature and heat flux
            var computedFiles = new List<string>();
            computedFiles.Add(Path.Combine(outputDirectory, "temperature_nodes_t0.vtk"));
            computedFiles.Add(Path.Combine(outputDirectory, "temperature_gauss_points_t0.vtk"));
            computedFiles.Add(Path.Combine(outputDirectory, "temperature_field_t0.vtk"));
            computedFiles.Add(Path.Combine(outputDirectory, "heat_flux_gauss_points_t0.vtk"));
            Utilities.Plotting.PlotTemperatureAndHeatFlux(model, solution,
                computedFiles[0], computedFiles[1], computedFiles[2], computedFiles[3]);
        }

        //[Fact]
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

            // Run homogenization analysis
            model.Initialize();
            IMatrix conductivity = Analysis.RunHomogenizationAnalysis3D(model, minCoords, maxCoords);

            // Print results
            string pathResults = outputDirectory + "\\equiv_cond.txt";
            using (var writer = new StreamWriter(pathResults, true))
            {
                writer.WriteLine();
                writer.WriteLine("#################################################################");
                writer.WriteLine("Date = " + DateTime.Now);
                writer.WriteLine(
                    $"conductivity = [ {conductivity[0, 0]} {conductivity[0, 1]} {conductivity[0, 2]};" +
                    $" {conductivity[1, 0]} {conductivity[1, 1]} {conductivity[1, 2]};" +
                    $" {conductivity[2, 0]} {conductivity[2, 1]} {conductivity[2, 2]}]");
            }
        }

        private static XModel<IXMultiphaseElement> CreateModel()
        {
            // Materials
            var matrixMaterial = new ThermalMaterial(conductMatrix, specialHeatCoeff);
            var inclusionMaterial = new ThermalMaterial(conductInclusion, specialHeatCoeff);
            var materialField = new MatrixInclusionsThermalMaterialField(matrixMaterial, inclusionMaterial,
                conductBoundaryMatrixInclusion, conductBoundaryInclusionInclusion, defaultPhaseID);

            var model = Models.CreateHexa8Model(minCoords, maxCoords, numElements,
                bulkIntegrationOrder, boundaryIntegrationOrder, materialField, cohesiveInterfaces);
            Models.ApplyBCsTemperatureDiffAlongX(model, 0, 1);
            return model;
        }

        private static PhaseGeometryModel CreatePhases(XModel<IXMultiphaseElement> model)
        {
            var geometryPreprocessor = new GeometryPreprocessor3D(model);
            geometryPreprocessor.MinCoords = minCoords;
            geometryPreprocessor.MaxCoords = maxCoords;
            geometryPreprocessor.BallRadius = ballRadius;
            geometryPreprocessor.NumBalls = numBalls;
            geometryPreprocessor.GeneratePhases(model);

            PhaseGeometryModel geometricModel = geometryPreprocessor.GeometryModel;
            model.GeometryModel = geometricModel;
            geometricModel.Enricher = new NodeEnricherMultiphaseNoJunctions(geometricModel);
            return geometricModel;
        }
    }
}
