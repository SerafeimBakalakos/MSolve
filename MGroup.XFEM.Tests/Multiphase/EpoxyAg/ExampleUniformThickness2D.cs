﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Entities;
using MGroup.XFEM.Materials;
using MGroup.XFEM.Output;
using MGroup.XFEM.Output.Writers;
using MGroup.XFEM.Tests.Utilities;

namespace MGroup.XFEM.Tests.Multiphase.EpoxyAg
{
    public static class ExampleUniformThickness2D
    {
        private static readonly string outputDirectory = @"C:\Users\Serafeim\Desktop\HEAT\2020\EpoxyAG\UniformThickness2D\";

        private static readonly double[] minCoords = { -1.0, -1.0 };
        private static readonly double[] maxCoords = { +1.0, +1.0 };
        private const double thickness = 1.0;
        private static readonly int[] numElements = { 45, 45 };
        private const int bulkIntegrationOrder = 2, boundaryIntegrationOrder = 2;

        private const int defaultPhaseID = 0;

        private const int numBalls = 8, rngSeed = 33;
        private const double epoxyPhaseRadius = 0.2, silverPhaseThickness = 0.1;

        private const double conductEpoxy = 1E0, conductSilver = 1E2;
        private const double conductBoundaryEpoxySilver = 1E1;
        private const double specialHeatCoeff = 1.0;

        public static void RunModelCreation()
        {
            // Create model and LSM
            (XModel<IXMultiphaseElement> model, BiMaterialField materialField) = CreateModel();
            model.FindConformingSubcells = true;
            GeometryPreprocessor2D geometryPreprocessor = CreatePhases(model, materialField);
            var geometryModel = geometryPreprocessor.GeometryModel;

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
            model.ModelObservers.Add(new IntegrationPointsPlotter(outputDirectory, model));

            // Plot enrichments
            double elementSize = (maxCoords[0] - minCoords[0]) / numElements[0];
            model.RegisterEnrichmentObserver(new PhaseEnrichmentPlotter(outputDirectory, model, elementSize, 2));

            // Initialize model state so that everything described above can be tracked
            model.Initialize();

            Console.WriteLine(geometryPreprocessor.PrintVolumes());
        }

        public static void RunAnalysis()
        {
            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            // Create model and LSM
            (XModel<IXMultiphaseElement> model, BiMaterialField materialField) = CreateModel();
            model.FindConformingSubcells = true;
            GeometryPreprocessor2D geometryPreprocessor = CreatePhases(model, materialField);

            // Run analysis
            model.Initialize();
            IVectorView solution = Analysis.RunStaticAnalysis(model);

            // Plot temperature and heat flux
            var computedFiles = new List<string>();
            computedFiles.Add(Path.Combine(outputDirectory, "temperature_nodes_t0.vtk"));
            computedFiles.Add(Path.Combine(outputDirectory, "temperature_gauss_points_t0.vtk"));
            computedFiles.Add(Path.Combine(outputDirectory, "temperature_field_t0.vtk"));
            computedFiles.Add(Path.Combine(outputDirectory, "heat_flux_gauss_points_t0.vtk"));
            Utilities.Plotting.PlotTemperatureAndHeatFlux(model, solution,
                computedFiles[0], computedFiles[1], computedFiles[2], computedFiles[3]);
        }

        private static (XModel<IXMultiphaseElement>, BiMaterialField) CreateModel()
        {
            // Materials
            var epoxyMaterial = new ThermalMaterial(conductEpoxy, specialHeatCoeff);
            var silverMaterial = new ThermalMaterial(conductSilver, specialHeatCoeff);
            var materialField = new BiMaterialField(epoxyMaterial, silverMaterial, conductBoundaryEpoxySilver);

            return (Models.CreateQuad4Model(minCoords, maxCoords, thickness, numElements,
                bulkIntegrationOrder, boundaryIntegrationOrder, materialField), materialField);
        }

        private static GeometryPreprocessor2D CreatePhases(XModel<IXMultiphaseElement> model, BiMaterialField materialField)
        {
            var preprocessor = new GeometryPreprocessor2D(model);
            preprocessor.MinCoordinates = minCoords;
            preprocessor.MaxCoordinates = maxCoords;
            preprocessor.NumBalls = numBalls;
            preprocessor.RngSeed = rngSeed;
            preprocessor.RadiusEpoxyPhase = epoxyPhaseRadius;
            preprocessor.ThicknessSilverPhase = silverPhaseThickness;

            preprocessor.GeneratePhases(model);
            materialField.PhasesWithMaterial0.Add(preprocessor.MatrixPhaseID);
            foreach (int p in preprocessor.EpoxyPhaseIDs) materialField.PhasesWithMaterial0.Add(p);
            foreach (int p in preprocessor.SilverPhaseIDs) materialField.PhasesWithMaterial1.Add(p);

            return preprocessor;
        }
    }
}
