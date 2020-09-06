using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Enrichment;
using MGroup.XFEM.Enrichment.SingularityResolution;
using MGroup.XFEM.Entities;
using MGroup.XFEM.Geometry;
using MGroup.XFEM.Geometry.ConformingMesh;
using MGroup.XFEM.Geometry.LSM;
using MGroup.XFEM.Geometry.Tolerances;
using MGroup.XFEM.Integration;
using MGroup.XFEM.Integration.Quadratures;
using MGroup.XFEM.Materials;
using MGroup.XFEM.Plotting;
using MGroup.XFEM.Plotting.Fields;
using MGroup.XFEM.Plotting.Mesh;
using MGroup.XFEM.Plotting.Writers;
using MGroup.XFEM.Tests.Utilities;
using Xunit;

namespace MGroup.XFEM.Tests.EpoxyAg
{
    public static class ExampleRandom3D
    {
        private const string outputDirectory = @"C:\Users\Serafeim\Desktop\HEAT\2020\EpoxyAG\Random3D\";
        private const string pathConformingMesh = outputDirectory + "conforming_mesh.vtk";
        private const string pathIntersections = outputDirectory + "intersections.vtk";
        private const string pathIntegrationBulk = outputDirectory + "integration_points_bulk.vtk";
        private const string pathIntegrationBoundary = outputDirectory + "integration_points_boundary.vtk";
        private const string pathPhasesOfNodes = outputDirectory + "phases_of_nodes.vtk";
        private const string pathPhasesOfElements = outputDirectory + "phases_of_elements.vtk";
        private const string pathStepEnrichedNodes = outputDirectory + "enriched_nodes_step.vtk";
        private const string pathTemperatureAtNodes = outputDirectory + "temperature_nodes.vtk";
        private const string pathTemperatureAtGPs = outputDirectory + "temperature_integration_points.vtk";
        private const string pathTemperatureField = outputDirectory + "temperature_field.vtk";
        private const string pathHeatFluxAtGPs = outputDirectory + "heat_flux_integration_points.vtk";

        private static readonly double[] minCoords = { -2000.0, -2000.0, -2000.0 };
        private static readonly double[] maxCoords = { +2000.0, +2000.0, +2000.0 };
        private static readonly int[] numElements = { 40, 40, 40 };
        private const int bulkIntegrationOrder = 2, boundaryIntegrationOrder = 2;

        private const double singularityRelativeAreaTolerance = 1E-8;
        private const int defaultPhaseID = 0;

        //private const int numBalls = 8, rngSeed = 33; //problems in intersection mesh
        //private const int numBalls = 8, rngSeed = 13;//problems in intersection mesh
        //private const int numBalls = 8, rngSeed = 17;
        private const int numBalls = 28, rngSeed = 33;
        //private const double epoxyPhaseRadius = 0.2, silverPhaseThickness = 0.1;

        private const double conductEpoxy = 0.25, conductSilver = 429;
        private const double conductBoundaryEpoxySilver = conductEpoxy;
        private const double specialHeatCoeff = 1.0;

