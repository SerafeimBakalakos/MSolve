using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ISAAR.MSolve.Analyzers;
using ISAAR.MSolve.Discretization;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Discretization.Integration.Quadratures;
using ISAAR.MSolve.Discretization.Mesh;
using ISAAR.MSolve.Discretization.Mesh.Generation;
using ISAAR.MSolve.Discretization.Mesh.Generation.Custom;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.Geometry.Shapes;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Problems;
using ISAAR.MSolve.Solvers.Direct;
using ISAAR.MSolve.XFEM.Multiphase.Elements;
using ISAAR.MSolve.XFEM.Multiphase.Enrichment;
using ISAAR.MSolve.XFEM.Multiphase.Entities;
using ISAAR.MSolve.XFEM.Multiphase.Geometry;
using ISAAR.MSolve.XFEM.Multiphase.Integration;
using ISAAR.MSolve.XFEM.Multiphase.Materials;
using ISAAR.MSolve.XFEM.Multiphase.Plotting;
using ISAAR.MSolve.XFEM.Multiphase.Plotting.Enrichments;
using ISAAR.MSolve.XFEM.Multiphase.Plotting.Fields;
using ISAAR.MSolve.XFEM.Multiphase.Plotting.Mesh;
using ISAAR.MSolve.XFEM.Multiphase.Plotting.Writers;

namespace ISAAR.MSolve.XFEM.Tests.Multiphase.Plotting
{
    public static class PhasePlots
    {
        private const int numElementsX = 50, numElementsY = 50;
        private const int subdomainID = 0;
        private const double minX = -1.0, minY = -1.0, maxX = 1.0, maxY = 1.0;
        private const double elementSize = (maxX - minX) / numElementsX;
        private const double thickness = 1.0;
        private static readonly PhaseGenerator generator = new PhaseGenerator(minX, maxX, numElementsX);
        private const bool integrationWithSubtriangles = true;
        private const double matrixConductivity = 1, inclusionConductivity = 10000/*4*/;
        private const double matrixInclusionInterfaceConductivity = 10/*2*/, inclusionInclusionInterfaceConductivity = 10000/*3*/;
        private const double specialHeatCoeff = 1.0;

        public static void PlotPercolationPhasesInteractions()
        {
            var paths = new OutputPaths();
            paths.finiteElementMesh = @"C:\Users\Serafeim\Desktop\HEAT\Paper\Percolation\fe_mesh.vtk";
            paths.conformingMesh = @"C:\Users\Serafeim\Desktop\HEAT\Paper\Percolation\conforming_mesh.vtk";
            paths.phasesGeometry = @"C:\Users\Serafeim\Desktop\HEAT\Paper\Percolation\phases_geometry.vtk";
            paths.nodalPhases = @"C:\Users\Serafeim\Desktop\HEAT\Paper\Percolation\nodal_phases.vtk";
            paths.elementPhases = @"C:\Users\Serafeim\Desktop\HEAT\Paper\Percolation\element_phases.vtk";
            paths.stepEnrichedNodes = @"C:\Users\Serafeim\Desktop\HEAT\Paper\Percolation\step_enriched_nodes.vtk";
            paths.junctionEnrichedNodes = @"C:\Users\Serafeim\Desktop\HEAT\Paper\Percolation\junction_enriched_nodes.vtk";
            paths.volumeIntegrationPoints = @"C:\Users\Serafeim\Desktop\HEAT\Paper\Percolation\volume_integration_points.vtk";
            paths.volumeIntegrationMesh = @"C:\Users\Serafeim\Desktop\HEAT\Paper\Percolation\volume_integration_mesh.vtk";
            paths.boundaryIntegrationPoints = @"C:\Users\Serafeim\Desktop\HEAT\Paper\Percolation\boundary_integration_points.vtk";
            paths.boundaryIntegrationCells = @"C:\Users\Serafeim\Desktop\HEAT\Paper\Percolation\boundary_integration_cells.vtk";
            paths.boundaryIntegrationVertices = @"C:\Users\Serafeim\Desktop\HEAT\Paper\Percolation\boundary_integration_vertices.vtk";
            paths.volumeIntegrationMaterials = @"C:\Users\Serafeim\Desktop\HEAT\Paper\Percolation\volume_integration_materials.vtk";
            paths.boundaryIntegrationMaterials = @"C:\Users\Serafeim\Desktop\HEAT\Paper\Percolation\boundary_integration_materials.vtk";
            paths.boundaryIntegrationPhaseJumps = @"C:\Users\Serafeim\Desktop\HEAT\Paper\Percolation\boundary_integration_phase_jumps.vtk";
            paths.temperatureField = @"C:\Users\Serafeim\Desktop\HEAT\Paper\Percolation\temperature_field.vtk";
            paths.heatFluxField = @"C:\Users\Serafeim\Desktop\HEAT\Paper\Percolation\heat_flux_field.vtk";
            PlotPhasesInteractions(generator.CreatePercolatedTetrisPhases, paths);
        }

