using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.XFEM.Tests.HEAT.Plotting;

namespace ISAAR.MSolve.XFEM.Tests
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            //ThermalInclusionBall2D.PlotLevelSets();
            //ThermalInclusionBall2D.PlotConformingMesh();
            //ThermalInclusionBall2D.PlotTemperature();
            ThermalInclusionBall2D.PlotTemperatureAndFlux();
        }
    }
}
