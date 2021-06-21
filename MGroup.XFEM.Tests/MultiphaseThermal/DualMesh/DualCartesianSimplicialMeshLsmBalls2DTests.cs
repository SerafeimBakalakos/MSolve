﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using ISAAR.MSolve.Discretization.Mesh;
using MGroup.Geometry.Mesh;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Enrichment.Enrichers;
using MGroup.XFEM.Enrichment.SingularityResolution;
using MGroup.XFEM.Entities;
using MGroup.XFEM.Geometry.LSM;
using MGroup.XFEM.Geometry.LSM.DualMesh;
using MGroup.XFEM.Geometry.Mesh;
using MGroup.XFEM.Geometry.Primitives;
using MGroup.XFEM.Integration;
using MGroup.XFEM.Integration.Quadratures;
using MGroup.XFEM.Materials;
using MGroup.XFEM.Output;
using MGroup.XFEM.Output.Fields;
using MGroup.XFEM.Output.Mesh;
using MGroup.XFEM.Output.Vtk;
using MGroup.XFEM.Output.Writers;
using MGroup.XFEM.Phases;
using MGroup.XFEM.Tests.Utilities;
using Xunit;

//TODO: Add tests for nodal level values. These should be hardcoded and work even if a LSM implementation only stored a subset of nodes.
namespace MGroup.XFEM.Tests.MultiphaseThermal.DualMesh
{
    public static class DualCartesianSimplicialMeshLsmBalls2DTests
    {
        private static readonly string outputDirectory = Path.Combine(
            Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName, 
            "Resources", "dual_cartesian_simplicial_mesh_lsm_balls_2D_temp");
        private static readonly string expectedDirectory = Path.Combine(
            Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName, 
            "Resources", "dual_cartesian_simplicial_mesh_lsm_balls_2D");

        private static readonly int dim = 2;
        private static readonly double[] minCoords = { -1.0, -1.0 };
        private static readonly double[] maxCoords = { +1.0, +1.0 };
        private static readonly int[] numElementsCoarse = { 4, 4 };
        private static readonly int[] numNodesCoarse = { 5, 5 };
        private static readonly int[] numNodesFine = { 21, 21 };
        private static readonly Circle2D initialCurve = new Circle2D(0.0, 0.0, 0.50);
        private const int defaultPhaseID = 0;
        private const int bulkIntegrationOrder = 2, boundaryIntegrationOrder = 2;