        public static void PlotScatteredPhasesInteractions()
        {
            var paths = new OutputPaths();
            paths.finiteElementMesh = @"C:\Users\Serafeim\Desktop\HEAT\Paper\Scattered\fe_mesh.vtk";
            paths.conformingMesh = @"C:\Users\Serafeim\Desktop\HEAT\Paper\Scattered\conforming_mesh.vtk";
            paths.phasesGeometry = @"C:\Users\Serafeim\Desktop\HEAT\Paper\Scattered\phases_geometry.vtk";
            paths.nodalPhases = @"C:\Users\Serafeim\Desktop\HEAT\Paper\Scattered\nodal_phases.vtk";
            paths.elementPhases = @"C:\Users\Serafeim\Desktop\HEAT\Paper\Scattered\element_phases.vtk";
            paths.stepEnrichedNodes = @"C:\Users\Serafeim\Desktop\HEAT\Paper\Scattered\step_enriched_nodes.vtk";
            paths.volumeIntegrationPoints = @"C:\Users\Serafeim\Desktop\HEAT\Paper\Scattered\volume_integration_points.vtk";
            paths.volumeIntegrationMesh = @"C:\Users\Serafeim\Desktop\HEAT\Paper\Scattered\volume_integration_mesh.vtk";
            paths.boundaryIntegrationPoints = @"C:\Users\Serafeim\Desktop\HEAT\Paper\Scattered\boundary_integration_points.vtk";
            paths.boundaryIntegrationCells = @"C:\Users\Serafeim\Desktop\HEAT\Paper\Scattered\boundary_integration_cells.vtk";
            paths.boundaryIntegrationVertices = @"C:\Users\Serafeim\Desktop\HEAT\Paper\Scattered\boundary_integration_vertices.vtk";
            paths.volumeIntegrationMaterials = @"C:\Users\Serafeim\Desktop\HEAT\Paper\Scattered\volume_integration_materials.vtk";
            paths.boundaryIntegrationMaterials = @"C:\Users\Serafeim\Desktop\HEAT\Paper\Scattered\boundary_integration_materials.vtk";
            paths.boundaryIntegrationPhaseJumps = @"C:\Users\Serafeim\Desktop\HEAT\Paper\Scattered\boundary_integration_phase_jumps.vtk";
            paths.temperatureField = @"C:\Users\Serafeim\Desktop\HEAT\Paper\Scattered\temperature_field.vtk";
            paths.heatFluxField = @"C:\Users\Serafeim\Desktop\HEAT\Paper\Scattered\heat_flux_field.vtk";
            PlotPhasesInteractions(generator.CreateScatterRectangularPhases, paths);
        }

        public static void PlotTetrisPhasesInteractions()
        {
            var paths = new OutputPaths();
            paths.finiteElementMesh = @"C:\Users\Serafeim\Desktop\HEAT\Paper\Tetris\fe_mesh.vtk";
            paths.conformingMesh = @"C:\Users\Serafeim\Desktop\HEAT\Paper\Tetris\conforming_mesh.vtk";
            paths.phasesGeometry = @"C:\Users\Serafeim\Desktop\HEAT\Paper\Tetris\phases_geometry.vtk";
            paths.nodalPhases = @"C:\Users\Serafeim\Desktop\HEAT\Paper\Tetris\nodal_phases.vtk";
            paths.elementPhases = @"C:\Users\Serafeim\Desktop\HEAT\Paper\Tetris\element_phases.vtk";
            paths.stepEnrichedNodes = @"C:\Users\Serafeim\Desktop\HEAT\Paper\Tetris\step_enriched_nodes.vtk";
            paths.junctionEnrichedNodes = @"C:\Users\Serafeim\Desktop\HEAT\Paper\Tetris\junction_enriched_nodes.vtk";
            paths.volumeIntegrationPoints = @"C:\Users\Serafeim\Desktop\HEAT\Paper\Tetris\volume_integration_points.vtk";
            paths.volumeIntegrationMesh = @"C:\Users\Serafeim\Desktop\HEAT\Paper\Tetris\volume_integration_mesh.vtk";
            paths.boundaryIntegrationPoints = @"C:\Users\Serafeim\Desktop\HEAT\Paper\Tetris\boundary_integration_points.vtk";
            paths.boundaryIntegrationCells = @"C:\Users\Serafeim\Desktop\HEAT\Paper\Tetris\boundary_integration_cells.vtk";
            paths.boundaryIntegrationVertices = @"C:\Users\Serafeim\Desktop\HEAT\Paper\Tetris\boundary_integration_vertices.vtk";
            paths.volumeIntegrationMaterials = @"C:\Users\Serafeim\Desktop\HEAT\Paper\Tetris\volume_integration_materials.vtk";
            paths.boundaryIntegrationMaterials = @"C:\Users\Serafeim\Desktop\HEAT\Paper\Tetris\boundary_integration_materials.vtk";
            paths.boundaryIntegrationPhaseJumps = @"C:\Users\Serafeim\Desktop\HEAT\Paper\Tetris\boundary_integration_phase_jumps.vtk";
            paths.temperatureField = @"C:\Users\Serafeim\Desktop\HEAT\Paper\Tetris\temperature_field.vtk";
            paths.heatFluxField = @"C:\Users\Serafeim\Desktop\HEAT\Paper\Tetris\heat_flux_field.vtk";
            PlotPhasesInteractions(generator.CreateSingleTetrisPhases, paths);
        }

