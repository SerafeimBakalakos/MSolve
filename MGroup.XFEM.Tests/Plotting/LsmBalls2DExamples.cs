using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ISAAR.MSolve.Analyzers;
using ISAAR.MSolve.Discretization;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Discretization.Mesh;
using ISAAR.MSolve.Discretization.Mesh.Generation;
using ISAAR.MSolve.Discretization.Mesh.Generation.Custom;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Problems;
using ISAAR.MSolve.Solvers.Direct;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Enrichment;
using MGroup.XFEM.Enrichment.SingularityResolution;
using MGroup.XFEM.Entities;
using MGroup.XFEM.Geometry;
using MGroup.XFEM.Geometry.ConformingMesh;
using MGroup.XFEM.Geometry.LSM;
using MGroup.XFEM.Geometry.Primitives;
using MGroup.XFEM.Geometry.Tolerances;
using MGroup.XFEM.Integration.Quadratures;
using MGroup.XFEM.Integration;
using MGroup.XFEM.Materials;
using MGroup.XFEM.Plotting;
using MGroup.XFEM.Plotting.Fields;
using MGroup.XFEM.Plotting.Mesh;
using MGroup.XFEM.Plotting.Writers;
using MGroup.XFEM.Tests.Utilities;

namespace MGroup.XFEM.Tests.Plotting
{
    public static class LsmBalls2DExamples
    {
        private const string outputDirectory = @"C:\Users\Serafeim\Desktop\HEAT\2020\Circles2D\";
        private const string pathConformingMesh = outputDirectory + "conforming_mesh.vtk";
        private const string pathIntersections = outputDirectory + "intersections.vtk";
        private const string pathIntegrationBulk = outputDirectory + "integration_points_bulk.vtk";
        private const string pathIntegrationBoundary = outputDirectory + "integration_points_boundary.vtk";
        private const string pathPhasesOfNodes = outputDirectory + "phases_of_nodes.vtk";
        private const string pathPhasesOfElements = outputDirectory + "phases_of_elements.vtk";
        private const string pathStepEnrichedNodes = outputDirectory + "enriched_nodes_step.vtk";
        //private const string pathJunctionEnrichedNodes = outputDirectory + "enriched_nodes_junction.vtk";
        private const string pathTemperatureAtNodes = outputDirectory + "temperature_nodes.vtk";
        private const string pathTemperatureAtGPs = outputDirectory + "temperature_integration_points.vtk";
        private const string pathTemperatureField = outputDirectory + "temperature_field.vtk";
        private const string pathHeatFluxAtGPs = outputDirectory + "heat_flux_integration_points.vtk";


        private static readonly double[] minCoords = { -1.0, -1.0 };
        private static readonly double[] maxCoords = { +1.0, +1.0 };
        private const double thickness = 1.0;
        private static readonly int[] numElements = { 15, 15 };
        private const int bulkIntegrationOrder = 2, boundaryIntegrationOrder = 2;

        private const int numBallsX = 2, numBallsY = 1;
        private const double ballRadius = 0.3;

        private const double zeroLevelSetTolerance = 1E-6;
        private const double singularityRelativeAreaTolerance = 1E-8;
        private const int defaultPhaseID = 0;


        private const double conductMatrix = 1E0, conductInclusion = 1E5;
        private const double conductBoundaryMatrixInclusion = 1E1, conductBoundaryInclusionInclusion = 1E2;
        private const double specialHeatCoeff = 1.0;

        public static void PlotGeometry()
        {
            // Create model and LSM
            XModel model = CreateModel();
            List<SimpleLsm2D> lsmCurves = InitializeLSM(model);

            // Plot original mesh and level sets
            PlotInclusionLevelSets(outputDirectory, "level_set", model, lsmCurves);

            // Plot intersections between level set curves and elements
            Dictionary<IXFiniteElement, List<IElementGeometryIntersection>> elementIntersections 
                = Utilities.Plotting.CalcIntersections(model, lsmCurves);
            var allIntersections = new List<IElementGeometryIntersection>();
            foreach (var intersections in elementIntersections.Values) allIntersections.AddRange(intersections);
            var intersectionPlotter = new LsmElementIntersectionsPlotter();
            intersectionPlotter.PlotIntersections(pathIntersections, allIntersections);

            // Plot conforming mesh
            Dictionary<IXFiniteElement, IElementSubcell[]> triangulation = 
                Utilities.Plotting.CreateConformingMesh(2, elementIntersections);
            var conformingMesh = new ConformingOutputMesh(model.Nodes, model.Elements, triangulation);
            using (var writer = new VtkFileWriter(pathConformingMesh))
            {
                writer.WriteMesh(conformingMesh);
            }

            // Plot bulk integration points
            var integrationBulk = new IntegrationWithConformingSubtriangles2D(GaussLegendre2D.GetQuadratureWithOrder(2, 2),
                TriangleQuadratureSymmetricGaussian.Order2Points3);
            foreach (IXFiniteElement element in model.Elements)
            {
                if (element is MockElement mock) mock.IntegrationBulk = integrationBulk;
            }
            var integrationPlotter = new IntegrationPlotter(model);
            integrationPlotter.PlotBulkIntegrationPoints(pathIntegrationBulk);

            // Plot boundary integration points
            integrationPlotter.PlotBoundaryIntegrationPoints(pathIntegrationBoundary, boundaryIntegrationOrder);
        }

