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
using MGroup.XFEM.Plotting;
using MGroup.XFEM.Plotting.Fields;
using MGroup.XFEM.Plotting.Mesh;
using MGroup.XFEM.Plotting.Writers;
using MGroup.XFEM.Tests.Utilities;
using Xunit;

//MODIFICATION NEEDED: enrichments, temperature field, temperature at nodes & GPs, heat flux at GPs
namespace MGroup.XFEM.Tests.Multiphase
{
    public static class LsmBalls2DTests
    {
        private static readonly string directory = Path.Combine(
            Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName, "Resources", "lsm_balls_2D_temp"); 

        private static readonly double[] minCoords = { -1.0, -1.0 };
        private static readonly double[] maxCoords = { +1.0, +1.0 };
        private const double thickness = 1.0;
        private static readonly int[] numElements = { 15, 15 };
        private const int bulkIntegrationOrder = 2, boundaryIntegrationOrder = 2;
        private const int numBallsX = 2, numBallsY = 1;
        private const double ballRadius = 0.3;

        private const int defaultPhaseID = 0;

        private const double conductMatrix = 1E0, conductInclusion = 1E5;
        private const double conductBoundaryMatrixInclusion = 1E1, conductBoundaryInclusionInclusion = 1E2;
        private const double specialHeatCoeff = 1.0;

        [Fact]
        public static void TestModel2D()
        {
            try
            {
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Create model and LSM
                XModel<IXMultiphaseElement> model = CreateModel();
                model.FindConformingSubcells = true;
                PhaseGeometryModel geometryModel = CreatePhases(model);

                // Plot level sets
                geometryModel.Observers.Add(new PhaseLevelSetPlotter(directory, model, geometryModel));

                // Plot phases of nodes
                geometryModel.Observers.Add(new NodalPhasesPlotter(directory, model));

                // Plot element - phase boundaries interactions
                geometryModel.Observers.Add(new LsmElementIntersectionsPlotter(directory, model));

                // Plot element subcells
                model.ModelObservers.Add(new ConformingMeshPlotter(directory, model));

                // Plot phases of each element subcell
                model.ModelObservers.Add(new ElementPhasePlotter(directory, model, geometryModel, defaultPhaseID));

                // Write the size of each phase
                model.ModelObservers.Add(new PhasesSizeWriter(directory, geometryModel));

                // Plot bulk and boundary integration points of each element
                model.ModelObservers.Add(new IntegrationPointsPlotter(directory, model));

                // Plot enrichments
                double elementSize = (maxCoords[0] - minCoords[0]) / numElements[0];
                model.RegisterEnrichmentObserver(new PhaseEnrichmentPlotter(directory, model, elementSize, 2));

                // Initialize model state so that everything described above can be tracked
                model.Initialize();

                // Compare output
                var computedFiles = new List<string>();
                computedFiles.Add(Path.Combine(directory, "level_set1_t0.vtk"));
                computedFiles.Add(Path.Combine(directory, "level_set2_t0.vtk"));
                computedFiles.Add(Path.Combine(directory, "nodal_phases_t0.vtk"));
                computedFiles.Add(Path.Combine(directory, "intersections_t0.vtk"));
                computedFiles.Add(Path.Combine(directory, "conforming_mesh_t0.vtk"));
                computedFiles.Add(Path.Combine(directory, "element_phases_t0.vtk"));
                computedFiles.Add(Path.Combine(directory, "phase_sizes_t0.txt"));
                computedFiles.Add(Path.Combine(directory, "gauss_points_bulk_t0.vtk"));
                computedFiles.Add(Path.Combine(directory, "gauss_points_boundary_t0.vtk"));
                computedFiles.Add(Path.Combine(directory, "enriched_nodes_heaviside_t0.vtk"));

                string expectedDirectory = Path.Combine(Directory.GetParent(
                    Directory.GetCurrentDirectory()).Parent.Parent.FullName, "Resources", "lsm_balls_2D");
                var expectedFiles = new List<string>();
                expectedFiles.Add(Path.Combine(expectedDirectory, "level_set1_t0.vtk"));
                expectedFiles.Add(Path.Combine(expectedDirectory, "level_set2_t0.vtk"));
                expectedFiles.Add(Path.Combine(expectedDirectory, "nodal_phases_t0.vtk"));
                expectedFiles.Add(Path.Combine(expectedDirectory, "intersections_t0.vtk"));
                expectedFiles.Add(Path.Combine(expectedDirectory, "conforming_mesh_t0.vtk"));
                expectedFiles.Add(Path.Combine(expectedDirectory, "element_phases_t0.vtk"));
                expectedFiles.Add(Path.Combine(expectedDirectory, "phase_sizes_t0.txt"));
                expectedFiles.Add(Path.Combine(expectedDirectory, "gauss_points_bulk_t0.vtk"));
                expectedFiles.Add(Path.Combine(expectedDirectory, "gauss_points_boundary_t0.vtk"));
                expectedFiles.Add(Path.Combine(expectedDirectory, "enriched_nodes_heaviside_t0.vtk"));

                for (int i = 0; i < expectedFiles.Count; ++i)
                {
                    Assert.True(IOUtilities.AreFilesEquivalent(expectedFiles[i], computedFiles[i]));
                }
            }
            finally
            {
                if (Directory.Exists(directory))
                {
                    DirectoryInfo di = new DirectoryInfo(directory);
                    di.Delete(true);//true means delete subdirectories and files
                }
            }
        }

