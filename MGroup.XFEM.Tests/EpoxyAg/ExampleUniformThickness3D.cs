using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
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
    public static class ExampleUniformThickness3D
    {
        private const string outputDirectory = @"C:\Users\Serafeim\Desktop\HEAT\2020\EpoxyAG\UniformThickness3D\";
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

        private static readonly double[] minCoords = { -1.0, -1.0, -1.0 };
        private static readonly double[] maxCoords = { +1.0, +1.0, +1.0 };
        private const double thickness = 1.0;
        private static readonly int[] numElements = { 45, 45, 45 };
        private const int bulkIntegrationOrder = 2, boundaryIntegrationOrder = 2;

        private const double zeroLevelSetTolerance = 1E-6;
        private const double singularityRelativeAreaTolerance = 1E-8;
        private const int defaultPhaseID = 0;

        //private const int numBalls = 8, rngSeed = 33; //problems in intersection mesh
        //private const int numBalls = 8, rngSeed = 13;//problems in intersection mesh
        //private const int numBalls = 8, rngSeed = 17;
        private const int numBalls = 8, rngSeed = 1;
        private const double epoxyPhaseRadius = 0.2, silverPhaseThickness = 0.1;

        private const double conductEpoxy = 1E0, conductSilver = 1E2;
        private const double conductBoundaryEpoxySilver = 1E1;
        private const double specialHeatCoeff = 1.0;

        public static void PlotGeometryAndEntities()
        {
            // Create physical model, LSM and phases
            Console.WriteLine("Creating physical and geometric models");
            (XModel model, BiMaterialField materialField) = CreateModel();
            GeometryPreprocessor3D preprocessor = CreatePhases(model, materialField);
            GeometricModel geometricModel = preprocessor.GeometricModel;

            // Plot original mesh and level sets
            Utilities.Plotting.PlotInclusionLevelSets(outputDirectory, "level_set_before_union", model, geometricModel);

            // Find and plot intersections between level set curves and elements
            Console.WriteLine("Identifying interactions between physical and geometric models");
            geometricModel.InteractWithNodes();
            geometricModel.UnifyOverlappingPhases(true);
            Utilities.Plotting.PlotInclusionLevelSets(outputDirectory, "level_set_after_union", model, geometricModel);

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
            var intersectionPlotter = new LsmElementIntersectionsPlotter();
            intersectionPlotter.PlotIntersections(pathIntersections, allIntersections);

            // Plot conforming mesh
            Dictionary<IXFiniteElement, IElementSubcell[]> triangulation =
                Utilities.Plotting.CreateConformingMesh(3, elementIntersections);
            var conformingMesh = new ConformingOutputMesh(model.Nodes, model.Elements, triangulation);
            using (var writer = new VtkFileWriter(pathConformingMesh))
            {
                writer.WriteMesh(conformingMesh);
            }
            phasePlotter.PlotElements(pathPhasesOfElements, conformingMesh);

            // Plot bulk integration points
            var integrationBulk = new IntegrationWithConformingSubtetrahedra3D(GaussLegendre3D.GetQuadratureWithOrder(2, 2, 2),
                TetrahedronQuadrature.Order2Points4);
            foreach (IXFiniteElement element in model.Elements)
            {
                if (element is MockElement mock) mock.IntegrationBulk = integrationBulk;
            }
            var integrationPlotter = new IntegrationPlotter(model);
            integrationPlotter.PlotBulkIntegrationPoints(pathIntegrationBulk);

            // Plot boundary integration points
            integrationPlotter.PlotBoundaryIntegrationPoints(pathIntegrationBoundary, boundaryIntegrationOrder);

            // Enrichment
            Console.WriteLine("Applying enrichments");
            var nodeEnricher = new NodeEnricherMultiphase(geometricModel, null);
            nodeEnricher.ApplyEnrichments();
            model.UpdateDofs();
            model.UpdateMaterials();

            double elementSize = (maxCoords[0] - minCoords[0]) / numElements[0];
            var enrichmentPlotter = new EnrichmentPlotter(model, elementSize, false);
            enrichmentPlotter.PlotStepEnrichedNodes(pathStepEnrichedNodes);
            //enrichmentPlotter.PlotJunctionEnrichedNodes(pathJunctionEnrichedNodes);

            // Write volume fractions
            PrintVolumes(preprocessor);
        }

        public static void PlotSolution()
        {
            // Create physical model, LSM and phases
            Console.WriteLine("Creating physical and geometric models");
            (XModel model, BiMaterialField materialField) = CreateModel();
            GeometryPreprocessor3D preprocessor = CreatePhases(model, materialField);
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
            PrintVolumes(preprocessor);

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

        private static GeometryPreprocessor3D CreatePhases(XModel model, BiMaterialField materialField)
        {
            var preprocessor = new GeometryPreprocessor3D();
            preprocessor.MinCoordinates = minCoords;
            preprocessor.MaxCoordinates = maxCoords;
            preprocessor.NumBalls = numBalls;
            preprocessor.RngSeed = rngSeed;
            preprocessor.RadiusEpoxyPhase = epoxyPhaseRadius;
            preprocessor.ThicknessSilverPhase = silverPhaseThickness;

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

        private static void PrintVolumes(GeometryPreprocessor3D preprocessor)
        {
            Dictionary<string, double> volumes = preprocessor.CalcPhaseVolumes();
            Console.WriteLine();
            Console.Write("Total areas of each material: ");
            foreach (string phase in volumes.Keys)
            {
                Console.Write($"{phase} phase : {volumes[phase]}, ");
            }
            Console.WriteLine();
        }
    }
}
