using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Enrichment.Enrichers;
using MGroup.XFEM.Enrichment.SingularityResolution;
using MGroup.XFEM.Entities;
using MGroup.XFEM.Geometry.LSM;
using MGroup.XFEM.Geometry.Primitives;
using MGroup.XFEM.Materials;
using MGroup.XFEM.Phases;
using MGroup.XFEM.Output;
using MGroup.XFEM.Output.Writers;
using MGroup.XFEM.Tests.Utilities;
using Xunit;
using MGroup.XFEM.Geometry.Mesh;
using MGroup.XFEM.Integration;
using MGroup.XFEM.Integration.Quadratures;
using MGroup.XFEM.Cracks;
using MGroup.XFEM.Cracks.Geometry;
using MGroup.XFEM.Output.Mesh;
using MGroup.XFEM.Output.Vtk;
using MGroup.XFEM.Output.EnrichmentObservers;
using MGroup.XFEM.Enrichment;

namespace MGroup.XFEM.Tests.Fracture.ThreeDimensional
{
    public static class StaticCircleCrack3DTests
    {
        private static readonly string outputDirectory = Path.Combine(
            Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName, "Resources", "circle_crack_3D_temp");
        private static readonly string expectedDirectory = Path.Combine(
            Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName, "Resources", "circle_crack_3D");

        private static readonly double[] minCoords = { -1.0, -1.0, -1.0 };
        private static readonly double[] maxCoords = { +1.0, +1.0, +1.0 };
        private static readonly int[] numElements = { 21, 21, 21 }; // Better to use odd numbers to avoid conforming cases
        private const int bulkIntegrationOrder = 2, boundaryIntegrationOrder = 2;

        private const double circleRadius = 0.3;

        private const double E = 2E6, v = 0.3;
        private const double jIntegralRadiusRatio = 2.0;
        private const double heavisideTol = 1E-4;
        private const double tipEnrichmentArea = 0.0;

        private const int subdomainID = 0;