        [Fact]
        public static void TestSolution2D()
        {
            try
            {
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Create model and LSM
                XModel<IXMultiphaseElement> model = CreateModel();
                model.FindConformingSubcells = true;
                PhaseGeometryModel geometryModel = CreatePhases(model);

                // Run analysis
                model.Initialize();
                IVectorView solution = Analysis.RunStaticAnalysis(model);

                // Plot temperature and heat flux
                PlotTemperatureAndHeatFlux(directory, model, solution);

                // Compare output
                var computedFiles = new List<string>();
                computedFiles.Add(Path.Combine(directory, "temperature_nodes.vtk"));
                computedFiles.Add(Path.Combine(directory, "temperature_gauss_points.vtk"));
                computedFiles.Add(Path.Combine(directory, "temperature_field.vtk"));
                computedFiles.Add(Path.Combine(directory, "heat_flux_gauss_points.vtk"));

                string expectedDirectory = Path.Combine(Directory.GetParent(
                    Directory.GetCurrentDirectory()).Parent.Parent.FullName, "Resources", "lsm_balls_2D");
                var expectedFiles = new List<string>();
                expectedFiles.Add(Path.Combine(expectedDirectory, "temperature_nodes.vtk"));
                expectedFiles.Add(Path.Combine(expectedDirectory, "temperature_gauss_points.vtk"));
                expectedFiles.Add(Path.Combine(expectedDirectory, "temperature_field.vtk"));
                expectedFiles.Add(Path.Combine(expectedDirectory, "heat_flux_gauss_points.vtk"));

                for (int i = 0; i < expectedFiles.Count; ++i)
                {
                    Assert.True(IOUtilities.AreFilesEquivalent(expectedFiles[i], computedFiles[i]));
                }
            }
            finally
            {
                if (Directory.Exists(directory))
                {
                    DirectoryInfo di = new DirectoryInfo(directory);
                    di.Delete(true);//true means delete subdirectories and files
                }
            }
        }

        private static XModel<IXMultiphaseElement> CreateModel()
        {
            // Materials
            var matrixMaterial = new ThermalMaterial(conductMatrix, specialHeatCoeff);
            var inclusionMaterial = new ThermalMaterial(conductInclusion, specialHeatCoeff);
            var interfaceMaterial = new ThermalInterfaceMaterial(conductBoundaryMatrixInclusion);
            var materialField = new MatrixInclusionsMaterialField(matrixMaterial, inclusionMaterial,
                conductBoundaryMatrixInclusion, conductBoundaryInclusionInclusion, defaultPhaseID);

            return Models.CreateQuad4Model(minCoords, maxCoords, thickness, numElements,
                bulkIntegrationOrder, boundaryIntegrationOrder, materialField);
        }

