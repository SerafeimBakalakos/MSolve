﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ISAAR.MSolve.Discretization;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Materials;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Enrichment.Enrichers;
using MGroup.XFEM.Enrichment.SingularityResolution;
using MGroup.XFEM.Entities;
using MGroup.XFEM.Geometry.LSM;
using MGroup.XFEM.Geometry.Primitives;
using MGroup.XFEM.Materials;
using MGroup.XFEM.Output;
using MGroup.XFEM.Output.Writers;
using MGroup.XFEM.Phases;
using MGroup.XFEM.Tests.Utilities;
using Xunit;

namespace MGroup.XFEM.Tests.MultiphaseStructural
{
    public static class CohesiveInclusions2DTests
    {
        private static readonly string outputDirectory = Path.Combine(
            Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName, "Resources", "cohesive_inclusions_2D_temp");
        private static readonly string expectedDirectory = Path.Combine(
            Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName, "Resources", "cohesive_inclusions_2D");

        private static readonly double[] minCoords = { -10.0, -10.0 };
        private static readonly double[] maxCoords = { +10.0, +10.0 };
        private const double thickness = 1.0;
        private static readonly int[] numElements = { 36, 36 };
        private const int bulkIntegrationOrder = 2, boundaryIntegrationOrder = 2;

        private const double matrixE = 2E6, inclusionE = 2E8, v = 0.3;
        //private const double cohesivenessNormal = 0, cohesivenessTangent = cohesivenessNormal;
        private const double cohesivenessNormal = 1E8, cohesivenessTangent = cohesivenessNormal;
        private const double loadXPerNode = 1E4;


        private static readonly int[] numBalls = { 2, 1 };
        private static readonly int numBallsTotal = 25;
        private const double ballRadius = 0.25;
        private const int defaultPhaseID = 0;