        [Fact]
        public static void TestIndividualMeshesLevelSets()
        {
            try
            {
                if (!Directory.Exists(outputDirectory))
                {
                    Directory.CreateDirectory(outputDirectory);
                }

                // Coarse mesh
                var coarseMesh = new UniformCartesianMesh2D.Builder(minCoords, maxCoords, numElementsCoarse).BuildMesh();
                XModel<IXMultiphaseElement> coarseModel = CreateModel(coarseMesh);
                var coarseOutputMesh = new ContinuousOutputMesh(coarseModel.XNodes, coarseModel.Elements);
                var coarseLsm = new SimpleLsm2D(0, coarseModel.XNodes, initialCurve);
                var coarseLsmField = new LevelSetField(coarseModel, coarseLsm, coarseOutputMesh);
                using (var writer = new VtkFileWriter(Path.Combine(outputDirectory, "coarseLevelSets.vtk")))
                {
                    writer.WriteMesh(coarseOutputMesh);
                    writer.WriteScalarField("level_set", coarseLsmField.Mesh, coarseLsmField.CalcValuesAtVertices());
                }

                using (var writer = new VtkPointWriter(Path.Combine(outputDirectory, "nodalCoarseLevelSets.vtk")))
                {
                    var nodalLevelSets = new Dictionary<double[], double>();
                    foreach (XNode node in coarseModel.XNodes)
                    {
                        nodalLevelSets[node.Coordinates] = coarseLsm.SignedDistanceOf(node);
                    }
                    writer.WriteScalarField("level_set", nodalLevelSets);
                }

                // Fine mesh
                var fineMesh = new UniformSimplicialMesh2D.Builder(minCoords, maxCoords, numNodesFine).BuildMesh();
                XModel<IXMultiphaseElement> fineModel = CreateModel(fineMesh);
                var fineOutputMesh = new ContinuousOutputMesh(fineModel.XNodes, fineModel.Elements);
                var fineLsm = new SimpleLsm2D(0, fineModel.XNodes, initialCurve);
                var fineLsmField = new LevelSetField(fineModel, fineLsm, fineOutputMesh);
                using (var writer = new VtkFileWriter(Path.Combine(outputDirectory, "fineLevelSets.vtk")))
                {
                    writer.WriteMesh(fineOutputMesh);
                    writer.WriteScalarField("level_set", fineLsmField.Mesh, fineLsmField.CalcValuesAtVertices());
                }
                using (var writer = new VtkPointWriter(Path.Combine(outputDirectory, "nodalFineLevelSets.vtk")))
                {
                    var nodalLevelSets = new Dictionary<double[], double>();
                    foreach (XNode node in fineModel.XNodes)
                    {
                        nodalLevelSets[node.Coordinates] = fineLsm.SignedDistanceOf(node);
                    }
                    writer.WriteScalarField("level_set", nodalLevelSets);
                }

                // Compare output
                var computedFiles = new List<string>();
                computedFiles.Add(Path.Combine(outputDirectory, "coarseLevelSets.vtk"));
                computedFiles.Add(Path.Combine(outputDirectory, "nodalCoarseLevelSets.vtk"));
                computedFiles.Add(Path.Combine(outputDirectory, "fineLevelSets.vtk"));
                computedFiles.Add(Path.Combine(outputDirectory, "nodalFineLevelSets.vtk"));

                var expectedFiles = new List<string>();
                expectedFiles.Add(Path.Combine(expectedDirectory, "coarseLevelSets.vtk"));
                expectedFiles.Add(Path.Combine(expectedDirectory, "nodalCoarseLevelSets.vtk"));
                expectedFiles.Add(Path.Combine(expectedDirectory, "fineLevelSets.vtk"));
                expectedFiles.Add(Path.Combine(expectedDirectory, "nodalFineLevelSets.vtk"));

                double tolerance = 1E-6;
                for (int i = 0; i < expectedFiles.Count; ++i)
                {
                    Assert.True(IOUtilities.AreDoubleValueFilesEquivalent(expectedFiles[i], computedFiles[i], tolerance));
                }
            }
            finally
            {
                if (Directory.Exists(outputDirectory))
                {
                    DirectoryInfo di = new DirectoryInfo(outputDirectory);
                    di.Delete(true);//true means delete subdirectories and files
                }
            }
        }

        [Theory]
        [InlineData(DualMeshLsmChoice.Global)]
        [InlineData(DualMeshLsmChoice.Fixed)]
        //[InlineData(DualMeshLsmChoice.Local)]
        public static void TestLevelSetsAtRandomPoints(DualMeshLsmChoice lsmChoice)
        {
            try
            {
                if (!Directory.Exists(outputDirectory))
                {
                    Directory.CreateDirectory(outputDirectory);
                }

                var mesh = new DualCartesianSimplicialMesh2D.Builder(minCoords, maxCoords, numNodesCoarse, numNodesFine)
                    .BuildMesh();
                XModel<IXMultiphaseElement> coarseModel = CreateModel(mesh.CoarseMesh);
                var dualMeshLsm = new DualMeshLsm(0, initialCurve, mesh, lsmChoice.Create(dim));

                int numPointsPerElemPerAxis = 15;
                var allPoints = new Dictionary<double[], double>();
                foreach (IXFiniteElement element in coarseModel.Elements)
                {
                    List<double[]> pointsNaturalCoarse = GeneratePointsPerElement(numPointsPerElemPerAxis);
                    for (int p = 0; p < pointsNaturalCoarse.Count; ++p)
                    {
                        var point = new XPoint(dim);
                        point.Element = element;
                        point.Coordinates[CoordinateSystem.ElementNatural] = pointsNaturalCoarse[p];
                        double[] cartesianCoords =
                            element.Interpolation.TransformNaturalToCartesian(element.Nodes, pointsNaturalCoarse[p]);
                        allPoints[cartesianCoords] = dualMeshLsm.SignedDistanceOf(point);
                    }
                }

                // Plot the level sets at these points
                string pathComputed = Path.Combine(outputDirectory, "random_point_level_sets.vtk");
                using (var writer = new VtkPointWriter(pathComputed))
                {
                    writer.WriteScalarField("level_set", allPoints);
                }

                // Compare output
                var computedFiles = new List<string>();
                computedFiles.Add(pathComputed);

                var expectedFiles = new List<string>();
                expectedFiles.Add(Path.Combine(expectedDirectory, "random_point_level_sets.vtk"));

                double tolerance = 1E-6;
                for (int i = 0; i < expectedFiles.Count; ++i)
                {
                    Assert.True(IOUtilities.AreDoubleValueFilesEquivalent(expectedFiles[i], computedFiles[i], tolerance));
                }
            }
            finally
            {
                if (Directory.Exists(outputDirectory))
                {
                    DirectoryInfo di = new DirectoryInfo(outputDirectory);
                    di.Delete(true);//true means delete subdirectories and files
                }
            }
        }

