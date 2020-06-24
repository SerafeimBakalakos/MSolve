﻿using System;
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

namespace MGroup.XFEM.Tests.Plotting
{
    public static class LsmBalls3DExamples
    {
        private const string outputDirectory = @"C:\Users\Serafeim\Desktop\HEAT\2020\Spheres3D\";
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

        private const double xMin = -1.0, xMax = 1.0, yMin = -1, yMax = 1.0, zMin = -1.0, zMax = +1.0;

        // There are 2 or more inclusions in the same element
        private const int numElementsX = 20, numElementsY = 20, numElementsZ = 20;
        private const int numBallsX = 2, numBallsY = 1, numBallsZ = 1;
        private const double ballRadius = 0.3;

        private const double zeroLevelSetTolerance = 1E-6;
        private const int subdomainID = 0;
        private const int defaultPhaseID = 0;
        
        private const int boundaryIntegrationOrder = 2;

        private const double conductMatrix = 1E0, conductInclusion = 1E5;
        private const double conductBoundaryMatrixInclusion = 1E1, conductBoundaryInclusionInclusion = 1E2;
        private const double specialHeatCoeff = 1.0;

        public static void PlotGeometry()
        {
            // Create model and LSM
            XModel model = CreateModel(numElementsX, numElementsY, numElementsZ);
            List<SimpleLsm3D> lsmSurfaces = InitializeLSM(model);

            // Plot original mesh and level sets
            PlotInclusionLevelSets(outputDirectory, "level_set", model, lsmSurfaces);

            // Plot intersections between level set curves and elements
            Dictionary<IXFiniteElement, List<LsmElementIntersection3D>> elementIntersections
                = CalcIntersections(model, lsmSurfaces);
            var allIntersections = new List<LsmElementIntersection3D>();
            foreach (var intersections in elementIntersections.Values) allIntersections.AddRange(intersections);
            var intersectionPlotter = new Lsm3DElementIntersectionsPlotter(model, lsmSurfaces);
            intersectionPlotter.PlotIntersections(pathIntersections, allIntersections);

            // Plot conforming mesh
            Dictionary<IXFiniteElement, ElementSubtetrahedron3D[]> triangulation = CreateConformingMesh(elementIntersections);
            var conformingMesh = new ConformingOutputMesh3D(model.Nodes, model.Elements, triangulation);
            using (var writer = new VtkFileWriter(pathConformingMesh))
            {
                writer.WriteMesh(conformingMesh);
            }

            // Plot bulk integration points
            var integrationBulk = new IntegrationWithConformingSubtetrahedra3D(GaussLegendre3D.GetQuadratureWithOrder(2, 2, 2),
                TetrahedronQuadrature.Order2Points4);
            foreach (IXFiniteElement element in model.Elements)
            {
                if (element is MockElement3D mock) mock.IntegrationBulk = integrationBulk;
            }
            var integrationPlotter = new IntegrationPlotter3D(model);
            integrationPlotter.PlotBulkIntegrationPoints(pathIntegrationBulk);

            // Plot boundary integration points
            integrationPlotter.PlotBoundaryIntegrationPoints(pathIntegrationBoundary, boundaryIntegrationOrder);

            // Plot boundary integration points
            integrationPlotter.PlotBoundaryIntegrationPoints(pathIntegrationBoundary, boundaryIntegrationOrder);
        }