        public static void PlotGeometryAndEntities()
        {
            // Create physical model, LSM and phases
            Console.WriteLine("Creating physical and geometric models");
            (XModel model, BiMaterialField materialField) = CreateModel();
            GeometryPreprocessor3DRandom preprocessor = CreatePhases(model, materialField);
            GeometricModel geometricModel = preprocessor.GeometricModel;
            geometricModel.EnableOptimizations = false;

            // Plot original mesh and level sets
            //Utilities.Plotting.PlotInclusionLevelSets(outputDirectory, "level_set_before_union", model, geometricModel);

            // Find and plot intersections between level set curves and elements
            Console.WriteLine("Identifying interactions between physical and geometric models");
            geometricModel.InteractWithNodes();
            geometricModel.UnifyOverlappingPhases(true);
            //Utilities.Plotting.PlotInclusionLevelSets(outputDirectory, "level_set_after_union", model, geometricModel);

            geometricModel.InteractWithElements();
            geometricModel.FindConformingMesh();

            // Plot phases
            var phasePlotter = new PhasePlotter(model, geometricModel, defaultPhaseID);
            phasePlotter.PlotNodes(pathPhasesOfNodes);

            //TODO: The next intersections and conforming mesh should have been taken care by the geometric model. 
            //      Read them from there.
            Dictionary<IXFiniteElement, List<IElementGeometryIntersection>> elementIntersections
                = Utilities.Plotting.CalcIntersections(model, geometricModel);
            var allIntersections = new List<IElementGeometryIntersection>();
            foreach (var intersections in elementIntersections.Values) allIntersections.AddRange(intersections);
            var intersectionPlotter = new LsmElementIntersectionsPlotter(false);
            intersectionPlotter.PlotIntersections(pathIntersections, allIntersections);

            // Plot conforming mesh
            Dictionary<IXFiniteElement, IElementSubcell[]> triangulation =
                Utilities.Plotting.CreateConformingMesh(3, elementIntersections);
            var conformingMesh = new ConformingOutputMesh(model.Nodes, model.Elements, triangulation);
            //using (var writer = new VtkFileWriter(pathConformingMesh))
            //{
            //    writer.WriteMesh(conformingMesh);
            //}
            phasePlotter.PlotElements(pathPhasesOfElements, conformingMesh);

            // Plot bulk integration points
            //var integrationBulk = new IntegrationWithConformingSubtetrahedra3D(GaussLegendre3D.GetQuadratureWithOrder(2, 2, 2),
            //    TetrahedronQuadrature.Order2Points4);
            //foreach (IXFiniteElement element in model.Elements)
            //{
            //    if (element is MockElement mock) mock.IntegrationBulk = integrationBulk;
            //}
            //var integrationPlotter = new IntegrationPlotter(model);
            //integrationPlotter.PlotBulkIntegrationPoints(pathIntegrationBulk);

            // Plot boundary integration points
            //integrationPlotter.PlotBoundaryIntegrationPoints(pathIntegrationBoundary, boundaryIntegrationOrder);

            // Enrichment
            Console.WriteLine("Applying enrichments");
            var nodeEnricher = new NodeEnricherMultiphase(geometricModel, null);
            nodeEnricher.ApplyEnrichments();
            model.UpdateDofs();
            model.UpdateMaterials();

            //double elementSize = (maxCoords[0] - minCoords[0]) / numElements[0];
            //var enrichmentPlotter = new EnrichmentPlotter(model, elementSize, false);
            //enrichmentPlotter.PlotStepEnrichedNodes(pathStepEnrichedNodes);
            //enrichmentPlotter.PlotJunctionEnrichedNodes(pathJunctionEnrichedNodes);

            // Write volume fractions
            Console.WriteLine(PrintVolumes(preprocessor));
        }

        public static void PlotSolution()
        {
            // Create physical model, LSM and phases
            Console.WriteLine("Creating physical and geometric models");
            (XModel model, BiMaterialField materialField) = CreateModel();
            GeometryPreprocessor3DRandom preprocessor = CreatePhases(model, materialField);
            GeometricModel geometricModel = preprocessor.GeometricModel;

            // Prepare for analysis
            Console.WriteLine("Identifying interactions between physical and geometric models");
            geometricModel.InteractWithNodes();
            geometricModel.UnifyOverlappingPhases(true);
            geometricModel.InteractWithElements();
            geometricModel.FindConformingMesh();

            // Enrichment
            Console.WriteLine("Applying enrichments");
            var nodeEnricher = new NodeEnricherMultiphase(geometricModel, null);
            nodeEnricher.ApplyEnrichments();
            model.UpdateDofs();
            model.UpdateMaterials();

            // Write volume fractions
            Console.WriteLine(PrintVolumes(preprocessor));

            // Run analysis and plot temperature and heat flux
            Console.WriteLine("Running XFEM analysis");
            IVectorView solution = Analysis.RunStaticAnalysis(model);

            // Plot temperature
            using (var writer = new VtkPointWriter(pathTemperatureAtNodes))
            {
                var temperatureField = new TemperatureAtNodesField(model);
                writer.WriteScalarField("temperature", temperatureField.CalcValuesAtVertices(solution));
            }
            using (var writer = new VtkPointWriter(pathTemperatureAtGPs))
            {
                var temperatureField = new TemperatureAtGaussPointsField(model);
                writer.WriteScalarField("temperature", temperatureField.CalcValuesAtVertices(solution));
            }

            Dictionary<IXFiniteElement, List<IElementGeometryIntersection>> elementIntersections
                = Utilities.Plotting.CalcIntersections(model, geometricModel);
            Dictionary<IXFiniteElement, IElementSubcell[]> triangulation =
                Utilities.Plotting.CreateConformingMesh(3, elementIntersections);
            var conformingMesh = new ConformingOutputMesh(model.Nodes, model.Elements, triangulation);
            using (var writer = new VtkFileWriter(pathTemperatureField))
            {
                var temperatureField = new TemperatureField(model, conformingMesh);
                writer.WriteMesh(conformingMesh);
                writer.WriteScalarField("temperature", conformingMesh, temperatureField.CalcValuesAtVertices(solution));
            }
            using (var writer = new VtkPointWriter(pathHeatFluxAtGPs))
            {
                var fluxField = new HeatFluxAtGaussPointsField(model);
                writer.WriteVectorField("heat_flux", fluxField.CalcValuesAtVertices(solution));
            }
        }

