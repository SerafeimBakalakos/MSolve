using System;
using MGroup.XFEM.Tests.Plotting;

namespace MGroup.XFEM.Tests
{
    class Program
    {
        public static void Main(string[] args)
        {
            //START HERE
            //TODO: Split these into tests using mock elements and actual ones. For the actual ones, integration points should
            //      be created and stored as planned for the analysis, instead of being created just to plot them.
            //LsmBalls2DExamples.PlotGeometryAndEntities();
            LsmBalls3DExamples.PlotGeometryAndEntities();
        }
    }
}
