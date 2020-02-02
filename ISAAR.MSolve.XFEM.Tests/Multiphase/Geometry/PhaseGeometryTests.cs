using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Integration.Quadratures;
using ISAAR.MSolve.Discretization.Mesh.Generation;
using ISAAR.MSolve.Discretization.Mesh.Generation.Custom;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.Geometry.Shapes;
using ISAAR.MSolve.Materials;
using ISAAR.MSolve.XFEM.Multiphase.Elements;
using ISAAR.MSolve.XFEM.Multiphase.Enrichment;
using ISAAR.MSolve.XFEM.Multiphase.Entities;
using ISAAR.MSolve.XFEM.Multiphase.Geometry;
using ISAAR.MSolve.XFEM.Multiphase.Integration;
using ISAAR.MSolve.XFEM.Multiphase.Output;
using ISAAR.MSolve.XFEM.Multiphase.Output.Enrichments;
using ISAAR.MSolve.XFEM.Multiphase.Output.Mesh;
using ISAAR.MSolve.XFEM.Multiphase.Output.Writers;

namespace ISAAR.MSolve.XFEM.Tests.Multiphase.Geometry
{
    public static class PhaseGeometryTests
    {
        private const int numElementsX = 50, numElementsY = 50;
        private const int subdomainID = 0;
        private const double minX = -1.0, minY = -1.0, maxX = 1.0, maxY = 1.0;
        private const double elementSize = (maxX - minX) / numElementsX;
        private static readonly PhaseGenerator generator = new PhaseGenerator(minX, maxX, numElementsX);
        private const bool integrationWithSubtriangles = true;

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
            paths.integrationPoints = @"C:\Users\Serafeim\Desktop\HEAT\Paper\Percolation\integration_points.vtk";
            paths.integrationMesh = @"C:\Users\Serafeim\Desktop\HEAT\Paper\Percolation\integration_mesh.vtk";
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
            paths.integrationPoints = @"C:\Users\Serafeim\Desktop\HEAT\Paper\Scattered\integration_points.vtk";
            paths.integrationMesh = @"C:\Users\Serafeim\Desktop\HEAT\Paper\Scattered\integration_mesh.vtk";
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
            paths.integrationPoints = @"C:\Users\Serafeim\Desktop\HEAT\Paper\Tetris\integration_points.vtk";
            paths.integrationMesh = @"C:\Users\Serafeim\Desktop\HEAT\Paper\Tetris\integration_mesh.vtk";
            PlotPhasesInteractions(generator.CreateSingleTetrisPhases, paths);

        }

        private static void PlotPhasesInteractions(Func<GeometricModel> genPhases, OutputPaths paths)
        {
            GeometricModel geometricModel = genPhases();
            XModel physicalModel = CreatePhysicalModel(geometricModel);

            geometricModel.AssossiatePhasesNodes(physicalModel);
            geometricModel.AssociatePhasesElements(physicalModel);
            geometricModel.FindConformingMesh(physicalModel);

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
            var nodeEnricher = new NodeEnricher(geometricModel);
            nodeEnricher.ApplyEnrichments();
            var enrichmentPlotter = new EnrichmentPlotter(physicalModel, elementSize);
            enrichmentPlotter.PlotStepEnrichedNodes(paths.stepEnrichedNodes);
            if (paths.junctionEnrichedNodes != null) enrichmentPlotter.PlotJunctionEnrichedNodes(paths.junctionEnrichedNodes);

            // Integration
            var integrationPlotter = new IntegrationMeshPlotter(physicalModel, geometricModel);
            integrationPlotter.PlotIntegrationMesh(paths.integrationMesh);
            integrationPlotter.PlotIntegrationPoints(paths.integrationPoints);
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
            IIntegrationStrategy integrationStrategy;
            if (integrationWithSubtriangles)
            {
                integrationStrategy = new IntegrationWithConformingSubtriangles2D(
                    GaussLegendre2D.GetQuadratureWithOrder(2, 2), geometricModel, 
                    TriangleQuadratureSymmetricGaussian.Order2Points3);
            }
            else
            {
                integrationStrategy = new IntegrationWithNonConformingSubsquares2D(
                    GaussLegendre2D.GetQuadratureWithOrder(2, 2), 8, GaussLegendre2D.GetQuadratureWithOrder(2, 2));
            }

            // Elements
            for (int e = 0; e < cells.Count; ++e)
            {
                var element = new MockQuad4(e, cells[e].Vertices);
                element.IntegrationStrategy = integrationStrategy;
                physicalModel.Elements.Add(element);
                physicalModel.Subdomains[subdomainID].Elements.Add(element);
            }

            physicalModel.ConnectDataStructures();
            return physicalModel;
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
            public string integrationPoints;
            public string integrationMesh;
        }
    }
}