        public static void RunHomogenization()
        {
            // Create physical model, LSM and phases
            Console.WriteLine("Creating physical and geometric models");
            (XModel model, BiMaterialField materialField) = CreateModel();
            GeometryPreprocessor3DRandom preprocessor = CreatePhases(model, materialField);
            GeometricModel geometricModel = preprocessor.GeometricModel;
            geometricModel.EnableOptimizations = false;

            // Geometric interactions
            Console.WriteLine("Identifying interactions between physical and geometric models");
            geometricModel.InteractWithNodes();
            geometricModel.UnifyOverlappingPhases(true);
            geometricModel.InteractWithElements();
            geometricModel.FindConformingMesh();

            // Materials
            Console.WriteLine("Creating materials");
            model.UpdateMaterials();

            // Enrichment
            Console.WriteLine("Applying enrichments");
            ISingularityResolver singularityResolver = new NullSingularityResolver();
            var nodeEnricher = new NodeEnricherMultiphase(geometricModel, singularityResolver);
            nodeEnricher.ApplyEnrichments();
            model.UpdateDofs();

            // Write volume fractions
            Console.WriteLine(PrintVolumes(preprocessor));

            // Run homogenization analysis
            IMatrix conductivity = Analysis.RunHomogenizationAnalysis3D(model, minCoords, maxCoords);
            Console.WriteLine(
                $"conductivity = [ {conductivity[0, 0]} {conductivity[0, 1]} {conductivity[0, 2]};"
                + $" {conductivity[1, 0]} {conductivity[1, 1]} {conductivity[1, 2]};"
                + $" {conductivity[2, 0]} {conductivity[2, 1]} {conductivity[2, 2]} ]");
        }

        private static GeometryPreprocessor3DRandom CreatePhases(XModel model, BiMaterialField materialField)
        {
            var preprocessor = new GeometryPreprocessor3DRandom();
            preprocessor.MinCoordinates = minCoords;
            preprocessor.MaxCoordinates = maxCoords;
            preprocessor.NumBalls = numBalls;
            preprocessor.RngSeed = rngSeed;

            preprocessor.GeneratePhases(model);
            materialField.PhasesWithMaterial0.Add(preprocessor.MatrixPhaseID);
            foreach (int p in preprocessor.EpoxyPhaseIDs) materialField.PhasesWithMaterial0.Add(p);
            foreach (int p in preprocessor.SilverPhaseIDs) materialField.PhasesWithMaterial1.Add(p);

            return preprocessor;
        }

        private static (XModel, BiMaterialField) CreateModel()
        {
            // Materials
            var epoxyMaterial = new ThermalMaterial(conductEpoxy, specialHeatCoeff);
            var silverMaterial = new ThermalMaterial(conductSilver, specialHeatCoeff);
            var materialField = new BiMaterialField(epoxyMaterial, silverMaterial, conductBoundaryEpoxySilver);

            return (Models.CreateHexa8Model(minCoords, maxCoords, numElements,
                bulkIntegrationOrder, boundaryIntegrationOrder, materialField), materialField);
        }

        private static string PrintVolumes(GeometryPreprocessor3DRandom preprocessor)
        {
            var stringBuilder = new StringBuilder();
            Dictionary<string, double> volumes = preprocessor.CalcPhaseVolumes();
            stringBuilder.AppendLine();
            stringBuilder.Append("Volumes of each material: ");
            foreach (string phase in volumes.Keys)
            {
                stringBuilder.Append($"{phase} phase : {volumes[phase]}, ");
            }
            stringBuilder.AppendLine();

            double totalVolume = 0;
            foreach (string phase in volumes.Keys)
            {
                totalVolume += volumes[phase];
            }
            stringBuilder.AppendLine($"Total volume: {totalVolume}");

            double volFracAg = volumes[preprocessor.SilverPhaseName] / totalVolume;
            stringBuilder.AppendLine($"Volume fraction Ag: {volFracAg}");
            double volFracInclusions = 
                (volumes[preprocessor.SilverPhaseName] + volumes[preprocessor.EpoxyPhaseName]) / totalVolume;
            stringBuilder.AppendLine($"Volume fraction inclusions: {volFracInclusions}");

            return stringBuilder.ToString();
        }
    }
}