        public static void PlotGeometryAndEntities()
        {
            // Create model and LSM
            XModel model = CreateModel(numElementsX, numElementsY, numElementsZ);
            List<SimpleLsm3D> lsmSurfaces = InitializeLSM(model);
            GeometricModel geometricModel = CreatePhases(model, lsmSurfaces);

            // Plot original mesh and level sets
            PlotInclusionLevelSets(outputDirectory, "level_set", model, lsmSurfaces);

            // Find and plot intersections between level set curves and elements
            geometricModel.InteractWithMesh();

            //TODO: The next intersections and conforming mesh should have been taken care by the geometric model. 
            //      Read them from there.
            Dictionary<IXFiniteElement, List<LsmElementIntersection3D>> elementIntersections
                = CalcIntersections(model, lsmSurfaces);
            var allIntersections = new List<LsmElementIntersection3D>();
            foreach (var intersections in elementIntersections.Values) allIntersections.AddRange(intersections);
            var intersectionPlotter = new Lsm3DElementIntersectionsPlotter(model, lsmSurfaces);
            intersectionPlotter.PlotIntersections(pathIntersections, allIntersections);

            // Plot conforming mesh
            Dictionary<IXFiniteElement, ElementSubtetrahedron3D[]> triangulation = CreateConformingMesh(elementIntersections);
            var conformingMesh = new ConformingOutputMesh3D(model.Nodes, model.Elements, triangulation);
            using (var writer = new VtkFileWriter(pathConformingMesh))
            {
                writer.WriteMesh(conformingMesh);
            }

            // Plot bulk integration points
            var integrationBulk = new IntegrationWithConformingSubtetrahedra3D(GaussLegendre3D.GetQuadratureWithOrder(2, 2, 2),
                TetrahedronQuadrature.Order2Points4);
            foreach (IXFiniteElement element in model.Elements)
            {
                if (element is MockElement3D mock) mock.IntegrationBulk = integrationBulk;
            }
            var integrationPlotter = new IntegrationPlotter3D(model);
            integrationPlotter.PlotBulkIntegrationPoints(pathIntegrationBulk);

            // Plot boundary integration points
            integrationPlotter.PlotBoundaryIntegrationPoints(pathIntegrationBoundary, boundaryIntegrationOrder);

            // Plot boundary integration points
            integrationPlotter.PlotBoundaryIntegrationPoints(pathIntegrationBoundary, boundaryIntegrationOrder);

            // Plot phases
            var phasePlotter = new PhasePlotter3D(model, geometricModel, defaultPhaseID);
            phasePlotter.PlotNodes(pathPhasesOfNodes);
            phasePlotter.PlotElements(pathPhasesOfElements, conformingMesh);

            // Enrichment
            ISingularityResolver singularityResolver = new NullSingularityResolver();
            var nodeEnricher = new NodeEnricherMultiphase(geometricModel, singularityResolver);
            nodeEnricher.ApplyEnrichments();
            model.UpdateDofs();
            model.UpdateMaterials();

            double elementSize = (xMax - xMin) / numElementsX;
            var enrichmentPlotter = new EnrichmentPlotter(model, elementSize, true);
            enrichmentPlotter.PlotStepEnrichedNodes(pathStepEnrichedNodes);
            //enrichmentPlotter.PlotJunctionEnrichedNodes(pathJunctionEnrichedNodes);
        }

        public static void PlotSolution()
        {
            // Create model and LSM
            XModel model = CreateModel(numElementsX, numElementsY, numElementsZ);
            List<SimpleLsm3D> lsmSurfaces = InitializeLSM(model);
            GeometricModel geometricModel = CreatePhases(model, lsmSurfaces);

            // Plot original mesh and level sets
            PlotInclusionLevelSets(outputDirectory, "level_set", model, lsmSurfaces);

            // Find and plot intersections between level set curves and elements
            geometricModel.InteractWithMesh();

            //TODO: The next intersections and conforming mesh should have been taken care by the geometric model. 
            //      Read them from there.
            Dictionary<IXFiniteElement, List<LsmElementIntersection3D>> elementIntersections
                = CalcIntersections(model, lsmSurfaces);
            var allIntersections = new List<LsmElementIntersection3D>();
            foreach (var intersections in elementIntersections.Values) allIntersections.AddRange(intersections);
            var intersectionPlotter = new Lsm3DElementIntersectionsPlotter(model, lsmSurfaces);
            intersectionPlotter.PlotIntersections(pathIntersections, allIntersections);

            // Plot conforming mesh
            Dictionary<IXFiniteElement, ElementSubtetrahedron3D[]> triangulation = CreateConformingMesh(elementIntersections);
            var conformingMesh = new ConformingOutputMesh3D(model.Nodes, model.Elements, triangulation);
            using (var writer = new VtkFileWriter(pathConformingMesh))
            {
                writer.WriteMesh(conformingMesh);
            }

            // Plot phases
            var phasePlotter = new PhasePlotter3D(model, geometricModel, defaultPhaseID);
            phasePlotter.PlotNodes(pathPhasesOfNodes);
            phasePlotter.PlotElements(pathPhasesOfElements, conformingMesh);

            // Plot bulk integration points
            var integrationBulk = new IntegrationWithConformingSubtetrahedra3D(GaussLegendre3D.GetQuadratureWithOrder(2, 2, 2),
                TetrahedronQuadrature.Order2Points4);
            foreach (IXFiniteElement element in model.Elements)
            {
                if (element is MockElement3D mock) mock.IntegrationBulk = integrationBulk;
            }
            var integrationPlotter = new IntegrationPlotter3D(model);
            integrationPlotter.PlotBulkIntegrationPoints(pathIntegrationBulk);

            // Plot boundary integration points
            integrationPlotter.PlotBoundaryIntegrationPoints(pathIntegrationBoundary, boundaryIntegrationOrder);

            // Plot boundary integration points
            integrationPlotter.PlotBoundaryIntegrationPoints(pathIntegrationBoundary, boundaryIntegrationOrder);

            // Enrichment
            ISingularityResolver singularityResolver = new NullSingularityResolver();
            var nodeEnricher = new NodeEnricherMultiphase(geometricModel, singularityResolver);
            nodeEnricher.ApplyEnrichments();
            model.UpdateDofs();
            model.UpdateMaterials();

            double elementSize = (xMax - xMin) / numElementsX;
            var enrichmentPlotter = new EnrichmentPlotter(model, elementSize, true);
            enrichmentPlotter.PlotStepEnrichedNodes(pathStepEnrichedNodes);
            //enrichmentPlotter.PlotJunctionEnrichedNodes(pathJunctionEnrichedNodes);


            // Run analysis and plot temperature and heat flux
            IVectorView solution = RunAnalysis(model);

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
                var temperatureField = new TemperatureField3D(model, conformingMesh);
                writer.WriteMesh(conformingMesh);
                writer.WriteScalarField("temperature", conformingMesh, temperatureField.CalcValuesAtVertices(solution));
            }
            using (var writer = new VtkPointWriter(pathHeatFluxAtGPs))
            {
                var fluxField = new HeatFluxAtGaussPointsField3D(model);
                writer.WriteVectorField("heat_flux", fluxField.CalcValuesAtVertices(solution));
            }
        }

