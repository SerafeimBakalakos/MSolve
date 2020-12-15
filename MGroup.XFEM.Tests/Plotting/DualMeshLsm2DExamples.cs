using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using ISAAR.MSolve.Discretization.Mesh;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Enrichment;
using MGroup.XFEM.Enrichment.SingularityResolution;
using MGroup.XFEM.Entities;
using MGroup.XFEM.Geometry;
using MGroup.XFEM.Geometry.ConformingMesh;
using MGroup.XFEM.Geometry.LSM;
using MGroup.XFEM.Geometry.Mesh;
using MGroup.XFEM.Geometry.Primitives;
using MGroup.XFEM.Integration;
using MGroup.XFEM.Integration.Quadratures;
using MGroup.XFEM.Plotting;
using MGroup.XFEM.Plotting.Mesh;
using MGroup.XFEM.Plotting.Writers;

namespace MGroup.XFEM.Tests.Plotting
{
    public static class DualMeshLsm2DExamples
    {
        private const string outputDirectory = @"C:\Users\Serafeim\Desktop\HEAT\DualMesh\lsm2d_circles";

        private static readonly double[] minCoords = { -1.0, -1.0 };
        private static readonly double[] maxCoords = { +1.0, +1.0 };
        private static readonly int[] numElementsFem = { 4, 4 };
        private static readonly int[] numElementsLsm = { 20, 20 };
        private static readonly Circle2D initialCurve = new Circle2D(0.0, 0.0, 0.49);
        private const int boundaryIntegrationOrder = 2;


        public static void PlotIndividualMeshesLevelSets()
        {
            // Coarse mesh
            var coarseMesh = new UniformMesh2D(minCoords, maxCoords, numElementsFem);
            XModel coarseModel = CreateModel(coarseMesh);
            var coarseOutputMesh = new ContinuousOutputMesh(coarseModel.Nodes, coarseModel.Elements);
            var coarseLsm = new SimpleLsm2D(0, coarseModel, initialCurve);
            var coarseLsmField = new LevelSetField(coarseModel, coarseLsm, coarseOutputMesh);
            using (var writer = new VtkFileWriter(outputDirectory + "\\coarseLevelSets.vtk"))
            {
                writer.WriteMesh(coarseOutputMesh);
                writer.WriteScalarField("level_set", coarseLsmField.Mesh, coarseLsmField.CalcValuesAtVertices());
            }

            using (var writer = new VtkPointWriter(outputDirectory + "\\nodalCoarseLevelSets.vtk"))
            {
                var nodalLevelSets = new Dictionary<double[], double>();
                foreach(XNode node in coarseModel.Nodes)
                {
                    nodalLevelSets[node.Coordinates] = coarseLsm.SignedDistanceOf(node);
                }
                writer.WriteScalarField("level_set", nodalLevelSets);
            }

            // Fine mesh
            var fineMesh = new UniformMesh2D(minCoords, maxCoords, numElementsLsm);
            XModel fineModel = CreateModel(fineMesh);
            var fineOutputMesh = new ContinuousOutputMesh(fineModel.Nodes, fineModel.Elements);
            var fineLsm = new SimpleLsm2D(0, fineModel, initialCurve);
            var fineLsmField = new LevelSetField(fineModel, fineLsm, fineOutputMesh);
            using (var writer = new VtkFileWriter(outputDirectory + "\\fineLevelSets.vtk"))
            {
                writer.WriteMesh(fineOutputMesh);
                writer.WriteScalarField("level_set", fineLsmField.Mesh, fineLsmField.CalcValuesAtVertices());
            }
            using (var writer = new VtkPointWriter(outputDirectory + "\\nodalFineLevelSets.vtk"))
            {
                var nodalLevelSets = new Dictionary<double[], double>();
                foreach (XNode node in fineModel.Nodes)
                {
                    nodalLevelSets[node.Coordinates] = fineLsm.SignedDistanceOf(node);
                }
                writer.WriteScalarField("level_set", nodalLevelSets);
            }
        }

        public static void PlotCircleLevelSets()
        {
            var mesh = new DualMesh2D(minCoords, maxCoords, numElementsFem, numElementsLsm);
            XModel coarseModel = CreateModel(mesh.FemMesh);
            var dualMeshLsm = new DualMeshLsm2D(0, mesh, initialCurve);

            int numPointsPerElemPerAxis = 10;
            var allPoints = new Dictionary<double[], double>();
            foreach (IXFiniteElement element in coarseModel.Elements)
            {
                List<double[]> pointsNaturalCoarse = GeneratePointsPerElement(numPointsPerElemPerAxis);
                for (int p = 0; p < pointsNaturalCoarse.Count; ++p)
                {
                    var point = new XPoint();
                    point.Element = element;
                    point.Coordinates[CoordinateSystem.ElementNatural] = pointsNaturalCoarse[p];
                    //point.ShapeFunctions = element.Interpolation.EvaluateFunctionsAt(pointsNaturalFem[p]);
                    double[] cartesianCoords = 
                        element.Interpolation.TransformNaturalToCartesian(element.Nodes, pointsNaturalCoarse[p]);
                    #region debug
                    //double tol = 1E-4;
                    //double targetX = -0.05;
                    //double targetY = -0.05;
                    //double x = cartesianCoords[0];
                    //double y = cartesianCoords[1];
                    //if ((Math.Abs(x - targetX) < tol) && (Math.Abs(y - targetY) < tol))
                    //{
                    //    Console.WriteLine();
                    //}
                    #endregion
                    allPoints[cartesianCoords] = dualMeshLsm.SignedDistanceOf(point);
                    //allPoints[cartesianCoords] = initialCurve.SignedDistanceOf(cartesianCoords);
                }
            }
            
            using (var writer = new VtkPointWriter(outputDirectory + "\\point_level_sets.vtk"))
            {
                writer.WriteScalarField("level_set", allPoints);

            }
        }

