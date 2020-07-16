using System;
using MGroup.XFEM.Tests.EpoxyAg;
using MGroup.XFEM.Tests.Plotting;
using MGroup.XFEM.Tests.Unions;

namespace MGroup.XFEM.Tests
{
    class Program
    {
        public static void Main(string[] args)
        {
            //START HERE
            //TODO: Split these into tests using mock elements and actual ones. For the actual ones, integration points should
            //      be created and stored as planned for the analysis, instead of being created just to plot them.
            //LsmBalls2DExamples.PlotSolution();
            //LsmBalls3DExamples.PlotSolution();

            //UnionTwoBalls2D.PlotGeometryAndEntities();
            //UnionTwoBalls3D.PlotGeometryAndEntities();

            //UnionTwoHollowBalls2D.PlotGeometryAndEntities();
            //UnionTwoHollowBalls3D.PlotGeometryAndEntities();

            //ExampleUniformThickness2D.PlotGeometryAndEntities();
            //ExampleUniformThickness2D.PlotSolution();

            //ExampleUniformThickness3D.PlotGeometryAndEntities();
            //ExampleUniformThickness3D.PlotSolution();

            //ExampleRandom3D.PlotGeometryAndEntities();
            //ExampleRandom3D.PlotSolution();
            ExampleRandom3D.RunHomogenization();

            //ExampleStochastic2D.RunAll();
            //ExampleStochastic3D.RunAll();
        }
    }
}