        private static Dictionary<IXFiniteElement, List<LsmElementIntersection3D>> CalcIntersections(
            XModel model, List<SimpleLsm3D> surfaces)
        {
            var intersections = new Dictionary<IXFiniteElement, List<LsmElementIntersection3D>>();
            foreach (IXFiniteElement element in model.Elements)
            {
                var element3D = (IXFiniteElement3D)element;
                var elementIntersections = new List<LsmElementIntersection3D>();
                foreach (IImplicitGeometry surface in surfaces)
                {
                    IElementGeometryIntersection intersection = surface.Intersect(element);
                    if (intersection.RelativePosition != RelativePositionCurveElement.Disjoint)
                    {
                        element3D.Intersections.Add((IElementSurfaceIntersection3D)intersection);
                        elementIntersections.Add((LsmElementIntersection3D)intersection);
                    }
                }
                if (elementIntersections.Count > 0) intersections.Add(element, elementIntersections);
            }
            return intersections;
        }

        private static Dictionary<IXFiniteElement, ElementSubtetrahedron3D[]> CreateConformingMesh(
            Dictionary<IXFiniteElement, List<LsmElementIntersection3D>> intersections)
        {
            var tolerance = new ArbitrarySideMeshTolerance();
            var triangulator = new ConformingTriangulator3D();
            var conformingMesh = new Dictionary<IXFiniteElement, ElementSubtetrahedron3D[]>();
            foreach (IXFiniteElement element in intersections.Keys)
            {
                var element3D = (IXFiniteElement3D)element;
                List<LsmElementIntersection3D> elementIntersections = intersections[element];
                ElementSubtetrahedron3D[] subtetra = triangulator.FindConformingMesh(element, elementIntersections, tolerance);
                conformingMesh[element] = subtetra;
                element3D.ConformingSubtetrahedra = subtetra;
            }
            return conformingMesh;
        }

        private static GeometricModel CreatePhases(XModel model, List<SimpleLsm3D> lsmSurfaces)
        {
            var geometricModel = new GeometricModel(3, model);
            var defaultPhase = new DefaultPhase(defaultPhaseID);
            geometricModel.Phases.Add(defaultPhase);
            for (int p = 0; p < lsmSurfaces.Count; ++p)
            {
                SimpleLsm3D curve = lsmSurfaces[p];
                var phase = new ConvexPhase(p + 1, geometricModel);
                geometricModel.Phases.Add(phase);
                var boundary = new PhaseBoundary(curve, defaultPhase, phase);
            }
            return geometricModel;
        }

