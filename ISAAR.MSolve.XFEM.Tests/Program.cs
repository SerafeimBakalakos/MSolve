using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.XFEM.Tests.HeatOLD.Plotting;
using ISAAR.MSolve.XFEM.Tests.Multiphase.Geometry;

namespace ISAAR.MSolve.XFEM.Tests
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            PhasesTests.PlotPhaseInteractions();


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
        }
    }
}