        //[Fact]
        public static void TestModel()
        {
            try
            {
                if (!Directory.Exists(outputDirectory))
                {
                    Directory.CreateDirectory(outputDirectory);
                }

                // Create model and LSM
                XModel<IXMultiphaseElement> model = CreateModel();
                model.FindConformingSubcells = true;
                PhaseGeometryModel geometryModel = CreatePhases(model);

                // Plot level sets
                geometryModel.GeometryObservers.Add(new PhaseLevelSetPlotter(outputDirectory, model, geometryModel));

                // Plot phases of nodes
                geometryModel.InteractionObservers.Add(new NodalPhasesPlotter(outputDirectory, model));

                // Plot element - phase boundaries interactions
                model.ModelObservers.Add(new LsmElementIntersectionsPlotter(outputDirectory, model));

                // Plot element subcells
                model.ModelObservers.Add(new ConformingMeshPlotter(outputDirectory, model));

                // Plot phases of each element subcell
                model.ModelObservers.Add(new ElementPhasePlotter(outputDirectory, model, geometryModel, defaultPhaseID));


                // Plot bulk and boundary integration points of each element
                model.ModelObservers.Add(new IntegrationPointsPlotter(outputDirectory, model, true));

                // Plot enrichments
                double elementSize = (maxCoords[0] - minCoords[0]) / numElements[0];
                model.RegisterEnrichmentObserver(new PhaseEnrichmentPlotter(outputDirectory, model, elementSize, 2));

                // Initialize model state so that everything described above can be tracked
                model.Initialize();

                // Compare output
                var computedFiles = new List<string>();
                computedFiles.Add(Path.Combine(outputDirectory, "level_set1_t0.vtk"));
                computedFiles.Add(Path.Combine(outputDirectory, "level_set2_t0.vtk"));
                computedFiles.Add(Path.Combine(outputDirectory, "nodal_phases_t0.vtk"));
                computedFiles.Add(Path.Combine(outputDirectory, "intersections_t0.vtk"));
                computedFiles.Add(Path.Combine(outputDirectory, "conforming_mesh_t0.vtk"));
                computedFiles.Add(Path.Combine(outputDirectory, "element_phases_t0.vtk"));
                computedFiles.Add(Path.Combine(outputDirectory, "phase_sizes_t0.txt"));
                computedFiles.Add(Path.Combine(outputDirectory, "gauss_points_bulk_t0.vtk"));
                computedFiles.Add(Path.Combine(outputDirectory, "gauss_points_boundary_t0.vtk"));
                computedFiles.Add(Path.Combine(outputDirectory, "gauss_points_boundary_normals_t0.vtk"));
                computedFiles.Add(Path.Combine(outputDirectory, "enriched_nodes_heaviside_t0.vtk"));

                var expectedFiles = new List<string>();
                expectedFiles.Add(Path.Combine(expectedDirectory, "level_set1_t0.vtk"));
                expectedFiles.Add(Path.Combine(expectedDirectory, "level_set2_t0.vtk"));
                expectedFiles.Add(Path.Combine(expectedDirectory, "nodal_phases_t0.vtk"));
                expectedFiles.Add(Path.Combine(expectedDirectory, "intersections_t0.vtk"));
                expectedFiles.Add(Path.Combine(expectedDirectory, "conforming_mesh_t0.vtk"));
                expectedFiles.Add(Path.Combine(expectedDirectory, "element_phases_t0.vtk"));
                expectedFiles.Add(Path.Combine(expectedDirectory, "phase_sizes_t0.txt"));
                expectedFiles.Add(Path.Combine(expectedDirectory, "gauss_points_bulk_t0.vtk"));
                expectedFiles.Add(Path.Combine(expectedDirectory, "gauss_points_boundary_t0.vtk"));
                expectedFiles.Add(Path.Combine(expectedDirectory, "gauss_points_boundary_normals_t0.vtk"));
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

        //[Fact]
        public static void TestSolution()
        {
            try
            {
                if (!Directory.Exists(outputDirectory))
                {
                    Directory.CreateDirectory(outputDirectory);
                }

                // Create model and LSM
                XModel<IXMultiphaseElement> model = CreateModel();
                model.FindConformingSubcells = true;
                PhaseGeometryModel geometryModel = CreatePhases(model);

                // Run analysis
                model.Initialize();
                IVectorView solution = Analysis.RunStructuralStaticAnalysis(model);

                // Plot displacements, strains, stresses
                var computedFiles = new List<string>();
                computedFiles.Add(Path.Combine(outputDirectory, "displacement_nodes_t0.vtk"));
                computedFiles.Add(Path.Combine(outputDirectory, "displacement_gauss_points_t0.vtk"));
                computedFiles.Add(Path.Combine(outputDirectory, "strains_gauss_points_t0.vtk"));
                computedFiles.Add(Path.Combine(outputDirectory, "stresses_gauss_points_t0.vtk"));
                computedFiles.Add(Path.Combine(outputDirectory, "displacement_strain_stress_field_t0.vtk"));
                Utilities.Plotting.PlotDisplacements(model, solution, computedFiles[0], computedFiles[1]);
                Utilities.Plotting.PlotStrainsStressesAtGaussPoints(model, solution, computedFiles[2], computedFiles[3]);
                Utilities.Plotting.PlotDisplacementStrainStressFields(model, solution, computedFiles[4]);

                // Compare output
                var expectedFiles = new List<string>();
                //expectedFiles.Add(Path.Combine(expectedDirectory, "temperature_nodes_t0.vtk"));
                //expectedFiles.Add(Path.Combine(expectedDirectory, "temperature_gauss_points_t0.vtk"));
                //expectedFiles.Add(Path.Combine(expectedDirectory, "temperature_field_t0.vtk"));
                //expectedFiles.Add(Path.Combine(expectedDirectory, "heat_flux_gauss_points_t0.vtk"));

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

        private static XModel<IXMultiphaseElement> CreateModel()
        {
            // Materials
            var materialMatrix = new ElasticMaterial2D(StressState2D.PlaneStress) { YoungModulus = matrixE, PoissonRatio = v };
            var materialInclusion = new ElasticMaterial2D(StressState2D.PlaneStress) { YoungModulus = inclusionE, PoissonRatio = v };
            var interfaceMaterial = new CohesiveInterfaceMaterial2D(Matrix.CreateFromArray(new double[,]
            {
                { cohesivenessNormal, 0 },
                { 0, cohesivenessTangent }
            }));
            var materialField = new MatrixInclusionsStructuralMaterialField(
                materialMatrix, materialInclusion, interfaceMaterial, 0);

            // Setup model
            XModel<IXMultiphaseElement> model = Models.CreateQuad4Model(minCoords, maxCoords, thickness, numElements,
                bulkIntegrationOrder, boundaryIntegrationOrder, materialField);
            Models.ApplyBoundaryConditionsCantileverTension(model, loadXPerNode);

            return model;
        }

        private static PhaseGeometryModel CreatePhases(XModel<IXMultiphaseElement> model)
        {
            var geometricModel = new PhaseGeometryModel(model);
            model.GeometryModel = geometricModel;
            geometricModel.Enricher = new NodeEnricherMultiphaseStructural(2, geometricModel, new NullSingularityResolver());
            Circle2D[] circles = CreateCircles();
            var defaultPhase = new DefaultPhase();
            geometricModel.Phases[defaultPhase.ID] = defaultPhase;
            for (int p = 0; p < circles.Length; ++p)
            {
                var lsm = new SimpleLsm2D(p + 1, model.XNodes, circles[p]);
                var phase = new LsmPhase(p + 1, geometricModel, -1);
                geometricModel.Phases[phase.ID] = phase;

                var boundary = new ClosedPhaseBoundary(phase.ID, lsm, defaultPhase, phase);
                defaultPhase.ExternalBoundaries.Add(boundary);
                defaultPhase.Neighbors.Add(phase);
                phase.ExternalBoundaries.Add(boundary);
                phase.Neighbors.Add(defaultPhase);
                geometricModel.PhaseBoundaries[boundary.ID] = boundary;
            }
            return geometricModel;
        }

        //private static List<SimpleLsm2D> InitializeLSM(XModel<IXMultiphaseElement> model)
        //{
        //    double xMin = minCoords[0], xMax = maxCoords[0], yMin = minCoords[1], yMax = maxCoords[1];
        //    var curves = new List<SimpleLsm2D>(numBalls[0] * numBalls[1]);
        //    double dx = (xMax - xMin) / (numBalls[0] + 1);
        //    double dy = (yMax - yMin) / (numBalls[1] + 1);
        //    int id = 1;
        //    for (int i = 0; i < numBalls[0]; ++i)
        //    {
        //        double centerX = xMin + (i + 1) * dx;
        //        for (int j = 0; j < numBalls[1]; ++j)
        //        {
        //            double centerY = yMin + (j + 1) * dy;
        //            var circle = new Circle2D(centerX, centerY, ballRadius);
        //            var lsm = new SimpleLsm2D(id++, model.XNodes, circle);
        //            curves.Add(lsm);
        //        }
        //    }

        //    return curves;
        //}

        private static Circle2D[] CreateCircles()
        {
            double radius = 1.1667;
            var circles = new Circle2D[numBallsTotal];
            if (numBallsTotal == 1)
            {
                circles[0] = new Circle2D(0, 0, radius);
            }
            else if (numBallsTotal == 4)
            {
                circles[0] = new Circle2D(3.722233, 3.722233, radius);
                circles[1] = new Circle2D(-3.722233, 3.722233, radius);
                circles[2] = new Circle2D(3.722233, -3.722233, radius);
                circles[3] = new Circle2D(-3.722233, -3.722233, radius);
            }
            else if (numBallsTotal == 9)
            {
                circles[0] = new Circle2D(+5.58335, +5.58335, radius);
                circles[1] = new Circle2D(8.88E-16, +5.58335, radius);
                circles[2] = new Circle2D(-5.58335, +5.58335, radius);
                circles[3] = new Circle2D(+5.58335, 8.88E-16, radius);
                circles[4] = new Circle2D(8.88E-16, 8.88E-16, radius);
                circles[5] = new Circle2D(-5.58335, 8.88E-16, radius);
                circles[6] = new Circle2D(+5.58335, -5.58335, radius);
                circles[7] = new Circle2D(8.88E-16, -5.58335, radius);
                circles[8] = new Circle2D(-5.58335, -5.58335, radius);
            }
            else if (numBallsTotal == 16)
            {
                circles[0] =  new Circle2D( 6.70002,  6.70002, radius);
                circles[1] =  new Circle2D(2.23E+00,  6.70002, radius);
                circles[2] =  new Circle2D(-2.23334,  6.70002, radius);
                circles[3] =  new Circle2D(-6.70002,  6.70E+00, radius);
                circles[4] =  new Circle2D(6.70E+00,  2.23E+00, radius);
                circles[5] =  new Circle2D( 2.23334,  2.23E+00, radius);
                circles[6] =  new Circle2D(-2.23334,  2.23334, radius);
                circles[7] =  new Circle2D(-6.70E+00, 2.23334, radius);
                circles[8] =  new Circle2D( 6.70002, -2.23334, radius);
                circles[9] =  new Circle2D( 2.23334, -2.23334, radius);
                circles[10] = new Circle2D(-2.23334, -2.23334, radius);
                circles[11] = new Circle2D(-6.70002, -2.23334, radius);
                circles[12] = new Circle2D( 6.70002, -6.70002, radius);
                circles[13] = new Circle2D( 2.23334, -6.70002, radius);
                circles[14] = new Circle2D(-2.23334, -6.70002, radius);
                circles[15] = new Circle2D(-6.70002, -6.70002, radius);
            }
            else if (numBallsTotal == 25)
            {
                circles[0] =  new Circle2D(7.444467, 7.444467, radius);
                circles[1] =  new Circle2D(3.722233, 7.444467, radius);
                circles[2] =  new Circle2D(0.00E+00, 7.444467, radius);
                circles[3] =  new Circle2D(-3.72223, 7.444467, radius);
                circles[4] =  new Circle2D(-7.44447, 7.44E+00, radius);
                circles[5] =  new Circle2D(7.44E+00, 3.72E+00, radius);
                circles[6] =  new Circle2D(3.722233, 3.72E+00, radius);
                circles[7] =  new Circle2D(0,        3.722233, radius);
                circles[8] =  new Circle2D(-3.72E+0, 3.722233, radius);
                circles[9] =  new Circle2D(-7.44447, 3.722233, radius);
                circles[10] = new Circle2D(7.444467, 0, radius);
                circles[11] = new Circle2D(3.722233, 0, radius);
                circles[12] = new Circle2D(0,        0, radius);
                circles[13] = new Circle2D(-3.72223, 0, radius);
                circles[14] = new Circle2D(-7.44447, 0, radius);
                circles[15] = new Circle2D(7.444467, -3.72223, radius);
                circles[16] = new Circle2D(3.722233, -3.72223, radius);
                circles[17] = new Circle2D(0,        -3.72223, radius);
                circles[18] = new Circle2D(-3.72223, -3.72223, radius);
                circles[19] = new Circle2D(-7.44447, -3.72223, radius);
                circles[20] = new Circle2D(7.444467, -7.44447, radius);
                circles[21] = new Circle2D(3.722233, -7.44447, radius);
                circles[22] = new Circle2D(0,        -7.44447, radius);
                circles[23] = new Circle2D(-3.72223, -7.44447, radius);
                circles[24] = new Circle2D(-7.44447, -7.44447, radius);
            }
            else throw new NotImplementedException();

            return circles;
        }
    }
}