        private static XModel CreateModel(int numElementsX, int numElementsY, int numElementsZ)
        {
            var model = new XModel();
            model.Subdomains[subdomainID] = new XSubdomain(subdomainID);

            // Mesh generation
            var meshGen = new UniformMeshGenerator3D<XNode>(xMin, yMin, zMin, xMax, yMax, zMax, 
                numElementsX, numElementsY, numElementsZ);
            (IReadOnlyList<XNode> nodes, IReadOnlyList<CellConnectivity<XNode>> cells) =
                meshGen.CreateMesh((id, x, y, z) => new XNode(id, new double[] { x, y, z }));

            // Nodes
            foreach (XNode node in nodes) model.Nodes.Add(node);

            // Materials
            var matrixMaterial = new ThermalMaterial(conductMatrix, specialHeatCoeff);
            var inclusionMaterial = new ThermalMaterial(conductInclusion, specialHeatCoeff);
            var interfaceMaterial = new ThermalInterfaceMaterial(conductBoundaryMatrixInclusion);
            var materialField = new MatrixInclusionsMaterialField(matrixMaterial, inclusionMaterial,
                conductBoundaryMatrixInclusion, conductBoundaryInclusionInclusion, defaultPhaseID);

            // Integration
            var stdQuadrature = GaussLegendre3D.GetQuadratureWithOrder(3, 3, 3);
            var subcellQuadrature = TetrahedronQuadrature.Order2Points4;
            var integrationBulk = new IntegrationWithConformingSubtetrahedra3D(stdQuadrature, subcellQuadrature);

            // Elements
            var elemFactory = new XThermalElement3DFactory(materialField, integrationBulk, boundaryIntegrationOrder);
            for (int e = 0; e < cells.Count; ++e)
            {
                XThermalElement3D element = elemFactory.CreateElement(e, CellType.Hexa8, cells[e].Vertices);
                model.Elements.Add(element);
                model.Subdomains[subdomainID].Elements.Add(element);
            }

            //for (int e = 0; e < cells.Count; ++e)
            //{
            //    var element = new MockElement3D(e, CellType.Hexa8, cells[e].Vertices);
            //    model.Elements.Add(element);
            //    model.Subdomains[subdomainID].Elements.Add(element);
            //}


            // Boundary conditions
            double meshTol = 1E-7;

            // Left side: T = +100
            double minX = model.Nodes.Select(n => n.X).Min();
            foreach (var node in model.Nodes.Where(n => Math.Abs(n.X - minX) <= meshTol))
            {
                node.Constraints.Add(new Constraint() { DOF = ThermalDof.Temperature, Amount = +100 });
            }

            // Right side: T = 100
            double maxX = model.Nodes.Select(n => n.X).Max();
            foreach (var node in model.Nodes.Where(n => Math.Abs(n.X - maxX) <= meshTol))
            {
                node.Constraints.Add(new Constraint() { DOF = ThermalDof.Temperature, Amount = -100 });
            }

            model.ConnectDataStructures();
            return model;
        }
        
        private static List<SimpleLsm3D> InitializeLSM(XModel model)
        {
            var surfaces = new List<SimpleLsm3D>(numBallsX * numBallsY * numBallsZ);
            double dx = (xMax - xMin) / (numBallsX + 1);
            double dy = (yMax - yMin) / (numBallsY + 1);
            double dz = (zMax - zMin) / (numBallsZ + 1);
            for (int i = 0; i < numBallsX; ++i)
            {
                double centerX = xMin + (i + 1) * dx;
                for (int j = 0; j < numBallsY; ++j)
                {
                    double centerY = yMin + (j + 1) * dy;
                    for (int k = 0; k < numBallsZ; ++k)
                    {
                        double centerZ = zMin + (k + 1) * dz;
                        var sphere = new Sphere(centerX, centerY, centerZ, ballRadius);
                        var lsm = new SimpleLsm3D(model, sphere);
                        surfaces.Add(lsm);
                    }
                }
            }

            return surfaces;
        }

        internal static void PlotInclusionLevelSets(string directoryPath, string vtkFilenamePrefix,
            XModel model, IList<SimpleLsm3D> lsmCurves)
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

        private static IVectorView RunAnalysis(XModel model)
        {
            Console.WriteLine("Starting analysis");
            SuiteSparseSolver solver = new SuiteSparseSolver.Builder().BuildSolver(model);
            //SkylineSolver solver = new SkylineSolver.Builder().BuildSolver(model);
            var problem = new ProblemThermalSteadyState(model, solver);
            var linearAnalyzer = new LinearAnalyzer(model, solver, problem);
            var staticAnalyzer = new StaticAnalyzer(model, solver, problem, linearAnalyzer);

            staticAnalyzer.Initialize();
            staticAnalyzer.Solve();

            Console.WriteLine("Analysis finished");
            return solver.LinearSystems[subdomainID].Solution;
        }
    }
}
