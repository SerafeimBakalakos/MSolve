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
using MGroup.XFEM.Entities;
using MGroup.XFEM.Geometry;
using MGroup.XFEM.Geometry.ConformingMesh;
using MGroup.XFEM.Geometry.LSM;
using MGroup.XFEM.Geometry.Primitives;
using MGroup.XFEM.Geometry.Tolerances;
using MGroup.XFEM.Integration;
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

        private const double xMin = -1.0, xMax = 1.0, yMin = -1, yMax = 1.0;
        private const double thickness = 1.0;

        // There are 2 or more inclusions in the same element
        private const int numElementsX = 15, numElementsY = 15;
        private const int numBallsX = 2, numBallsY = 1;
        private const double ballRadius = 0.3;

        private const double zeroLevelSetTolerance = 1E-6;
        private const int subdomainID = 0;


        public static void Run()
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
            Dictionary<IXFiniteElement, ElementSubtriangle2D[]> conformingMesh = CreateConformingMesh(elementIntersections);
            PlotConformingMesh(model, conformingMesh);

            // Plot bulk integration points
            var integrationBulk = new IntegrationWithConformingSubtriangles2D(GaussLegendre2D.GetQuadratureWithOrder(2, 2),
                TriangleQuadratureSymmetricGaussian.Order2Points3);
            foreach (IXFiniteElement element in model.Elements)
            {
                ((MockElement)element).IntegrationBulk = integrationBulk;
            }
            var integrationPlotter = new IntegrationPlotter2D(model);
            integrationPlotter.PlotVolumeIntegrationPoints(pathIntegrationBulk);
        }

        private static Dictionary<IXFiniteElement, List<LsmElementIntersection2D>> CalcIntersections(
            XModel model, List<SimpleLsm2D> curves)
        {
            var intersections = new Dictionary<IXFiniteElement, List<LsmElementIntersection2D>>();
            foreach (IXFiniteElement element in model.Elements)
            {
                var elementIntersections = new List<LsmElementIntersection2D>();
                foreach (IImplicitCurve2D curve in curves)
                {
                    IElementCurveIntersection2D intersection = curve.Intersect(element);
                    if (intersection.RelativePosition != RelativePositionCurveElement.Disjoint)
                    {
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
                List<LsmElementIntersection2D> elementIntersections = intersections[element];
                ElementSubtriangle2D[] subtriangles = triangulator.FindConformingMesh(element, elementIntersections, tolerance);
                conformingMesh[element] = subtriangles;
                element.ConformingSubtriangles2D = subtriangles;
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

            // Elements
            int numGaussPointsInterface = 2;
            for (int e = 0; e < cells.Count; ++e)
            {
                var element = new MockElement(e, CellType.Quad4, cells[e].Vertices);
                model.Elements.Add(element);
                model.Subdomains[subdomainID].Elements.Add(element);
            }

            model.ConnectDataStructures();
            return model;
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

        internal static void PlotConformingMesh(XModel model, Dictionary<IXFiniteElement, ElementSubtriangle2D[]> triangulation)
        {
            var conformingMesh = new ConformingOutputMesh2D(model.Nodes, model.Elements, triangulation);
            using (var writer = new VtkFileWriter(pathConformingMesh))
            {
                writer.WriteMesh(conformingMesh);
            }   
        }
    }
}