        [Fact]
        public static void TestModel()
        {
            try
            {
                if (!Directory.Exists(outputDirectory))
                {
                    Directory.CreateDirectory(outputDirectory);
                }

                // Create model and LSM
                XModel<IXCrackElement> model = CreateModel();
                model.FindConformingSubcells = true;

                var crack = (LsmCrack3D)model.GeometryModel.GetDiscontinuity(0);

                // Plot the FE mesh
                var outputMesh = new ContinuousOutputMesh(model.XNodes, model.Elements);
                string meshPath = outputDirectory + "\\mesh.vtk";
                using (var writer = new VtkFileWriter(meshPath))
                {
                    writer.WriteMesh(outputMesh);
                }

                // Set up observers. //TODO: Each observer should define and link all its necessary observers in an optional constructor.
                //crack.Observers.Add(new CrackPathPlotter(crack, outputDirectory));
                crack.Observers.Add(new CrackLevelSetPlotter(crack, outputMesh, outputDirectory));
                crack.Observers.Add(new CrackInteractingElementsPlotter(crack, outputDirectory));

                //var newTipNodes = new NewCrackTipNodesObserver(crack);
                //model.RegisterEnrichmentObserver(newTipNodes);
                //var allBodyNodes = new CrackBodyNodesObserver(crack);
                //model.RegisterEnrichmentObserver(allBodyNodes);
                //var rejectedBodyNodes = new RejectedCrackBodyNodesObserver(crack, newTipNodes, allBodyNodes);
                //model.RegisterEnrichmentObserver(rejectedBodyNodes);

                //var enrichmentPlotter = new CrackEnrichmentPlotter(crack, outputDir, newTipNodes, previousTipNodes, allBodyNodes,
                //    newBodyNodes, rejectedBodyNodes, nearModifiedNodes);
                //model.RegisterEnrichmentObserver(enrichmentPlotter);


                //// Plot element - phase boundaries interactions
                model.ModelObservers.Add(new LsmElementIntersectionsPlotter(outputDirectory, model));

                // Plot element subcells
                model.ModelObservers.Add(new ConformingMeshPlotter(outputDirectory, model, false));
                model.ModelObservers.Add(new ConformingMeshPlotter(outputDirectory, model, true));

                //// Plot bulk and boundary integration points of each element
                //model.ModelObservers.Add(new IntegrationPointsPlotter(outputDirectory, model));

                // Propagate the crack. During this the observers will plot the data they pull from model. 
                model.Initialize();

                //// Compare output
                //var computedFiles = new List<string>();
                //computedFiles.Add(Path.Combine(outputDirectory, "level_set0_t0.vtk"));
                //computedFiles.Add(Path.Combine(outputDirectory, "intersections_t0.vtk"));
                //computedFiles.Add(Path.Combine(outputDirectory, "conforming_mesh_t0.vtk"));
                //computedFiles.Add(Path.Combine(outputDirectory, "gauss_points_bulk_t0.vtk"));
                //computedFiles.Add(Path.Combine(outputDirectory, "gauss_points_boundary_t0.vtk"));
                //computedFiles.Add(Path.Combine(outputDirectory, "enriched_nodes_heaviside_t0.vtk"));
                //computedFiles.Add(Path.Combine(outputDirectory, "enriched_nodes_tip_t0.vtk"));

                //var expectedFiles = new List<string>();
                //expectedFiles.Add(Path.Combine(expectedDirectory, "level_set0_t0.vtk"));
                //expectedFiles.Add(Path.Combine(expectedDirectory, "intersections_t0.vtk"));
                //expectedFiles.Add(Path.Combine(expectedDirectory, "conforming_mesh_t0.vtk"));
                //expectedFiles.Add(Path.Combine(expectedDirectory, "gauss_points_bulk_t0.vtk"));
                //expectedFiles.Add(Path.Combine(expectedDirectory, "gauss_points_boundary_t0.vtk"));
                //expectedFiles.Add(Path.Combine(expectedDirectory, "enriched_nodes_heaviside_t0.vtk"));
                //expectedFiles.Add(Path.Combine(expectedDirectory, "enriched_nodes_tipe_t0.vtk"));

                //double tolerance = 1E-6;
                //for (int i = 0; i < expectedFiles.Count; ++i)
                //{
                //    Assert.True(IOUtilities.AreDoubleValueFilesEquivalent(expectedFiles[i], computedFiles[i], tolerance));
                //}
                throw new Exception();
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

        private static XModel<IXCrackElement> CreateModel()
        {
            var model = new XModel<IXCrackElement>(3);
            model.Subdomains[subdomainID] = new XSubdomain(subdomainID);
            model.FindConformingSubcells = true;

            // Materials, integration
            var material = new HomogeneousFractureMaterialField3D(E, v);
            var enrichedIntegration = new IntegrationWithConformingSubtetrahedra3D(TetrahedronQuadrature.Order2Points4);
            var bulkIntegration = new CrackElementIntegrationStrategy(
                enrichedIntegration, enrichedIntegration, enrichedIntegration);
            var factory = new XCrackElementFactory3D(material, bulkIntegration);

            // Mesh
            var mesh = new UniformMesh3D(minCoords, maxCoords, numElements);
            Utilities.Models.AddNodesElements(model, mesh, factory);
            //ApplyBoundaryConditions(model);

            // Crack, enrichments
            model.GeometryModel = CreateCrack(model);

            return model;
        }

        private static CrackGeometryModel CreateCrack(XModel<IXCrackElement> model)
        {
            var geometryModel = new CrackGeometryModel(model);
            geometryModel.Enricher = new NodeEnricherIndependentCracks(
                geometryModel, new RelativeAreaSingularityResolver(heavisideTol), tipEnrichmentArea);
            geometryModel.Enricher = new NullEnricher();

            //var initialGeom = new Circle3D(circleRadius, 0, 0, 0);
            OpenLsm3D lsmGeometry = InitializeLevelSet(model);

            var jIntegrationRule = 
                new IntegrationWithNonconformingSubhexahedra3D(8, GaussLegendre3D.GetQuadratureWithOrder(4, 4, 4));
            IPropagator propagator = null;
            var crack = new LsmCrack3D(0, lsmGeometry, model, propagator);
            geometryModel.Cracks[crack.ID] = crack;

            return geometryModel;
        }

        private static OpenLsm3D InitializeLevelSet(XModel<IXCrackElement> model)
        {
            var circle = new Circle2D(new double[] { 0, 0 }, circleRadius);
            var lsm = new OpenLsm3D(0);
            foreach (XNode node in model.XNodes)
            {
                lsm.LevelSetsBody[node.ID] = node.Coordinates[2];
                lsm.LevelSetsTip[node.ID] = circle.SignedDistanceOf(new double[] { node.Coordinates[0], node.Coordinates[1] });
            }
            return lsm;
        }

        private class NullEnricher : INodeEnricher
        {
            public void ApplyEnrichments()
            {
            }

            public IEnumerable<EnrichmentItem> DefineEnrichments()
            {
                return new EnrichmentItem[0];
            }
        }
    }
}