        public static void PlotGeometryAndEntities()
        {
            // Create physical model, LSM and phases
            XModel model = CreateModel();
            List<SimpleLsm2D> lsmCurves = InitializeLSM(model);
            GeometricModel geometricModel = CreatePhases(model, lsmCurves);

            // Plot original mesh and level sets
            PlotInclusionLevelSets(outputDirectory, "level_set", model, lsmCurves);

            // Find and plot intersections between level set curves and elements
            geometricModel.InteractWithNodes();
            geometricModel.InteractWithElements();
            geometricModel.FindConformingMesh();

            //TODO: The next intersections and conforming mesh should have been taken care by the geometric model. 
            //      Read them from there.
            Dictionary<IXFiniteElement, List<IElementGeometryIntersection>> elementIntersections
                 = Utilities.Plotting.CalcIntersections(model, lsmCurves);
            var allIntersections = new List<IElementGeometryIntersection>();
            foreach (var intersections in elementIntersections.Values) allIntersections.AddRange(intersections);
            var intersectionPlotter = new LsmElementIntersectionsPlotter();
            intersectionPlotter.PlotIntersections(pathIntersections, allIntersections);

            // Plot conforming mesh
            Dictionary<IXFiniteElement, IElementSubcell[]> triangulation =
                Utilities.Plotting.CreateConformingMesh(2, elementIntersections);
            var conformingMesh = new ConformingOutputMesh(model.Nodes, model.Elements, triangulation);
            using (var writer = new VtkFileWriter(pathConformingMesh))
            {
                writer.WriteMesh(conformingMesh);
            }

            // Plot bulk integration points
            var integrationBulk = new IntegrationWithConformingSubtriangles2D(GaussLegendre2D.GetQuadratureWithOrder(2, 2),
                TriangleQuadratureSymmetricGaussian.Order2Points3);
            foreach (IXFiniteElement element in model.Elements)
            {
                if (element is MockElement mock) mock.IntegrationBulk = integrationBulk;
            }
            var integrationPlotter = new IntegrationPlotter(model);
            integrationPlotter.PlotBulkIntegrationPoints(pathIntegrationBulk);

            // Plot boundary integration points
            integrationPlotter.PlotBoundaryIntegrationPoints(pathIntegrationBoundary, boundaryIntegrationOrder);

            // Plot phases
            var phasePlotter = new PhasePlotter(model, geometricModel, defaultPhaseID);
            phasePlotter.PlotNodes(pathPhasesOfNodes);
            phasePlotter.PlotElements(pathPhasesOfElements, conformingMesh);

            // Enrichment
            ISingularityResolver singularityResolver 
                = new RelativeAreaResolver(geometricModel, singularityRelativeAreaTolerance);
            var nodeEnricher = new NodeEnricherMultiphase(geometricModel, singularityResolver);
            nodeEnricher.ApplyEnrichments();
            model.UpdateDofs();
            model.UpdateMaterials();

            double elementSize = (maxCoords[0] - minCoords[0]) / numElements[0];
            var enrichmentPlotter = new EnrichmentPlotter(model, elementSize, false);
            enrichmentPlotter.PlotStepEnrichedNodes(pathStepEnrichedNodes);
            //enrichmentPlotter.PlotJunctionEnrichedNodes(pathJunctionEnrichedNodes);
        }

