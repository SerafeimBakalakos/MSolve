using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Mesh.Generation;
using ISAAR.MSolve.Discretization.Mesh.Generation.Custom;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.Geometry.Shapes;
using ISAAR.MSolve.Materials;
using ISAAR.MSolve.XFEM.Multiphase.Elements;
using ISAAR.MSolve.XFEM.Multiphase.Entities;
using ISAAR.MSolve.XFEM.Multiphase.Geometry;
using ISAAR.MSolve.XFEM.Multiphase.Output;
using ISAAR.MSolve.XFEM.Multiphase.Output.Mesh;
using ISAAR.MSolve.XFEM.Multiphase.Output.Writers;

namespace ISAAR.MSolve.XFEM.Tests.Multiphase.Geometry
{
    public static class PhaseGeometryTests
    {
        private const int numElementsX = 50, numElementsY = 50;
        private const int subdomainID = 0;
        private const double minX = -1.0, minY = -1.0, maxX = 1.0, maxY = 1.0;

        public static void PlotScatteredPhasesInteractions()
        {
            string pathConformingMesh = @"C:\Users\Serafeim\Desktop\HEAT\Paper\Scattered\conforming_mesh.vtk";
            string pathElementPhases = @"C:\Users\Serafeim\Desktop\HEAT\Paper\Scattered\element_phases.vtk";
            string pathNodalPhases = @"C:\Users\Serafeim\Desktop\HEAT\Paper\Scattered\nodal_phases.vtk";
            string pathPhases = @"C:\Users\Serafeim\Desktop\HEAT\Paper\Scattered\phases.vtk";

            XModel physicalModel = CreatePhysicalModel();
            var generator = new PhaseGenerator(minX, maxX, numElementsX);
            GeometricModel geometricModel = generator.CreateScatterRectangularPhases(physicalModel);
            geometricModel.FindConformingMesh();

            var plotter = new PhasePlotter(physicalModel, geometricModel, -10);
            plotter.PlotPhases(pathPhases);
            plotter.PlotNodes(pathNodalPhases);

            var conformingMesh = new ConformingOutputMesh2D(geometricModel, physicalModel.Nodes, physicalModel.Elements);
            plotter.PlotElements(pathElementPhases, conformingMesh);
        }

        public static void PlotTetrisPhasesInteractions()
        {
            string pathConformingMesh = @"C:\Users\Serafeim\Desktop\HEAT\Paper\Tetris\conforming_mesh.vtk";
            string pathElementPhases = @"C:\Users\Serafeim\Desktop\HEAT\Paper\Tetris\element_phases.vtk";
            string pathNodalPhases = @"C:\Users\Serafeim\Desktop\HEAT\Paper\Tetris\nodal_phases.vtk";
            string pathPhases = @"C:\Users\Serafeim\Desktop\HEAT\Paper\Tetris\phases.vtk";

            XModel physicalModel = CreatePhysicalModel();
            var generator = new PhaseGenerator(minX, maxX, numElementsX);
            GeometricModel geometricModel = generator.CreateSingleTetrisPhases(physicalModel);
            geometricModel.FindConformingMesh();

            var plotter = new PhasePlotter(physicalModel, geometricModel, -10);
            plotter.PlotPhases(pathPhases);
            plotter.PlotNodes(pathNodalPhases);

            var conformingMesh = new ConformingOutputMesh2D(geometricModel, physicalModel.Nodes, physicalModel.Elements);
            plotter.PlotElements(pathElementPhases, conformingMesh);
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
    }
}
