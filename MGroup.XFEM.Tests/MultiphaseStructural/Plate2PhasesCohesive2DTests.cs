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
    public static class Plate2PhasesCohesive2DTests
    {
        private static readonly string outputDirectory = Path.Combine(
            Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName, "Resources", "plate_2phases_cohesive_2D_temp");
        private static readonly string expectedDirectory = Path.Combine(
            Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName, "Resources", "plate_2phases_cohesive_2D");

        private static readonly double[] minCoords = { -10.0, -10.0 };
        private static readonly double[] maxCoords = { +10.0, +10.0 };
        private const double thickness = 1.0;
        private static readonly int[] numElements = { 31, 31 };
        private const int bulkIntegrationOrder = 2, boundaryIntegrationOrder = 2;

        private const double load = 1E4;
        private const double Etop = 2E6, Ebottom = 1E6, v = 0.3;
        //private const double cohesivenessNormal = 0, cohesivenessTangent = 0;
        private const double cohesivenessNormal = 1E4, cohesivenessTangent = cohesivenessNormal;
        //private const double cohesivenessNormal = 1E11, cohesivenessTangent = cohesivenessNormal;

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
                geometryModel.InteractionObservers.Add(new LsmElementIntersectionsPlotter(outputDirectory, model));

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
            var material0 = new ElasticMaterial2D(StressState2D.PlaneStress) { YoungModulus = Etop, PoissonRatio = v };
            var material1 = new ElasticMaterial2D(StressState2D.PlaneStress) { YoungModulus = Ebottom, PoissonRatio = v };
            var interfaceMaterial = new CohesiveInterfaceMaterial2D(Matrix.CreateFromArray(new double[,]
            {
                { cohesivenessNormal, 0 },
                { 0, cohesivenessTangent }
            }));
            var materialField = new StructuralBiMaterialField2D(material0, material1, interfaceMaterial);
            materialField.PhasesWithMaterial0.Add(0);
            materialField.PhasesWithMaterial1.Add(1);

            // Setup model
            XModel<IXMultiphaseElement> model = Models.CreateQuad4Model(minCoords, maxCoords, thickness, numElements,
                bulkIntegrationOrder, boundaryIntegrationOrder, materialField);
            Models.ApplyBoundaryConditionsCantileverOpening(model, load);

            return model;
        }

        /// <summary>
        /// Top phase is the more rigid material
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        private static PhaseGeometryModel CreatePhases(XModel<IXMultiphaseElement> model)
        {
            var geometricModel = new PhaseGeometryModel(model);
            model.GeometryModel = geometricModel;
            geometricModel.Enricher = new NodeEnricherMultiphaseStructural(2, geometricModel, new NullSingularityResolver());

            var defaultPhase = new DefaultPhase();
            geometricModel.Phases[defaultPhase.ID] = defaultPhase;
            var phase1 = new LsmPhase(1, geometricModel, -1);
            geometricModel.Phases[phase1.ID] = phase1;

            double[] start = { minCoords[0] , 0.5 * (minCoords[1] + maxCoords[1]) };
            double[] end = { maxCoords[0], 0.5 * (minCoords[1] + maxCoords[1]) };
            var line = new Line2D(start, end);
            var lsmCurve = new SimpleLsm2D(1, model.XNodes, line);
            var boundary = new ClosedPhaseBoundary(1, lsmCurve, defaultPhase, phase1);
            geometricModel.PhaseBoundaries[boundary.ID] = boundary;

            defaultPhase.ExternalBoundaries.Add(boundary);
            defaultPhase.Neighbors.Add(phase1);

            phase1.ExternalBoundaries.Add(boundary);
            phase1.Neighbors.Add(defaultPhase);

            return geometricModel;
        }
    }
}