using System;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Text;
using ISAAR.MSolve.Analyzers.Multiscale;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Logging;
using ISAAR.MSolve.Problems;
using ISAAR.MSolve.Solvers.Direct;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Enrichment;
using MGroup.XFEM.Enrichment.SingularityResolution;
using MGroup.XFEM.Entities;
using MGroup.XFEM.Geometry;
using MGroup.XFEM.Geometry.ConformingMesh;
using MGroup.XFEM.Integration;
using MGroup.XFEM.Integration.Quadratures;
using MGroup.XFEM.Materials;
using MGroup.XFEM.Plotting;
using MGroup.XFEM.Plotting.Fields;
using MGroup.XFEM.Plotting.Mesh;
using MGroup.XFEM.Plotting.Writers;
using MGroup.XFEM.Tests.Utilities;

namespace MGroup.XFEM.Tests.EpoxyAg
{
    public class ExampleStochastic3D
    {
        private const string outputDirectory = @"C:\Users\Serafeim\Desktop\HEAT\2020\EpoxyAG\Stochastic3D\";
        private const string pathResults = outputDirectory + "results.txt";

        private static readonly double[] minCoords = { -1.0, -1.0, -1.0 };
        private static readonly double[] maxCoords = { +1.0, +1.0, +1.0 };
        private static readonly int[] numElements = { 45, 45, 45 };
        private const int bulkIntegrationOrder = 2, boundaryIntegrationOrder = 2;

        private const double singularityRelativeAreaTolerance = 1E-8;
        private const int defaultPhaseID = 0;

        private const int numBalls = 8;
        private const double epoxyPhaseRadius = 0.2, silverPhaseThickness = 0.1;

        private const double conductEpoxy = 1E0, conductSilver = 1E2;
        private const double conductBoundaryEpoxySilver = 1E1;
        private const double specialHeatCoeff = 1.0;


        private readonly int rngSeed;

        public ExampleStochastic3D(int rngSeed)
        {
            this.rngSeed = rngSeed;
        }

        public static void RunAll()
        {
            int numRealizations = 10;
            int initialSeed = 33;
            var rng = new Random(initialSeed);
            for (int i = 0; i < numRealizations; i++)
            {
                Console.WriteLine();
                Console.WriteLine("Realization " + i);
                int realizationSeed = rng.Next();
                var realization = new ExampleStochastic3D(realizationSeed);
                realization.Run();
            }
        }

        public void Run()
        {
            try
            {
                // Create physical model, LSM and phases
                Console.WriteLine("Creating physical and geometric models");
                (XModel model, BiMaterialField materialField) = CreateModel();
                GeometryPreprocessor3D preprocessor = CreatePhases(model, materialField);
                GeometricModel geometricModel = preprocessor.GeometricModel;

                // Geometric interactions
                Console.WriteLine("Identifying interactions between physical and geometric models");
                geometricModel.InteractWithNodes();
                geometricModel.UnifyOverlappingPhases(true);
                geometricModel.InteractWithElements();
                geometricModel.FindConformingMesh();

                // Materials
                Console.WriteLine("Creating materials");
                model.UpdateMaterials();

                // Enrichment
                Console.WriteLine("Applying enrichments");
                ISingularityResolver singularityResolver = new NullSingularityResolver();
                var nodeEnricher = new NodeEnricherMultiphase(geometricModel, singularityResolver);
                nodeEnricher.ApplyEnrichments();
                model.UpdateDofs();

                // Calculate volumes of each phase
                Dictionary<string, double> volumes = preprocessor.CalcPhaseVolumes();

                // Run homogenization analysis
                IMatrix conductivity = Analysis.RunHomogenizationAnalysis3D(model, minCoords, maxCoords);

                // Print results
                using (var writer = new StreamWriter(pathResults, true))
                {
                    writer.WriteLine();
                    writer.WriteLine("#################################################################");
                    writer.WriteLine("Date = " + DateTime.Now);
                    writer.WriteLine("Realization with seed = " + rngSeed);
                    string volumesString = Utilities.Printing.PrintVolumes(volumes);
                    writer.WriteLine(volumesString);
                    writer.WriteLine(
                        $"conductivity = [ {conductivity[0, 0]} {conductivity[0, 1]} {conductivity[0, 2]};" 
                        + $" {conductivity[1, 0]} {conductivity[1, 1]} {conductivity[1, 2]};"
                        + $" {conductivity[2, 0]} {conductivity[2, 1]} {conductivity[2, 2]} ]");
                }
            }
            catch(Exception)
            {
                using (var writer = new StreamWriter(pathResults, true))
                {
                    writer.WriteLine();
                    writer.WriteLine("#################################################################");
                    writer.WriteLine("Realization with seed = " + rngSeed);
                    writer.WriteLine("Analysis failed!");
                }
            }
        }

        private GeometryPreprocessor3D CreatePhases(XModel model, BiMaterialField materialField)
        {
            var preprocessor = new GeometryPreprocessor3D();
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

        private static (XModel, BiMaterialField) CreateModel()
        {
            // Materials
            var epoxyMaterial = new ThermalMaterial(conductEpoxy, specialHeatCoeff);
            var silverMaterial = new ThermalMaterial(conductSilver, specialHeatCoeff);
            var materialField = new BiMaterialField(epoxyMaterial, silverMaterial, conductBoundaryEpoxySilver);

            return (Models.CreateHexa8Model(minCoords, maxCoords, numElements,
                bulkIntegrationOrder, boundaryIntegrationOrder, materialField), materialField);
        }
    }
}
