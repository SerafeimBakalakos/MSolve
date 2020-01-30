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
    public static class PhasesTests
    {
        private const string pathConformingMesh = @"C:\Users\Serafeim\Desktop\HEAT\Paper\conforming_mesh.vtk";
        private const string pathElementPhases = @"C:\Users\Serafeim\Desktop\HEAT\Paper\element_phases.vtk";
        private const string pathNodalPhases = @"C:\Users\Serafeim\Desktop\HEAT\Paper\nodal_phases.vtk";
        private const string pathPhases = @"C:\Users\Serafeim\Desktop\HEAT\Paper\phases.vtk";

        private const int numElementsX = 100, numElementsY = 100;
        private const double thickness = 1.0;
        private const double zeroLevelSetTolerance = 1E-6;
        private const int subdomainID = 0;

        private const double minX = -1.0, minY = -1.0, maxX = 1.0, maxY = 1.0;
        private const double cntLength = 0.4, cntHeight = cntLength / 10;
        private const int numCNTs = 25;
        private const bool cntsCannotInteract = true;

        private const double conductivityMatrix = 1.0, conductivityInclusion = 1000.0;
        private const double interfaceResistance = 1E-2;

        public static void PlotPhaseInteractions()
        {
            XModel physicalModel = CreatePhysicalModel();
            GeometricModel geometricModel = CreateGeometricModel(physicalModel);
            geometricModel.FindConformingMesh();

            var plotter = new PhasePlotter(physicalModel, geometricModel, -10);
            plotter.PlotPhases(pathPhases);
            plotter.PlotNodes(pathNodalPhases);

            var conformingMesh = new ConformingOutputMesh2D(geometricModel, physicalModel.Nodes, physicalModel.Elements);
            plotter.PlotElements(pathElementPhases, conformingMesh);

            //// Plot conforming mesh
            //using (var writer = new VtkFileWriter(pathConformingMesh))
            //{
            //    writer.WriteMesh(conformingMesh);
            //}
        }

        private static GeometricModel CreateGeometricModel(XModel physicalModel)
        {
            var geometricModel = new GeometricModel(physicalModel);
            geometricModel.MeshTolerance = new UsedDefinedMeshTolerance(elementSize: (maxX - minX) / numElementsX);
            var defaultPhase = new DefaultPhase();
            geometricModel.Phases.Add(defaultPhase);
            List<Rectangle2D> rectangles = ScatterDisjointCNTs();
            int phaseID = 1;
            foreach (Rectangle2D rect in rectangles)
            {
                var phase = new ConvexPhase(phaseID++);
                geometricModel.Phases.Add(phase);
                for (int i = 0; i < 4; ++i)
                {
                    CartesianPoint start = rect.Vertices[i];
                    CartesianPoint end = rect.Vertices[(i + 1) % 4];
                    var segment = new XFEM.Multiphase.Geometry.LineSegment2D(start, end, thickness);
                    var boundary = new PhaseBoundary(segment);
                    phase.Boundaries.Add(boundary);
                    boundary.PositivePhase = phase; // The vertices are in anti-clockwise order
                    boundary.NegativePhase = defaultPhase;
                }
            }
            geometricModel.AssossiatePhasesNodes();
            geometricModel.AssociatePhasesElements();
            return geometricModel;
        }

        private static XModel CreatePhysicalModel()
        {
            var model = new XModel();
            model.Subdomains[subdomainID] = new XSubdomain(subdomainID);

            // Materials
            double density = 1.0;
            double specificHeat = 1.0;
            var materialPos = new ThermalMaterial(density, specificHeat, conductivityMatrix);
            var materialNeg = new ThermalMaterial(density, specificHeat, conductivityInclusion);
            //var materialField = new ThermalBiMaterialField2D(materialPos, materialNeg, geometricModel);

            // Mesh generation
            var meshGen = new UniformMeshGenerator2D<XNode>(minX, minY, maxX, maxY, numElementsX, numElementsY);
            (IReadOnlyList<XNode> nodes, IReadOnlyList<CellConnectivity<XNode>> cells) =
                meshGen.CreateMesh((id, x, y, z) => new XNode(id, x, y));

            // Nodes
            foreach (XNode node in nodes) model.Nodes.Add(node);

            // Elements
            //var integrationForConductivity = new RectangularSubgridIntegration2D<XThermalElement2D>(8,
            //    GaussLegendre2D.GetQuadratureWithOrder(2, 2));
            //int numGaussPointsInterface = 2;
            //var elementFactory = new XThermalElement2DFactory(materialField, thickness, integrationForConductivity,
            //    numGaussPointsInterface);
            for (int e = 0; e < cells.Count; ++e)
            {
                //var element = elementFactory.CreateElement(e, cells[e].CellType, cells[e].Vertices);
                var element = new MockQuad4(e, cells[e].Vertices);
                model.Elements.Add(element);
                model.Subdomains[subdomainID].Elements.Add(element);
            }

            //ApplyBoundaryConditions(model);
            model.ConnectDataStructures();
            return model;
        }

        private static List<Rectangle2D> ScatterDisjointCNTs()
        {
            int seed = 25;
            var rng = new Random(seed);
            var cnts = new List<Rectangle2D>();
            cnts.Add(GenerateRectangle(rng));
            for (int i = 1; i < numCNTs; ++i)
            {
                //Console.WriteLine("Trying new CNT");
                Rectangle2D newCNT = null;
                do
                {
                    newCNT = GenerateRectangle(rng);
                }
                while (cntsCannotInteract && InteractsWithOtherCNTs(newCNT, cnts));
                cnts.Add(newCNT);
            }
            return cnts;
        }

        private static Rectangle2D GenerateRectangle(Random rng)
        {
            //double lbX = minX + 0.5 * cntLength, ubX = maxX - 0.5 * cntLength;
            //double lbY = minY + 0.5 * cntLength, ubY = maxY - 0.5 * cntLength;
            double lbX = minX, ubX = maxX;
            double lbY = minY, ubY = maxY;

            double centroidX = lbX + (ubX - lbX) * rng.NextDouble();
            double centroidY = lbY + (ubY - lbY) * rng.NextDouble();
            double angle = Math.PI * rng.NextDouble();
            return new Rectangle2D(new CartesianPoint(centroidX, centroidY), cntLength, cntHeight, angle);
        }

        private static bool InteractsWithOtherCNTs(Rectangle2D newCNT, List<Rectangle2D> currentCNTs)
        {
            var scaledCNT = newCNT.ScaleRectangle();
            foreach (Rectangle2D cnt in currentCNTs)
            {
                if (!cnt.ScaleRectangle().IsDisjointFrom(scaledCNT))
                {
                    //Console.WriteLine("It interacts with an existing one");
                    return true;
                }
            }
            return false;
        }

        private static Rectangle2D ScaleRectangle(this Rectangle2D rectangle)
        {
            double scaleFactor = 1.2;
            return new Rectangle2D(rectangle.Centroid,
                scaleFactor * rectangle.LengthAxis0, scaleFactor * rectangle.LengthAxis1, rectangle.Axis0Angle);
        }
    }
}