        private static PhaseGeometryModel CreatePhases(XModel<IXMultiphaseElement> model)
        {
            var geometricModel = new PhaseGeometryModel(model);
            model.GeometryModel = geometricModel;
            geometricModel.Enricher = new NodeEnricherMultiphase(geometricModel, new NullSingularityResolver());
            List<SimpleLsm2D> lsmCurves = InitializeLSM(model);
            var defaultPhase = new DefaultPhase(defaultPhaseID);
            geometricModel.Phases[defaultPhase.ID] = defaultPhase;
            for (int p = 0; p < lsmCurves.Count; ++p)
            {
                SimpleLsm2D curve = lsmCurves[p];
                var phase = new LsmPhase(p + 1, geometricModel, -1);
                geometricModel.Phases[phase.ID] = phase;

                var boundary = new ClosedLsmPhaseBoundary(phase.ID, curve, defaultPhase, phase);
                defaultPhase.ExternalBoundaries.Add(boundary);
                defaultPhase.Neighbors.Add(phase);
                phase.ExternalBoundaries.Add(boundary);
                phase.Neighbors.Add(defaultPhase);
                geometricModel.PhaseBoundaries[boundary.ID] = boundary;
            }
            return geometricModel;
        }

        private static List<SimpleLsm2D> InitializeLSM(XModel<IXMultiphaseElement> model)
        {
            double xMin = minCoords[0], xMax = maxCoords[0], yMin = minCoords[1], yMax = maxCoords[1];
            var curves = new List<SimpleLsm2D>(numBallsX * numBallsY);
            double dx = (xMax - xMin) / (numBallsX + 1);
            double dy = (yMax - yMin) / (numBallsY + 1);
            int id = 1;
            for (int i = 0; i < numBallsX; ++i)
            {
                double centerX = xMin + (i + 1) * dx;
                for (int j = 0; j < numBallsY; ++j)
                {
                    double centerY = yMin + (j + 1) * dy;
                    var circle = new Circle2D(centerX, centerY, ballRadius);
                    var lsm = new SimpleLsm2D(id++, model.XNodes, circle);
                    curves.Add(lsm);
                }
            }

            return curves;
        }

        private static void PlotTemperatureAndHeatFlux(string outputDirectory, XModel<IXMultiphaseElement> model,
            IVectorView solution)
        {
            // Temperature at nodes
            string path = Path.Combine(outputDirectory, "temperature_nodes.vtk");
            using (var writer = new VtkPointWriter(path))
            {
                var temperatureField = new TemperatureAtNodesField(model);
                writer.WriteScalarField("temperature", temperatureField.CalcValuesAtVertices(solution));
            }

            // Temperature at Gauss Points
            path = Path.Combine(outputDirectory, "temperature_gauss_points.vtk");
            using (var writer = new VtkPointWriter(path))
            {
                var temperatureField = new TemperatureAtGaussPointsField(model);
                writer.WriteScalarField("temperature", temperatureField.CalcValuesAtVertices(solution));
            }

            // Temperature field
            path = Path.Combine(outputDirectory, "temperature_field.vtk");
            var conformingMesh = new ConformingOutputMesh(model);
            using (var writer = new VtkFileWriter(path))
            {
                var temperatureField = new TemperatureField(model, conformingMesh);
                writer.WriteMesh(conformingMesh);
                writer.WriteScalarField("temperature", conformingMesh, temperatureField.CalcValuesAtVertices(solution));
            }

            // Heat flux at Gauss Points
            path = Path.Combine(outputDirectory, "heat_flux_gauss_points.vtk");
            using (var writer = new VtkPointWriter(path))
            {
                var fluxField = new HeatFluxAtGaussPointsField(model);
                writer.WriteVectorField("heat_flux", fluxField.CalcValuesAtVertices(solution));
            }
        }
    }
}
