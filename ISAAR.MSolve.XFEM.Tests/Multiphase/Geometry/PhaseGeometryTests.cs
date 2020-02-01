using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Mesh.Generation;
using ISAAR.MSolve.Discretization.Mesh.Generation.Custom;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.Geometry.Shapes;
using ISAAR.MSolve.Materials;
using ISAAR.MSolve.XFEM.Multiphase.Elements;
using ISAAR.MSolve.XFEM.Multiphase.Enrichment;
using ISAAR.MSolve.XFEM.Multiphase.Entities;
using ISAAR.MSolve.XFEM.Multiphase.Geometry;
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

        public static void PlotPercolationPhasesInteractions()
        {
            var paths = new OutputPaths();
            paths.finiteElementMesh = @"C:\Users\Serafeim\Desktop\HEAT\Paper\Percolation\fe_mesh.vtk";
            paths.conformingMesh = @"C:\Users\Serafeim\Desktop\HEAT\Paper\Percolation\conforming_mesh.vtk";
            paths.phasesGeometry = @"C:\Users\Serafeim\Desktop\HEAT\Paper\Percolation\phases_geometry.vtk";
            paths.nodalPhases = @"C:\Users\Serafeim\Desktop\HEAT\Paper\Percolation\nodal_phases.vtk";
            paths.elementPhases = @"C:\Users\Serafeim\Desktop\HEAT\Paper\Percolation\element_phases.vtk";
            paths.stepEnrichedNodes = @"C:\Users\Serafeim\Desktop\HEAT\Paper\Percolation\step_enriched_nodes.vtk";
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
            PlotPhasesInteractions(generator.CreateSingleTetrisPhases, paths);

        }

        private static void PlotPhasesInteractions(Func<XModel, GeometricModel> genPhases, OutputPaths paths)
        {
            XModel physicalModel = CreatePhysicalModel();
            GeometricModel geometricModel = genPhases(physicalModel);
            geometricModel.FindConformingMesh();

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
            nodeEnricher.DefineStepEnrichments();
            nodeEnricher.EnrichNodes();

            var enrichmentPlotter = new EnrichmentPlotter(physicalModel, elementSize);
            enrichmentPlotter.PlotStepEnrichedNodes(paths.stepEnrichedNodes);
        }

        private static XModel CreatePhysicalModel()
        {
            var model = new XModel();
            model.Subdomains[subdomainID] = new XSubdomain(subdomainID);

            // Mesh generation
            var meshGen = new UniformMeshGenerator2D<XNode>(minX, minY, maxX, maxY, numElementsX, numElementsY);
            (IReadOnlyList<XNode> nodes, IReadOnlyList<CellConnectivity<XNode>> cells) =
                meshGen.CreateMesh((id, x, y, z) => new XNode(id, x, y));

            // Nodes
            foreach (XNode node in nodes) model.Nodes.Add(node);

            // Elements
            for (int e = 0; e < cells.Count; ++e)
            {
                var element = new MockQuad4(e, cells[e].Vertices);
                model.Elements.Add(element);
                model.Subdomains[subdomainID].Elements.Add(element);
            }

            model.ConnectDataStructures();
            return model;
        }

        private class OutputPaths
        {
            public string finiteElementMesh;
            public string conformingMesh;
            public string phasesGeometry;
            public string nodalPhases;
            public string elementPhases;
            public string stepEnrichedNodes;
        }
    }
}
