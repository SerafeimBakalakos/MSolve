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
    public static class ExampleUniformThickness2D
    {
        private const string outputDirectory = @"C:\Users\Serafeim\Desktop\HEAT\2020\EpoxyAG\UniformThickness2D\";
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

        private static readonly double[] minCoords = { -1.0, -1.0 };
        private static readonly double[] maxCoords = { +1.0, +1.0 };
        private const double thickness = 1.0;
        private static readonly int[] numElements = { 45, 45 };
        private const int bulkIntegrationOrder = 2, boundaryIntegrationOrder = 2;

        private const double zeroLevelSetTolerance = 1E-6;
        private const double singularityRelativeAreaTolerance = 1E-8;
        private const int defaultPhaseID = 0;

        private const int numBalls = 8, rngSeed = 33;
        private const double epoxyPhaseRadius = 0.2, silverPhaseThickness = 0.1;

        private const double conductEpoxy = 1E0, conductSilver = 1E2;
        private const double conductBoundaryEpoxySilver = 1E1;
        private const double specialHeatCoeff = 1.0;

        public static void PlotGeometryAndEntities()
        {
            // Create physical model, LSM and phases
            (XModel model, BiMaterialField materialField) = CreateModel();
            GeometricModel geometricModel = CreatePhases(model, materialField);

            // Plot original mesh and level sets
            PlotInclusionLevelSets(outputDirectory, "level_set_before_union", model, geometricModel);

            // Find and plot intersections between level set curves and elements
            geometricModel.InteractWithNodes();
            geometricModel.UnifyOverlappingPhases(true);
            PlotInclusionLevelSets(outputDirectory, "level_set_after_union", model, geometricModel);

            geometricModel.InteractWithElements();
            geometricModel.FindConformingMesh();

            // Plot phases
            var phasePlotter = new PhasePlotter2D(model, geometricModel, defaultPhaseID);
            phasePlotter.PlotNodes(pathPhasesOfNodes);

            //TODO: The next intersections and conforming mesh should have been taken care by the geometric model. 
            //      Read them from there.
            Dictionary<IXFiniteElement, List<LsmElementIntersection2D>> elementIntersections
                = CalcIntersections(model, geometricModel);
            var allIntersections = new List<LsmElementIntersection2D>();
            foreach (var intersections in elementIntersections.Values) allIntersections.AddRange(intersections);
            var intersectionPlotter = new LsmElementIntersectionsPlotter();
            intersectionPlotter.PlotIntersections(pathIntersections, allIntersections);

            // Plot conforming mesh
            Dictionary<IXFiniteElement, ElementSubtriangle2D[]> triangulation = CreateConformingMesh(elementIntersections);
            var conformingMesh = new ConformingOutputMesh2D(model.Nodes, model.Elements, triangulation);
            using (var writer = new VtkFileWriter(pathConformingMesh))
            {
                writer.WriteMesh(conformingMesh);
            }
            phasePlotter.PlotElements(pathPhasesOfElements, conformingMesh);

            // Plot bulk integration points
            var integrationBulk = new IntegrationWithConformingSubtriangles2D(GaussLegendre2D.GetQuadratureWithOrder(2, 2),
                TriangleQuadratureSymmetricGaussian.Order2Points3);
            foreach (IXFiniteElement element in model.Elements)
            {
                if (element is MockElement mock) mock.IntegrationBulk = integrationBulk;
            }
            var integrationPlotter = new IntegrationPlotter2D(model);
            integrationPlotter.PlotBulkIntegrationPoints(pathIntegrationBulk);

            // Plot boundary integration points
            integrationPlotter.PlotBoundaryIntegrationPoints(pathIntegrationBoundary, boundaryIntegrationOrder);

            // Enrichment
            ISingularityResolver singularityResolver
                = new RelativeAreaResolver2D(geometricModel, singularityRelativeAreaTolerance);
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
            (XModel model, BiMaterialField materialField) = CreateModel();
            GeometricModel geometricModel = CreatePhases(model, materialField);

            // Prepare for analysis
            geometricModel.InteractWithNodes();
            geometricModel.UnifyOverlappingPhases(true);
            geometricModel.InteractWithElements();
            geometricModel.FindConformingMesh();
            model.UpdateMaterials();


            // Enrichment
            ISingularityResolver singularityResolver
                = new RelativeAreaResolver2D(geometricModel, singularityRelativeAreaTolerance);
            var nodeEnricher = new NodeEnricherMultiphase(geometricModel, singularityResolver);
            nodeEnricher.ApplyEnrichments();
            model.UpdateDofs();

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

            Dictionary<IXFiniteElement, List<LsmElementIntersection2D>> elementIntersections
                = CalcIntersections(model, geometricModel);
            Dictionary<IXFiniteElement, ElementSubtriangle2D[]> triangulation = CreateConformingMesh(elementIntersections);
            var conformingMesh = new ConformingOutputMesh2D(model.Nodes, model.Elements, triangulation);
            using (var writer = new VtkFileWriter(pathTemperatureField))
            {
                var temperatureField = new TemperatureField2D(model, conformingMesh);
                writer.WriteMesh(conformingMesh);
                writer.WriteScalarField("temperature", conformingMesh, temperatureField.CalcValuesAtVertices(solution));
            }
            using (var writer = new VtkPointWriter(pathHeatFluxAtGPs))
            {
                var fluxField = new HeatFluxAtGaussPointsField2D(model);
                writer.WriteVectorField("heat_flux", fluxField.CalcValuesAtVertices(solution));
            }
        }

        private static GeometricModel CreatePhases(XModel model, BiMaterialField materialField)
        {
            var preprocessor = new GeometryPreprocessor2D();
            preprocessor.MinCoordinates = minCoords;
            preprocessor.MaxCoordinates = maxCoords;
            preprocessor.NumBalls = numBalls;
            preprocessor.RngSeed = rngSeed;
            preprocessor.RadiusEpoxyPhase = epoxyPhaseRadius;
            preprocessor.ThicknessSilverPhase = silverPhaseThickness;

            preprocessor.GeneratePhases(model);
            foreach (int p in preprocessor.EpoxyPhases) materialField.PhasesWithMaterial0.Add(p);
            foreach (int p in preprocessor.SilverPhases) materialField.PhasesWithMaterial1.Add(p);

            return preprocessor.GeometricModel;
        }

        private static Dictionary<IXFiniteElement, List<LsmElementIntersection2D>> CalcIntersections(
            XModel model, GeometricModel geometricModel)
        {
            IImplicitGeometry[] curves = FindCurvesOf(geometricModel).Values.ToArray();
            var intersections = new Dictionary<IXFiniteElement, List<LsmElementIntersection2D>>();
            foreach (IXFiniteElement element in model.Elements)
            {
                var element2D = (IXFiniteElement2D)element;
                var elementIntersections = new List<LsmElementIntersection2D>();
                foreach (IImplicitGeometry curve in curves)
                {
                    IElementGeometryIntersection intersection = curve.Intersect(element);
                    if (intersection.RelativePosition != RelativePositionCurveElement.Disjoint)
                    {
                        element2D.Intersections.Add(intersection);
                        elementIntersections.Add((LsmElementIntersection2D)intersection);
                    }
                }
                if (elementIntersections.Count > 0) intersections.Add(element, elementIntersections);
            }
            return intersections;
        }

        private static Dictionary<IXFiniteElement, ElementSubtriangle2D[]> CreateConformingMesh(
            Dictionary<IXFiniteElement, List<LsmElementIntersection2D>> intersections)
        {
            var tolerance = new ArbitrarySideMeshTolerance();
            var triangulator = new ConformingTriangulator2D();
            var conformingMesh = new Dictionary<IXFiniteElement, ElementSubtriangle2D[]>();
            foreach (IXFiniteElement element in intersections.Keys)
            {
                var element2D = (IXFiniteElement2D)element;
                List<LsmElementIntersection2D> elementIntersections = intersections[element];
                ElementSubtriangle2D[] subtriangles = triangulator.FindConformingMesh(element, elementIntersections, tolerance);
                conformingMesh[element] = subtriangles;
                element2D.ConformingSubtriangles = subtriangles;
            }
            return conformingMesh;
        }

        private static (XModel, BiMaterialField) CreateModel()
        {
            // Materials
            var epoxyMaterial = new ThermalMaterial(conductEpoxy, specialHeatCoeff);
            var silverMaterial = new ThermalMaterial(conductSilver, specialHeatCoeff);
            var materialField = new BiMaterialField(epoxyMaterial, silverMaterial, conductBoundaryEpoxySilver);

            return (Models.CreateQuad4Model(minCoords, maxCoords, thickness, numElements,
                bulkIntegrationOrder, boundaryIntegrationOrder, materialField), materialField);
        }

        private static Dictionary<int, SimpleLsm2D> FindCurvesOf(GeometricModel geometricModel)
        {
            var lsmCurves = new Dictionary<int, SimpleLsm2D>();
            foreach (IPhase phase in geometricModel.Phases)
            {
                if (phase is DefaultPhase) continue;
                Debug.Assert(phase.ExternalBoundaries.Count == 1);
                lsmCurves[phase.ID] = (SimpleLsm2D)(phase.ExternalBoundaries[0].Geometry);
            }
            return lsmCurves;
        }

        private static void PlotInclusionLevelSets(string directoryPath, string vtkFilenamePrefix,
            XModel model, GeometricModel geometricModel)
        {
            Dictionary<int, SimpleLsm2D> lsmCurves = FindCurvesOf(geometricModel);
            foreach (var pair in lsmCurves)
            {
                int phaseId = pair.Key;
                SimpleLsm2D geometry = pair.Value;
                directoryPath = directoryPath.Trim('\\');
                string suffix = (lsmCurves.Count == 1) ? "" : String.Format("{0:000}", phaseId);
                string file = $"{directoryPath}\\{vtkFilenamePrefix}{suffix}.vtk";
                using (var writer = new VtkFileWriter(file))
                {
                    var levelSetField = new LevelSetField(model, geometry);
                    writer.WriteMesh(levelSetField.Mesh);
                    writer.WriteScalarField($"inclusion{suffix}_level_set",
                        levelSetField.Mesh, levelSetField.CalcValuesAtVertices());
                }
            }
        }
    }
}