        [Theory]
        [InlineData(DualMeshLsmChoice.Global)]
        [InlineData(DualMeshLsmChoice.Fixed)]
        [InlineData(DualMeshLsmChoice.Local)]
        public static void TestModel(DualMeshLsmChoice lsmChoice)
        {
            try
            {
                if (!Directory.Exists(outputDirectory))
                {
                    Directory.CreateDirectory(outputDirectory);
                }

                // Create model and LSM
                var mesh = new DualCartesianSimplicialMesh2D.Builder(minCoords, maxCoords, numNodesCoarse, numNodesFine).BuildMesh();
                XModel<IXMultiphaseElement> model = CreateModel(mesh.CoarseMesh);
                model.FindConformingSubcells = true;
                PhaseGeometryModel geometryModel = CreatePhases(lsmChoice, model, mesh);

                // Plot phases of nodes
                geometryModel.InteractionObservers.Add(new NodalPhasesPlotter(outputDirectory, model));

                // Plot element - phase boundaries interactions
                geometryModel.InteractionObservers.Add(new LsmElementIntersectionsPlotter(outputDirectory, model));

                // Plot element subcells
                model.ModelObservers.Add(new ConformingMeshPlotter(outputDirectory, model));

                // Plot phases of each element subcell
                model.ModelObservers.Add(new ElementPhasePlotter(outputDirectory, model, geometryModel, defaultPhaseID));

                // Write the size of each phase
                model.ModelObservers.Add(new PhasesSizeWriter(outputDirectory, model, geometryModel));

                // Plot bulk and boundary integration points of each element
                bool plotNormals = false;
                model.ModelObservers.Add(new IntegrationPointsPlotter(outputDirectory, model, plotNormals));

                // Plot enrichments
                double elementSize = (maxCoords[0] - minCoords[0]) / numElementsCoarse[0];
                model.RegisterEnrichmentObserver(new PhaseEnrichmentPlotter(outputDirectory, model, elementSize, 2));

                // Initialize model state so that everything described above can be tracked
                model.Initialize();

                // Compare output
                var computedFiles = new List<string>();
                computedFiles.Add(Path.Combine(outputDirectory, "nodal_phases_t0.vtk"));
                computedFiles.Add(Path.Combine(outputDirectory, "intersections_t0.vtk"));
                computedFiles.Add(Path.Combine(outputDirectory, "conforming_mesh_t0.vtk"));
                computedFiles.Add(Path.Combine(outputDirectory, "element_phases_t0.vtk"));
                computedFiles.Add(Path.Combine(outputDirectory, "phase_sizes_t0.txt"));
                computedFiles.Add(Path.Combine(outputDirectory, "gauss_points_bulk_t0.vtk"));
                computedFiles.Add(Path.Combine(outputDirectory, "gauss_points_boundary_t0.vtk"));
                computedFiles.Add(Path.Combine(outputDirectory, "enriched_nodes_heaviside_t0.vtk"));

                var expectedFiles = new List<string>();
                expectedFiles.Add(Path.Combine(expectedDirectory, "nodal_phases_t0.vtk"));
                expectedFiles.Add(Path.Combine(expectedDirectory, "intersections_t0.vtk"));
                expectedFiles.Add(Path.Combine(expectedDirectory, "conforming_mesh_t0.vtk"));
                expectedFiles.Add(Path.Combine(expectedDirectory, "element_phases_t0.vtk"));
                expectedFiles.Add(Path.Combine(expectedDirectory, "phase_sizes_t0.txt"));
                expectedFiles.Add(Path.Combine(expectedDirectory, "gauss_points_bulk_t0.vtk"));
                expectedFiles.Add(Path.Combine(expectedDirectory, "gauss_points_boundary_t0.vtk"));
                expectedFiles.Add(Path.Combine(expectedDirectory, "enriched_nodes_heaviside_t0.vtk"));

                double tolerance = 1E-6;
                for (int i = 0; i < expectedFiles.Count; ++i)
                {
                    Assert.True(IOUtilities.AreDoubleValueFilesEquivalent(expectedFiles[i], computedFiles[i], tolerance));
                }
            }
            finally
            {
                if (Directory.Exists(outputDirectory))
                {
                    DirectoryInfo di = new DirectoryInfo(outputDirectory);
                    di.Delete(true);//true means delete subdirectories and files
                }
            }
        }

