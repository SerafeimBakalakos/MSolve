using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ISAAR.MSolve.Discretization.Integration;
using ISAAR.MSolve.Discretization.Integration.Quadratures;
using ISAAR.MSolve.Discretization.Mesh;
using ISAAR.MSolve.Discretization.Mesh.Generation;
using ISAAR.MSolve.Discretization.Mesh.Generation.Custom;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Enrichment;
using MGroup.XFEM.Enrichment.SingularityResolution;
using MGroup.XFEM.Entities;
using MGroup.XFEM.Geometry;
using MGroup.XFEM.Geometry.ConformingMesh;
using MGroup.XFEM.Geometry.LSM;
using MGroup.XFEM.Geometry.Primitives;
using MGroup.XFEM.Geometry.Tolerances;
using MGroup.XFEM.Integration;
using MGroup.XFEM.Materials;
using MGroup.XFEM.Plotting;
using MGroup.XFEM.Plotting.Mesh;
using MGroup.XFEM.Plotting.Writers;

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

        private const double xMin = -1.0, xMax = 1.0, yMin = -1, yMax = 1.0;
        private const double thickness = 1.0;

        // There are 2 or more inclusions in the same element
        private const int numElementsX = 15, numElementsY = 15;
        private const int numBallsX = 2, numBallsY = 1;
        private const double ballRadius = 0.3;

        private const double zeroLevelSetTolerance = 1E-6;
        private const double singularityRelativeAreaTolerance = 1E-8;
        private const int subdomainID = 0;
        private const int defaultPhaseID = 0;

        private const int boundaryIntegrationOrder = 2;

        private const double conductMatrix = 1E0, conductInclusion = 1E5;
        private const double conductBoundaryMatrixInclusion = 1E1, conductBoundaryInclusionInclusion = 1E2;
        private const double specialHeatCoeff = 1.0;

        public static void PlotGeometry()
        {
            // Create model and LSM
            XModel model = CreateModelQuads(numElementsX, numElementsY);
            List<SimpleLsm2D> lsmCurves = InitializeLSM(model);

            // Plot original mesh and level sets
            PlotInclusionLevelSets(outputDirectory, "level_set", model, lsmCurves);

            // Plot intersections between level set curves and elements
            Dictionary<IXFiniteElement, List<LsmElementIntersection2D>> elementIntersections 
                = CalcIntersections(model, lsmCurves);
            var allIntersections = new List<LsmElementIntersection2D>();
            foreach (var intersections in elementIntersections.Values) allIntersections.AddRange(intersections);
            var intersectionPlotter = new Lsm2DElementIntersectionsPlotter(model, lsmCurves);
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
                if (element is MockElement2D mock) mock.IntegrationBulk = integrationBulk;
            }
            var integrationPlotter = new IntegrationPlotter2D(model);
            integrationPlotter.PlotBulkIntegrationPoints(pathIntegrationBulk);

            // Plot boundary integration points
            integrationPlotter.PlotBoundaryIntegrationPoints(pathIntegrationBoundary, boundaryIntegrationOrder);
        }

        public static void PlotGeometryAndEntities()
        {
            // Create physical model, LSM and phases
            XModel model = CreateModelQuads(numElementsX, numElementsY);
            List<SimpleLsm2D> lsmCurves = InitializeLSM(model);
            GeometricModel geometricModel = CreatePhases(model, lsmCurves);

            // Plot original mesh and level sets
            PlotInclusionLevelSets(outputDirectory, "level_set", model, lsmCurves);

            // Find and plot intersections between level set curves and elements
            geometricModel.InteractWithMesh();

            //TODO: The next intersections and conforming mesh should have been taken care by the geometric model. 
            //      Read them from there.
            Dictionary<IXFiniteElement, List<LsmElementIntersection2D>> elementIntersections
                = CalcIntersections(model, lsmCurves);
            var allIntersections = new List<LsmElementIntersection2D>();
            foreach (var intersections in elementIntersections.Values) allIntersections.AddRange(intersections);
            var intersectionPlotter = new Lsm2DElementIntersectionsPlotter(model, lsmCurves);
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
                if (element is MockElement2D mock) mock.IntegrationBulk = integrationBulk;
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

            double elementSize = (xMax - xMin) / numElementsX;
            var enrichmentPlotter = new EnrichmentPlotter(model, elementSize, false);
            enrichmentPlotter.PlotStepEnrichedNodes(pathStepEnrichedNodes);
            //enrichmentPlotter.PlotJunctionEnrichedNodes(pathJunctionEnrichedNodes);
        }

        private static Dictionary<IXFiniteElement, List<LsmElementIntersection2D>> CalcIntersections(
            XModel model, List<SimpleLsm2D> curves)
        {
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
                        element2D.Intersections.Add((IElementCurveIntersection2D)intersection);
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

        private static XModel CreateModelQuads(int numElementsX, int numElementsY)
        {
            var model = new XModel();
            model.Subdomains[subdomainID] = new XSubdomain(subdomainID);

            // Mesh generation
            var meshGen = new UniformMeshGenerator2D<XNode>(xMin, yMin, xMax, yMax, numElementsX, numElementsY);
            (IReadOnlyList<XNode> nodes, IReadOnlyList<CellConnectivity<XNode>> cells) =
                meshGen.CreateMesh((id, x, y, z) => new XNode(id, x, y, z));

            // Nodes
            foreach (XNode node in nodes) model.Nodes.Add(node);

            // Materials
            var matrixMaterial = new ThermalMaterial(conductMatrix, specialHeatCoeff);
            var inclusionMaterial = new ThermalMaterial(conductInclusion, specialHeatCoeff);
            var interfaceMaterial = new ThermalInterfaceMaterial(conductBoundaryMatrixInclusion);
            var materialField = new MatrixInclusionsMaterialField(matrixMaterial, inclusionMaterial, 
                conductBoundaryMatrixInclusion, conductBoundaryInclusionInclusion, defaultPhaseID);

            // Integration
            var stdQuadrature = GaussLegendre2D.GetQuadratureWithOrder(2, 2);
            var subcellQuadrature = TriangleQuadratureSymmetricGaussian.Order2Points3;
            var integrationBulk = new IntegrationWithConformingSubtriangles2D(stdQuadrature, subcellQuadrature);

            // Elements
            var elemFactory = new XThermalElement2DFactory(materialField, 1, integrationBulk, boundaryIntegrationOrder);
            for (int e = 0; e < cells.Count; ++e)
            {
                XThermalElement2D element = elemFactory.CreateElement(e, CellType.Quad4, cells[e].Vertices);
                model.Elements.Add(element);
                model.Subdomains[subdomainID].Elements.Add(element);
            }

            //for (int e = 0; e < cells.Count; ++e)
            //{
            //    var element = new MockElement2D(e, CellType.Quad4, cells[e].Vertices);
            //    model.Elements.Add(element);
            //    model.Subdomains[subdomainID].Elements.Add(element);
            //}

            model.ConnectDataStructures();
            return model;
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
            }
            return geometricModel;
        }

        private static List<SimpleLsm2D> InitializeLSM(XModel model)
        {
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

        internal static void PlotInclusionLevelSets(string directoryPath, string vtkFilenamePrefix,
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
