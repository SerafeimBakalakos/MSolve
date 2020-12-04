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
using MGroup.XFEM.Enrichment.Enrichers;

namespace MGroup.XFEM.Tests.Unions
{
    public static class UnionTwoHollowBalls2D
    {
        private const string outputDirectory = @"C:\Users\Serafeim\Desktop\HEAT\2020\UnionTwoHollowBalls2D\";
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
            XModel<IXMultiphaseElement> model = CreateModel();
            PhaseGeometryModel_OLD geometricModel = CreatePhases(model);

            // Plot original mesh and level sets
            Utilities.Plotting.PlotInclusionLevelSets(outputDirectory, "level_set_before_union", model, geometricModel);

            // Find and plot intersections between level set curves and elements
            geometricModel.InteractWithNodes();
            Assert.Equal(5, geometricModel.Phases.Count);
            geometricModel.UnifyOverlappingPhases(true);
            Assert.Equal(4, geometricModel.Phases.Count);
            Utilities.Plotting.PlotInclusionLevelSets(outputDirectory, "level_set_after_union", model, geometricModel);

            geometricModel.InteractWithElements();
            geometricModel.FindConformingMesh();

            //TODO: The next intersections and conforming mesh should have been taken care by the geometric model. 
            //      Read them from there.
            Dictionary<IXFiniteElement, List<IElementDiscontinuityInteraction>> elementIntersections
                = Utilities.Plotting.CalcIntersections(model, geometricModel);
            var allIntersections = new List<IElementDiscontinuityInteraction>();
            foreach (var intersections in elementIntersections.Values) allIntersections.AddRange(intersections);
            var intersectionPlotter = new LsmElementIntersectionsPlotter_OLD();
            intersectionPlotter.PlotIntersections(pathIntersections, allIntersections);

            // Plot conforming mesh
            Dictionary<IXFiniteElement, IElementSubcell[]> triangulation =
                Utilities.Plotting.CreateConformingMesh(2, elementIntersections);
            var conformingMesh = new ConformingOutputMesh_OLD(model.XNodes, model.Elements, triangulation);
            using (var writer = new VtkFileWriter(pathConformingMesh))
            {
                writer.WriteMesh(conformingMesh);
            }

            // Plot bulk integration points
            var integrationBulk = new IntegrationWithConformingSubtriangles2D(TriangleQuadratureSymmetricGaussian.Order2Points3);
            foreach (IXFiniteElement element in model.Elements)
            {
                if (element is MockElement mock) mock.IntegrationBulk = integrationBulk;
            }
            var integrationPlotter = new IntegrationPlotter_OLD(model);
            integrationPlotter.PlotBulkIntegrationPoints(pathIntegrationBulk);

            // Plot boundary integration points
            integrationPlotter.PlotBoundaryIntegrationPoints(pathIntegrationBoundary, boundaryIntegrationOrder);

            // Plot phases
            var phasePlotter = new PhasePlotter_OLD(model, geometricModel, defaultPhaseID);
            phasePlotter.PlotNodes(pathPhasesOfNodes);
            phasePlotter.PlotElements(pathPhasesOfElements, conformingMesh);

            // Enrichment
            ISingularityResolver singularityResolver
                    //= new MultiphaseRelativeAreaResolver(geometricModel, singularityRelativeAreaTolerance);
                    = new NullSingularityResolver();
            var nodeEnricher = new NodeEnricherMultiphase_OLD(geometricModel, singularityResolver);
            nodeEnricher.ApplyEnrichments();
            model.UpdateDofs();
            model.UpdateMaterials();

            double elementSize = (maxCoords[0] - minCoords[0]) / numElements[0];
            var enrichmentPlotter = new EnrichmentPlotter_OLD(model, elementSize, false);
            enrichmentPlotter.PlotStepEnrichedNodes(pathStepEnrichedNodes);
            //enrichmentPlotter.PlotJunctionEnrichedNodes(pathJunctionEnrichedNodes);
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

        private static PhaseGeometryModel_OLD CreatePhases(XModel<IXMultiphaseElement> model)
        {
            var ballsInternal = new Circle2D[2];
            ballsInternal[0] = new Circle2D(-0.3, 0, 0.15);
            ballsInternal[1] = new Circle2D(+0.3, 0, 0.1);

            var ballsExternal = new Circle2D[2];
            ballsExternal[0] = new Circle2D(-0.3, 0, 0.5);
            ballsExternal[1] = new Circle2D(+0.3, 0, 0.4);

            var geometricModel = new PhaseGeometryModel_OLD(2, model);
            var defaultPhase = new DefaultPhase(defaultPhaseID);
            geometricModel.Phases.Add(defaultPhase);
            for (int b = 0; b < 2; ++b)
            {
                var externalLsm = new SimpleLsm2D(2 * b + 1, model.XNodes, ballsExternal[b]);
                var externalPhase = new HollowLsmPhase(2 * b + 1, geometricModel, 0);
                geometricModel.Phases.Add(externalPhase);

                var externalBoundary = new ClosedLsmPhaseBoundary(externalPhase.ID, externalLsm, defaultPhase, externalPhase);
                defaultPhase.ExternalBoundaries.Add(externalBoundary);
                defaultPhase.Neighbors.Add(externalPhase);
                externalPhase.ExternalBoundaries.Add(externalBoundary);
                externalPhase.Neighbors.Add(defaultPhase);

                var internalLsm = new SimpleLsm2D(2 * b + 2, model.XNodes, ballsInternal[b]);
                var internalPhase = new LsmPhase_OLD(2 * b + 2, geometricModel, -1);
                geometricModel.Phases.Add(internalPhase);

                var internalBoundary = new ClosedLsmPhaseBoundary(internalPhase.ID, internalLsm, externalPhase, internalPhase);
                externalPhase.InternalBoundaries.Add(internalBoundary);
                externalPhase.InternalPhases.Add(internalPhase);
                externalPhase.Neighbors.Add(internalPhase);
                internalPhase.ExternalBoundaries.Add(internalBoundary);
                internalPhase.Neighbors.Add(externalPhase);
            }
            return geometricModel;
        }
    }
}