        private static void PlotPhasesInteractions(Func<GeometricModel> genPhases, OutputPaths paths)
        {
            GeometricModel geometricModel = genPhases();
            XModel physicalModel = CreatePhysicalModel(geometricModel);
            PrepareForAnalysis(physicalModel, geometricModel);
            
            var feMesh = new ContinuousOutputMesh<XNode>(physicalModel.Nodes, physicalModel.Elements);
            using (var writer = new VtkFileWriter(paths.finiteElementMesh))
            {
                writer.WriteMesh(feMesh);
            }

            var phasePlotter = new PhasePlotter(physicalModel, geometricModel, -10);
            phasePlotter.PlotPhases(paths.phasesGeometry);

            var conformingMesh = new ConformingOutputMesh2D(geometricModel, physicalModel.Nodes, physicalModel.Elements);
            using (var writer = new VtkFileWriter(paths.conformingMesh))
            {
                writer.WriteMesh(conformingMesh);
            }

            phasePlotter.PlotNodes(paths.nodalPhases);
            phasePlotter.PlotElements(paths.elementPhases, conformingMesh);

            // Enrichment
            var enrichmentPlotter = new EnrichmentPlotter(physicalModel, elementSize);
            enrichmentPlotter.PlotStepEnrichedNodes(paths.stepEnrichedNodes);
            if (paths.junctionEnrichedNodes != null) enrichmentPlotter.PlotJunctionEnrichedNodes(paths.junctionEnrichedNodes);

            // Integration
            var integrationPlotter = new IntegrationMeshPlotter(physicalModel, geometricModel);
            integrationPlotter.PlotVolumeIntegrationMesh(paths.volumeIntegrationMesh);
            integrationPlotter.PlotVolumeIntegrationPoints(paths.volumeIntegrationPoints);
            integrationPlotter.PlotBoundaryIntegrationMesh(paths.boundaryIntegrationCells, paths.boundaryIntegrationVertices);
            integrationPlotter.PlotBoundaryIntegrationPoints(paths.boundaryIntegrationPoints);

            // Material
            var materialPlotter = new MaterialPlotter(physicalModel);
            materialPlotter.PlotVolumeMaterials(paths.volumeIntegrationMaterials);
            materialPlotter.PlotBoundaryMaterials(paths.boundaryIntegrationMaterials);
            materialPlotter.PlotBoundaryPhaseJumpCoefficients(paths.boundaryIntegrationPhaseJumps);

            // Analysis
            IVectorView solution = RunAnalysis(physicalModel);

            // Plot temperature
            using (var writer = new VtkFileWriter(paths.temperatureField))
            {
                var temperatureField = new TemperatureField2D(physicalModel, conformingMesh);
                writer.WriteMesh(conformingMesh);
                writer.WriteScalarField("temperature", conformingMesh, temperatureField.CalcValuesAtVertices(solution));
            }
        }

