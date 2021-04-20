using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ISAAR.MSolve.FEM.Entities;
using ISAAR.MSolve.Logging.Interfaces;
using ISAAR.MSolve.Materials;
using ISAAR.MSolve.Materials.Interfaces;
using MGroup.XFEM.FEM.Output;
using Xunit;

namespace MGroup.XFEM.Tests.FEM
{
    /// <summary>
    /// For the .geo script that creates the mesh, see Resources\gmsh_sphere_inclusions\spheres_in_rve.geo
    /// </summary>
    public static class SphereInclusionsTest
    {
        private const string workingDirectory = @"C:\Users\Serafeim\Desktop\HEAT\elasticity\FEM\sphere_in_rve";
        private const string meshFile = workingDirectory + "\\spheres_in_rve.msh";

        private const double matrixE = 1E0, inclusionE = 1E3;

        [Fact]
        public static void Run()
        {
            var materialsOfPhysicalGroups = new Dictionary<int, IContinuumMaterial>();
            materialsOfPhysicalGroups[1] = new ElasticMaterial3D() { YoungModulus = matrixE, PoissonRatio = 0.3 };
            materialsOfPhysicalGroups[2] = new ElasticMaterial3D() { YoungModulus = inclusionE, PoissonRatio = 0.3 };
            Model model = FemUtilities.Create3DModelFromGmsh(meshFile, materialsOfPhysicalGroups);
            FemUtilities.ApplyBCsCantileverTension(model, 3);

            string outputDirectory = workingDirectory;
            ILogFactory logs = new VtkLogFactory(3, model, outputDirectory)
            {
                LogDisplacements = true,
                LogStrains = true,
                LogStresses = true
            };

            FemUtilities.RunStaticLinearAnalysis(model, logFactory: logs);
        }


    }
}