        private static XModel<IXMultiphaseElement> CreateModel(IStructuredMesh mesh)
        {
            var model = new XModel<IXMultiphaseElement>(dim);
            model.Subdomains[0] = new XSubdomain(0);
            for (int n = 0; n < mesh.NumNodesTotal; ++n)
            {
                model.XNodes.Add(new XNode(n, mesh.GetNodeCoordinates(mesh.GetNodeIdx(n))));
            }

            var matrixMaterial = new ThermalMaterial(1, 1);
            var inclusionMaterial = new ThermalMaterial(1, 1);
            var materialField = new MatrixInclusionsThermalMaterialField(matrixMaterial, inclusionMaterial,
                1, 1, defaultPhaseID);

            var stdQuadrature = GaussLegendre2D.GetQuadratureWithOrder(bulkIntegrationOrder, bulkIntegrationOrder);
            var subcellQuadrature = TriangleQuadratureSymmetricGaussian.Order2Points3;
            var integrationBulk = new IntegrationWithConformingSubtriangles2D(subcellQuadrature);

            var elemFactory = new XThermalElement2DFactory(materialField, 1, integrationBulk, boundaryIntegrationOrder, true);
            for (int e = 0; e < mesh.NumElementsTotal; ++e)
            {
                var nodes = new List<XNode>();
                int[] connectivity = mesh.GetElementConnectivity(e);
                foreach (int n in connectivity)
                {
                    nodes.Add(model.XNodes[n]);
                }
                XThermalElement2D element = elemFactory.CreateElement(e, mesh.CellType, nodes);
                model.Elements.Add(element);
                model.Subdomains[0].Elements.Add(element);
            }

            return model;
        }

        private static PhaseGeometryModel CreatePhases(DualMeshLsmChoice lsmChoice,
            XModel<IXMultiphaseElement> model, IDualMesh mesh)
        {
            var geometricModel = new PhaseGeometryModel(model);
            model.GeometryModel = geometricModel;
            geometricModel.Enricher = NodeEnricherMultiphaseNoJunctions.CreateThermalStep(geometricModel);
            
            var defaultPhase = new DefaultPhase();
            geometricModel.Phases[defaultPhase.ID] = defaultPhase;
            var phase = new LsmPhase(1, geometricModel, -1);
            geometricModel.Phases[phase.ID] = phase;

            var dualMeshLsm = new DualMeshLsm(0, initialCurve, mesh, lsmChoice.Create(dim));
            var boundary = new ClosedPhaseBoundary(phase.ID, dualMeshLsm, defaultPhase, phase);
            defaultPhase.ExternalBoundaries.Add(boundary);
            defaultPhase.Neighbors.Add(phase);
            phase.ExternalBoundaries.Add(boundary);
            phase.Neighbors.Add(defaultPhase);
            geometricModel.PhaseBoundaries[boundary.ID] = boundary;

            return geometricModel;
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
