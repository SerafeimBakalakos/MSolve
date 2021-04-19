using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ISAAR.MSolve.FEM.Entities;
using ISAAR.MSolve.Logging.Interfaces;
using ISAAR.MSolve.Logging.VTK;
using ISAAR.MSolve.Materials;
using Xunit;

namespace MGroup.XFEM.Tests.FEM
{
    public static class SphereInclusionTest
    {
        private const string workingDirectory = @"C:\Users\Serafeim\Desktop\HEAT\elasticity\FEM\sphere_in_rve";
        private const string meshFile = workingDirectory + "\\spheres_in_rve.msh";

        [Fact]
        public static void Run()
        {
            Model model = FemUtilities.Create3DModelFromGmsh(meshFile);
            FemUtilities.ApplyBCsCantileverTension(model, 3);

            string outputDirectory = workingDirectory;
            //ILogFactory logs = new VtkLogFactory(model, outputDirectory)
            //{
            //    LogDisplacements = true,
            //    LogStrains = true,
            //    LogStresses = true
            //};

            FemUtilities.RunStaticLinearAnalysis(model, null/*logFactory: logs*/);
        }


    }
}
