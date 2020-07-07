using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
using System.Diagnostics;
using Xunit;
using MGroup.XFEM.Tests.Utilities;

namespace MGroup.XFEM.Tests.Unions
{
    public static class UnionTwoBalls2D
    {
        private const string outputDirectory = @"C:\Users\Serafeim\Desktop\HEAT\2020\UnionTwoBalls2D\";
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

        private const double zeroLevelSetTolerance = 1E-6;
        private const double singularityRelativeAreaTolerance = 1E-8;
        private const int defaultPhaseID = 0;


        private const double conductMatrix = 1E0, conductInclusion = 1E5;
        private const double conductBoundaryMatrixInclusion = 1E1, conductBoundaryInclusionInclusion = 1E2;
        private const double specialHeatCoeff = 1.0;

        public static void PlotGeometryAndEntities()
        {
            // Create physical model, LSM and phases
            XModel model = CreateModel();
            GeometricModel geometricModel = CreatePhases(model);

            // Plot original mesh and level sets
            PlotInclusionLevelSets(outputDirectory, "level_set_before_union", model, geometricModel);

            // Find and plot intersections between level set curves and elements
            geometricModel.InteractWithNodes();
            Assert.Equal(3, geometricModel.Phases.Count);
            geometricModel.UnifyOverlappingPhases(true);
            Assert.Equal(2, geometricModel.Phases.Count);
            PlotInclusionLevelSets(outputDirectory, "level_set_after_union", model, geometricModel);

            geometricModel.InteractWithElements();
            geometricModel.FindConformingMesh();

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

            // Plot phases
            var phasePlotter = new PhasePlotter2D(model, geometricModel, defaultPhaseID);
            phasePlotter.PlotNodes(pathPhasesOfNodes);
            phasePlotter.PlotElements(pathPhasesOfElements, conformingMesh);

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

        private static Dictionary<IXFiniteElement, List<LsmElementIntersection2D>> CalcIntersections(
            XModel model, GeometricModel geometricModel)
        {
            IImplicitGeometry[] curves = FindCurvesOf(geometricModel); 
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

        private static GeometricModel CreatePhases(XModel model)
        {
            var balls = new Circle2D[2];
            balls[0] = new Circle2D(-0.3, 0, 0.5);
            balls[1] = new Circle2D(+0.3, 0, 0.4);

            var geometricModel = new GeometricModel(2, model);
            var defaultPhase = new DefaultPhase(defaultPhaseID);
            geometricModel.Phases.Add(defaultPhase);
            for (int p = 0; p < 2; ++p)
            {
                var lsm = new SimpleLsm2D(model, balls[p]);
                var phase = new LsmPhase(p + 1, geometricModel, 0);
                geometricModel.Phases.Add(phase);

                var boundary = new PhaseBoundary(lsm, defaultPhase, phase);
                defaultPhase.ExternalBoundaries.Add(boundary);
                defaultPhase.Neighbors.Add(phase);
                phase.ExternalBoundaries.Add(boundary);
                phase.Neighbors.Add(defaultPhase);
            }
            return geometricModel;
        }

        private static SimpleLsm2D[] FindCurvesOf(GeometricModel geometricModel)
        {
            var lsmCurves = new HashSet<SimpleLsm2D>();
            foreach (IPhase phase in geometricModel.Phases)
            {
                if (phase is DefaultPhase) continue;
                foreach (PhaseBoundary boundary in phase.ExternalBoundaries)
                {
                    lsmCurves.Add((SimpleLsm2D)(boundary.Geometry));
                }
            }
            return lsmCurves.ToArray();
        }

        private static void PlotInclusionLevelSets(string directoryPath, string vtkFilenamePrefix,
            XModel model, GeometricModel geometricModel)
        {
            IImplicitGeometry[] lsmCurves = FindCurvesOf(geometricModel);
            for (int c = 0; c < lsmCurves.Length; ++c)
            {
                directoryPath = directoryPath.Trim('\\');
                string suffix = (lsmCurves.Length == 1) ? "" : $"{c}";
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