        public static void PlotSolution()
        {
            // Create physical model, LSM and phases
            XModel model = CreateModel();
            List<SimpleLsm2D> lsmCurves = InitializeLSM(model);
            GeometricModel geometricModel = CreatePhases(model, lsmCurves);

            // Plot original mesh and level sets
            PlotInclusionLevelSets(outputDirectory, "level_set", model, lsmCurves);

            // Find and plot intersections between level set curves and elements
            geometricModel.InteractWithNodes();
            geometricModel.InteractWithElements();
            geometricModel.FindConformingMesh();

            //TODO: The next intersections and conforming mesh should have been taken care by the geometric model. 
            //      Read them from there.
            Dictionary<IXFiniteElement, List<IElementGeometryIntersection>> elementIntersections
                = Utilities.Plotting.CalcIntersections(model, lsmCurves);
            var allIntersections = new List<IElementGeometryIntersection>();
            foreach (var intersections in elementIntersections.Values) allIntersections.AddRange(intersections);
            var intersectionPlotter = new LsmElementIntersectionsPlotter();
            intersectionPlotter.PlotIntersections(pathIntersections, allIntersections);

            // Plot conforming mesh
            Dictionary<IXFiniteElement, IElementSubcell[]> triangulation =
                Utilities.Plotting.CreateConformingMesh(2, elementIntersections);
            var conformingMesh = new ConformingOutputMesh(model.Nodes, model.Elements, triangulation);
            using (var writer = new VtkFileWriter(pathConformingMesh))
            {
                writer.WriteMesh(conformingMesh);
            }

            // Plot phases
            var phasePlotter = new PhasePlotter(model, geometricModel, defaultPhaseID);
            phasePlotter.PlotNodes(pathPhasesOfNodes);
            phasePlotter.PlotElements(pathPhasesOfElements, conformingMesh);

            // Plot bulk integration points
            model.UpdateMaterials();
            var integrationBulk = new Integration.IntegrationWithConformingSubtriangles2D(GaussLegendre2D.GetQuadratureWithOrder(2, 2),
                TriangleQuadratureSymmetricGaussian.Order2Points3);
            foreach (IXFiniteElement element in model.Elements)
            {
                if (element is MockElement mock) mock.IntegrationBulk = integrationBulk;
            }
            var integrationPlotter = new IntegrationPlotter(model);
            integrationPlotter.PlotBulkIntegrationPoints(pathIntegrationBulk);

            // Plot boundary integration points
            integrationPlotter.PlotBoundaryIntegrationPoints(pathIntegrationBoundary, boundaryIntegrationOrder);

            // Enrichment
            ISingularityResolver singularityResolver
                = new RelativeAreaResolver(geometricModel, singularityRelativeAreaTolerance);
            var nodeEnricher = new NodeEnricherMultiphase(geometricModel, singularityResolver);
            nodeEnricher.ApplyEnrichments();
            model.UpdateDofs();

            double elementSize = (maxCoords[0] - minCoords[0]) / numElements[0];
            var enrichmentPlotter = new EnrichmentPlotter(model, elementSize, false);
            enrichmentPlotter.PlotStepEnrichedNodes(pathStepEnrichedNodes);
            //enrichmentPlotter.PlotJunctionEnrichedNodes(pathJunctionEnrichedNodes);

            // Run analysis and plot temperature and heat flux
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
        

        private static XModel CreateModel()
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

        private static GeometricModel CreatePhases(XModel model, List<SimpleLsm2D> lsmCurves)
        {
            var geometricModel = new GeometricModel(2, model);
            var defaultPhase = new DefaultPhase(defaultPhaseID);
            geometricModel.Phases.Add(defaultPhase);
            for (int p = 0; p < lsmCurves.Count; ++p)
            {
                SimpleLsm2D curve = lsmCurves[p];
                var phase = new ConvexPhase(p + 1, geometricModel);
                geometricModel.Phases.Add(phase);

                var boundary = new PhaseBoundary(curve, defaultPhase, phase);
                defaultPhase.ExternalBoundaries.Add(boundary);
                defaultPhase.Neighbors.Add(phase);
                phase.ExternalBoundaries.Add(boundary);
                phase.Neighbors.Add(defaultPhase);
            }
            return geometricModel;
        }

        private static List<SimpleLsm2D> InitializeLSM(XModel model)
        {
            double xMin = minCoords[0], xMax = maxCoords[0], yMin = minCoords[1], yMax = maxCoords[1];
            var curves = new List<SimpleLsm2D>(numBallsX * numBallsY);
            double dx = (xMax - xMin) / (numBallsX + 1);
            double dy = (yMax - yMin) / (numBallsY + 1);
            for (int i = 0; i < numBallsX; ++i)
            {
                double centerX = xMin + (i + 1) * dx;
                for (int j = 0; j < numBallsY; ++j)
                {
                    double centerY = yMin + (j + 1) * dy;
                    var circle = new Circle2D(centerX, centerY, ballRadius);
                    var lsm = new SimpleLsm2D(model, circle);
                    curves.Add(lsm);
                }
            }

            return curves;
        }

        private static void PlotInclusionLevelSets(string directoryPath, string vtkFilenamePrefix,
            XModel model, IList<SimpleLsm2D> lsmCurves)
        {
            for (int c = 0; c < lsmCurves.Count; ++c)
            {
                directoryPath = directoryPath.Trim('\\');
                string suffix = (lsmCurves.Count == 1) ? "" : $"{c}";
                string file = $"{directoryPath}\\{vtkFilenamePrefix}{suffix}.vtk";
                using (var writer = new VtkFileWriter(file))
                {
                    var levelSetField = new LevelSetField(model, lsmCurves[c]);
                    writer.WriteMesh(levelSetField.Mesh);
                    writer.WriteScalarField($"inclusion{suffix}_level_set",
                        levelSetField.Mesh, levelSetField.CalcValuesAtVertices());
                }
            }
        }
    }
}
