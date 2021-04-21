using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Input;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Output;
using ISAAR.MSolve.LinearAlgebra.Output.Formatting;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Materials;
using ISAAR.MSolve.Solvers.Direct;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Entities;
using MGroup.XFEM.Multiscale;
using MGroup.XFEM.Multiscale.RveBuilders;
using MGroup.XFEM.Output.Writers;
using MGroup.XFEM.Phases;
using MGroup.XFEM.Tests.Utilities;
using Xunit;

namespace MGroup.XFEM.Tests.Multiscale
{
    public static class ElasticRveTests
    {
        private const string outputDirectory = @"C:\Users\Serafeim\Desktop\HEAT\elasticity\homogenization_training\";
        //private const string outputPath = @"C:\Users\Serafeim\Desktop\HEAT\elasticity\homogenization_training\rve_results.txt";
        private const double matrixE = 1, inclusionE = 1, v = 0.3;


        [Fact]
        public static void TestSolution()
        {
            var matrixMaterial = new ElasticMaterial3D() { YoungModulus = matrixE, PoissonRatio = v };
            var inclusionMaterial = new ElasticMaterial3D() { YoungModulus = inclusionE, PoissonRatio = v };

            var rveGenerator = new RveRandomSphereInclusions();
            rveGenerator.Seed = 13;
            rveGenerator.NumRealizations = 100;
            rveGenerator.MinStrains = new double[] { -1, -1, -1, -1, -1, -1 };
            rveGenerator.MaxStrains = new double[] { +1, +1, +1, +1, +1, +1 };
            rveGenerator.VolumeFraction = 0.13;
            rveGenerator.MatrixMaterial = matrixMaterial;
            rveGenerator.InclusionMaterial = inclusionMaterial;
            rveGenerator.SolverBuilder = new SuiteSparseSolver.Builder();
            rveGenerator.LsmMeshRefinementLevel = 1;

            XModel<IXMultiphaseElement> model = rveGenerator.CreateModelPhysical();
            Models.ApplyBCsCantileverTension(model);
            PhaseGeometryModel geometryModel = rveGenerator.CreatePhases(model);

            // Observers
            geometryModel.InteractionObservers.Add(new LsmElementIntersectionsPlotter(outputDirectory, model));
            model.ModelObservers.Add(new ConformingMeshPlotter(outputDirectory, model));

            // Run analysis
            model.Initialize();
            IVectorView solution = Analysis.RunStructuralStaticAnalysis(model, rveGenerator.SolverBuilder);

            // Plot displacements, strains, stresses
            var computedFiles = new List<string>();
            computedFiles.Add(Path.Combine(outputDirectory, "displacement_nodes_t0.vtk"));
            computedFiles.Add(Path.Combine(outputDirectory, "displacement_gauss_points_t0.vtk"));
            computedFiles.Add(Path.Combine(outputDirectory, "strains_gauss_points_t0.vtk"));
            computedFiles.Add(Path.Combine(outputDirectory, "stresses_gauss_points_t0.vtk"));
            computedFiles.Add(Path.Combine(outputDirectory, "displacement_strain_stress_field_t0.vtk"));
            //Utilities.Plotting.PlotDisplacements(model, solution, computedFiles[0], computedFiles[1]);
            Utilities.Plotting.PlotStrainsStressesAtGaussPoints(model, solution, computedFiles[2], computedFiles[3]);
            Utilities.Plotting.PlotDisplacementStrainStressFields(model, solution, computedFiles[4]);
        }

        [Fact]
        public static void TestHomogenization()
        {
            var matrixMaterial = new ElasticMaterial3D() { YoungModulus = matrixE, PoissonRatio = v };
            var inclusionMaterial = new ElasticMaterial3D() { YoungModulus = inclusionE, PoissonRatio = v };

            var rveGenerator = new RveRandomSphereInclusions();
            rveGenerator.Seed = 13;
            rveGenerator.NumRealizations = 100;
            rveGenerator.MinStrains = new double[] { -1, -1, -1, -1, -1, -1 };
            rveGenerator.MaxStrains = new double[] { +1, +1, +1, +1, +1, +1 };
            rveGenerator.VolumeFraction = 0.10;
            rveGenerator.MatrixMaterial = matrixMaterial;
            rveGenerator.InclusionMaterial = inclusionMaterial;
            rveGenerator.SolverBuilder = new SuiteSparseSolver.Builder();

            rveGenerator.RunAnalysis();

            string path = outputDirectory + "\\rve_results.txt";
            var textWriter = new StreamWriter(path);
            //var matrixWriter = new FullMatrixWriter();
            //var vectorWriter = new Array1DWriter();
            //vectorWriter.ArrayFormat = Array1DFormat.PlainHorizontal;
            for (int r = 0; r < rveGenerator.NumRealizations; ++r)
            {
                textWriter.WriteLine("********************************************");
                textWriter.WriteLine($"Realization {r}:");
                textWriter.WriteLine();

                textWriter.WriteLine("Strains: ");
                double[] e = rveGenerator.Strains[r];
                for (int i = 0; i < e.Length; ++i)
                {
                    textWriter.Write(e[i] + " ");
                }
                //vectorWriter.WriteToFile(rveGenerator.Strains[r], outputPath, true);
                textWriter.WriteLine();
                textWriter.WriteLine();

                textWriter.WriteLine("Stresses: ");
                double[] s = rveGenerator.Stresses[r];
                for (int i = 0; i < s.Length; ++i)
                {
                    textWriter.Write(s[i] + " ");
                }
                //vectorWriter.WriteToFile(rveGenerator.Stresses[r], outputPath, true);
                textWriter.WriteLine();
                textWriter.WriteLine();

                textWriter.WriteLine("Elasticity tensor: ");
                IMatrixView C = rveGenerator.ConstitutiveMatrices[r];
                for (int i = 0; i < C.NumRows; ++i)
                {
                    for (int j = 0; j < C.NumColumns; ++j)
                    {
                        textWriter.Write(C[i, j] + " ");
                    }
                    textWriter.WriteLine();
                }
                //matrixWriter.WriteToFile(rveGenerator.ElasticityTensors[r], outputPath, true);
                textWriter.WriteLine();
                textWriter.WriteLine();
                textWriter.WriteLine();
            }
            textWriter.Dispose();
        }
    }
}