        public static void PlotElementCurveIntersections()
        {
            var mesh = new DualMesh2D(minCoords, maxCoords, numElementsFem, numElementsLsm);
            XModel model = CreateModel(mesh.FemMesh);

            var dualMeshLsm = new DualMeshLsm2D(0, mesh, initialCurve);
            var lsmCurves = new IImplicitGeometry[] { dualMeshLsm };

            // Plot intersections between level set curves and elements
            Dictionary<IXFiniteElement, List<IElementGeometryIntersection>> elementIntersections
                = Utilities.Plotting.CalcIntersections(model, lsmCurves);
            var allIntersections = new List<IElementGeometryIntersection>();
            foreach (var intersections in elementIntersections.Values) allIntersections.AddRange(intersections);
            var intersectionPlotter = new LsmElementIntersectionsPlotter();
            intersectionPlotter.PlotIntersections(outputDirectory + "\\intersections.vtk", allIntersections);

            // Plot conforming mesh
            Dictionary<IXFiniteElement, IElementSubcell[]> triangulation =
                Utilities.Plotting.CreateConformingMesh(2, elementIntersections);
            var conformingMesh = new ConformingOutputMesh(model.Nodes, model.Elements, triangulation);
            using (var writer = new VtkFileWriter(outputDirectory + "\\conforming_mesh.vtk"))
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
            var integrationPlotter = new IntegrationPlotter(model);
            integrationPlotter.PlotBulkIntegrationPoints(outputDirectory + "\\gauss_points_bulk.vtk");

            // Plot boundary integration points
            integrationPlotter.PlotBoundaryIntegrationPoints(outputDirectory + "\\gauss_points_boundary.vtk", boundaryIntegrationOrder);

            // Create phases
            var geometricModel = new GeometricModel(2, model);
            geometricModel.Phases.Add(new DefaultPhase(0));
            geometricModel.Phases.Add(new LsmPhase(1, geometricModel, -1));
            var boundary = new PhaseBoundary(dualMeshLsm, geometricModel.Phases[1], geometricModel.Phases[0]);
            geometricModel.Phases[0].ExternalBoundaries.Add(boundary);
            geometricModel.Phases[0].Neighbors.Add(geometricModel.Phases[1]);
            geometricModel.Phases[1].ExternalBoundaries.Add(boundary);
            geometricModel.Phases[1].Neighbors.Add(geometricModel.Phases[0]);

            // Phases-mesh interaction
            geometricModel.InteractWithNodes();
            geometricModel.InteractWithElements();
            geometricModel.FindConformingMesh();

            // Plot phases
            var phasePlotter = new PhasePlotter(model, geometricModel, 0);
            phasePlotter.PlotNodes(outputDirectory + "\\phases_of_nodes.vtk");
            phasePlotter.PlotElements(outputDirectory + "\\phases_of_elements.vtk", conformingMesh);

            // Enrichment
            var nodeEnricher = new NodeEnricherMultiphase(geometricModel, new NullSingularityResolver());
            nodeEnricher.ApplyEnrichments();
            model.UpdateDofs();
            model.UpdateMaterials();

            double elementSize = (maxCoords[0] - minCoords[0]) / numElementsFem[0];
            var enrichmentPlotter = new EnrichmentPlotter(model, elementSize, false);
            enrichmentPlotter.PlotStepEnrichedNodes(outputDirectory + "\\enriched_nodes_step.vtk");
        }

        private static XModel CreateModel(IStructuredMesh mesh)
        {
            var model = new XModel();
            for (int n = 0; n < mesh.NumNodesTotal; ++n)
            {
                model.Nodes.Add(new XNode(n, mesh.GetNodeCoordinates(mesh.GetNodeIdx(n))));
            }

            for (int e = 0; e < mesh.NumElementsTotal; ++e)
            {
                var nodes = new List<XNode>();
                int[] connectivity = mesh.GetElementConnectivity(mesh.GetElementIdx(e));
                foreach (int n in connectivity)
                {
                    nodes.Add(model.Nodes[n]);
                }
                model.Elements.Add(new MockElement(e, CellType.Quad4, nodes));
            }

            return model;
        }

        private static List<double[]> GeneratePointsPerElement(int numPointsPerAxis, double tolerance = 0.0)
        {
            var points = new List<double[]>();
            double minCoord = -1 + tolerance;
            double maxCoord = 1 - tolerance;
            double space = (maxCoord - minCoord) / numPointsPerAxis;
            for (int i = 0; i < numPointsPerAxis; ++i)
            {
                double xi = minCoord + 0.5 * space + i * space;
                for (int j = 0; j < numPointsPerAxis; ++j)
                {
                    double eta = minCoord + 0.5 * space + j * space;
                    points.Add(new double[] { xi, eta });
                }
            }
            return points;
        }
    }
}
