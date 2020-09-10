using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.XFEM_OLD.Tests.HeatOLD.Plotting;
using ISAAR.MSolve.XFEM_OLD.Tests.Multiphase.Homogenization;
using ISAAR.MSolve.XFEM_OLD.Tests.Multiphase.Plotting;

namespace ISAAR.MSolve.XFEM_OLD.Tests
{
    public static class Program
    {
        public static void Main(string[] args)
        {

            //Multiphase.Paper1.Paper1Example2.RunParametricHomogenization();
            //Multiphase.Paper1.Paper1Example2.RunSingleAnalysisAndPlotting();
            //HomogenizationExamples.RunHomogenizationAnalysis();

            //Multiphase.ExamplePhasesFromCsv.Run();

            //Multiphase.Singularity.Phases3Elements5.RunTest();
            //Multiphase.Singularity.AngledRectangle.RunTest();
            //Multiphase.Singularity.Phases2Elements3.RunTest();
            //Multiphase.Singularity.TwoVerticalInclusions.RunTest();

            //BenchmarkTests.Test2Phases();
            //ThreePhasesBenchmark.RunTest();
            //ThreePhasesBenchmarkHardcoded.RunTest();

            JunctionSingularityBenchmark.RunTest();

            //HollowPhasePlots.PlotHollowPhasesInteractionsFromCSV();
            //HollowPhasePlots.PlotHollowPhasesInteractions();
            //PhasePlots.PlotScatteredPhasesInteractions();
            //PhasePlots.PlotTetrisPhasesInteractions();
            //PhasePlots.PlotPercolationPhasesInteractions();


            //ThermalInclusionBall2D.PlotLevelSets();
            //ThermalInclusionBall2D.PlotConformingMesh();
            //ThermalInclusionBall2D.PlotTemperature();
            //ThermalInclusionBall2D.PlotTemperatureAndFlux();

            //ThermalInclusionMultipleBalls2D.PlotLevelSets();
            //ThermalInclusionMultipleBalls2D.PlotConformingMesh();
            //ThermalInclusionMultipleBalls2D.PlotTemperature();
            //ThermalInclusionMultipleBalls2D.PlotTemperatureAndFlux();

            //ThermalInclusionMultisplitBalls2D.PlotLevelSets();
            //ThermalInclusionMultisplitBalls2D.PlotConformingMesh();
            //ThermalInclusionMultisplitBalls2D.PlotTemperature();
            //ThermalInclusionMultisplitBalls2D.PlotTemperatureAndFlux();

            //ThermalInclusionCNTsNottingham.PlotLevelSets();
            //ThermalInclusionCNTsNottingham.PlotConformingMesh();
            //ThermalInclusionCNTsNottingham.PlotTemperature();
            //ThermalInclusionCNTsNottingham.PlotTemperatureAndFlux();

            //ThreewayJunction2D.PlotLevelSetsAndEnrichments();

            //ComboTest();
        }

        private static void ComboTest()
        {
            int[] options = { 5, 0, 23, 17, 36 };
            IEnumerable<int[]> combos = ISAAR.MSolve.XFEM_OLD.Multiphase.Utilities.Combinations.FindAllCombos(options, 3);
            foreach (int[] combo in combos)
            {
                for (int i = 0; i < combo.Length; ++i)
                {
                    Console.Write(combo[i] + " ");
                }
                Console.WriteLine();
            }
        }
    }
}