        private static XModel CreatePhysicalModel(GeometricModel geometricModel)
        {
            var physicalModel = new XModel();
            physicalModel.Subdomains[subdomainID] = new XSubdomain(subdomainID);

            // Mesh generation
            var meshGen = new UniformMeshGenerator2D<XNode>(minX, minY, maxX, maxY, numElementsX, numElementsY);
            (IReadOnlyList<XNode> nodes, IReadOnlyList<CellConnectivity<XNode>> cells) =
                meshGen.CreateMesh((id, x, y, z) => new XNode(id, x, y));

            // Nodes
            foreach (XNode node in nodes) physicalModel.Nodes.Add(node);

            // Integration
            IIntegrationStrategy volumeIntegration;
            if (integrationWithSubtriangles)
            {
                volumeIntegration = new IntegrationWithConformingSubtriangles2D(GaussLegendre2D.GetQuadratureWithOrder(2, 2),
                    TriangleQuadratureSymmetricGaussian.Order2Points3,
                    element => geometricModel.GetConformingTriangulationOf(element));
            }
            else
            {
                volumeIntegration = new IntegrationWithNonConformingSubsquares2D(
                    GaussLegendre2D.GetQuadratureWithOrder(2, 2), 8, GaussLegendre2D.GetQuadratureWithOrder(2, 2));
            }
            IBoundaryIntegration boundaryIntegration = new LinearBoundaryIntegration(GaussLegendre1D.GetQuadratureWithOrder(3));

            // Materials
            var matrixMaterial = new ThermalMaterial(matrixConductivity, specialHeatCoeff);
            var inclusionMaterial = new ThermalMaterial(inclusionConductivity, specialHeatCoeff);
            var materialField = new MatrixInclusionsMaterialField(matrixMaterial, inclusionMaterial,
                matrixInclusionInterfaceConductivity, inclusionInclusionInterfaceConductivity, 0);

            // Elements
            var factory = new XThermalElement2DFactory(materialField, thickness, volumeIntegration, boundaryIntegration);
            for (int e = 0; e < cells.Count; ++e)
            {
                XThermalElement2D element = factory.CreateElement(e, CellType.Quad4, cells[e].Vertices);
                physicalModel.Elements.Add(element);
                physicalModel.Subdomains[subdomainID].Elements.Add(element);
            }

            // Boundary conditions
            ApplyBoundaryConditions(physicalModel);

            return physicalModel;
        }

        private static void ApplyBoundaryConditions(XModel physicalModel)
        {
            double meshTol = 1E-7;

            // Left side: T = +100
            double minX = physicalModel.Nodes.Select(n => n.X).Min();
            foreach (var node in physicalModel.Nodes.Where(n => Math.Abs(n.X - minX) <= meshTol))
            {
                node.Constraints.Add(new Constraint() { DOF = ThermalDof.Temperature, Amount = +100 });
            }

            // Right side: T = 100
            double maxX = physicalModel.Nodes.Select(n => n.X).Max();
            foreach (var node in physicalModel.Nodes.Where(n => Math.Abs(n.X - maxX) <= meshTol))
            {
                node.Constraints.Add(new Constraint() { DOF = ThermalDof.Temperature, Amount = -100 });
            }

            // Node inside circle
            //XNode internalNode = model.Nodes.Where(n => (Math.Abs(n.X + 0.4) <= meshTol) && (Math.Abs(n.Y) <= meshTol)).First();
            //System.Diagnostics.Debug.Assert(internalNode != null);
            //internalNode.Constraints.Add(new Constraint() { DOF = ThermalDof.Temperature, Amount = 0.1 });
        }

        private static void PrepareForAnalysis(XModel physicalModel, GeometricModel geometricModel)
        {
            physicalModel.ConnectDataStructures();

            geometricModel.AssossiatePhasesNodes(physicalModel);
            geometricModel.AssociatePhasesElements(physicalModel);
            geometricModel.FindConformingMesh(physicalModel);

            var nodeEnricher = new NodeEnricher(geometricModel);
            nodeEnricher.ApplyEnrichments();

            physicalModel.UpdateDofs();
            physicalModel.UpdateMaterials();
        }

        private static IVectorView RunAnalysis(XModel physicalModel)
        {
            SkylineSolver solver = new SkylineSolver.Builder().BuildSolver(physicalModel);
            var problem = new ProblemThermalSteadyState(physicalModel, solver);
            var linearAnalyzer = new LinearAnalyzer(physicalModel, solver, problem);
            var staticAnalyzer = new StaticAnalyzer(physicalModel, solver, problem, linearAnalyzer);

            staticAnalyzer.Initialize();
            staticAnalyzer.Solve();

            return solver.LinearSystems[subdomainID].Solution;
        }

        private class OutputPaths
        {
            public string finiteElementMesh;
            public string conformingMesh;
            public string phasesGeometry;
            public string nodalPhases;
            public string elementPhases;
            public string stepEnrichedNodes;
            public string junctionEnrichedNodes;
            public string volumeIntegrationPoints;
            public string volumeIntegrationMesh;
            public string boundaryIntegrationPoints;
            public string boundaryIntegrationCells;
            public string boundaryIntegrationVertices;
            public string volumeIntegrationMaterials;
            public string boundaryIntegrationMaterials;
            public string boundaryIntegrationPhaseJumps;
            public string temperatureField;
            public string heatFluxField;

        }
    }
}
