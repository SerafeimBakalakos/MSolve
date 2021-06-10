using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Output;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Materials;
using ISAAR.MSolve.Materials.Interfaces;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Entities;
using MGroup.XFEM.Output.Fields;
using MGroup.XFEM.Output.Mesh;
using MGroup.XFEM.Output.Vtk;

namespace MGroup.XFEM.Tests.Utilities
{
    public class Plotting
    {
        public static void PlotDisplacements(XModel<IXMultiphaseElement> model, IVectorView solution,
            string pathDisplacementsAtNodes, string pathDisplacementsAtGaussPoints)
        {
            // Displacements at nodes
            using (var writer = new VtkPointWriter(pathDisplacementsAtNodes))
            {
                var displacementField = new DisplacementsAtNodesField(model);
                writer.WriteVectorField("displacements", displacementField.CalcValuesAtVertices(solution));
            }

            // Displacements at Gauss Points
            using (var writer = new VtkPointWriter(pathDisplacementsAtGaussPoints))
            {
                var displacementField = new DisplacementsAtGaussPointsField(model);
                writer.WriteVectorField("displacements", displacementField.CalcValuesAtVertices(solution));
            }
        }

        public static void PlotDisplacementStrainStressFields(
            XModel<IXMultiphaseElement> model, IVectorView solution, string path)
        {
            var conformingMesh = new ConformingOutputMesh(model);
            using (var writer = new VtkFileWriter(path))
            {
                writer.WriteMesh(conformingMesh);

                var displacementField = new DisplacementField(model, conformingMesh);
                writer.WriteVector2DField("displacements", conformingMesh, displacementField.CalcValuesAtVertices(solution));

                var strainStressField = new StrainStressField(model, conformingMesh);
                (IEnumerable<double[]> strains, IEnumerable<double[]> stresses) = strainStressField.CalcTensorsAtVertices(solution);

                writer.WriteTensor2DField("strain", conformingMesh, strains);
                writer.WriteTensor2DField("stress", conformingMesh, stresses);
            }
        }

        public static void PlotTemperatureAndHeatFlux(XModel<IXMultiphaseElement> model, IVectorView solution,
            string pathTemperatureAtNodes, string pathTemperatureAtGaussPoints, string pathTemperatureField,
            string pathHeatFluxAtGaussPoints)
        {
            // Temperature at nodes
            using (var writer = new VtkPointWriter(pathTemperatureAtNodes))
            {
                var temperatureField = new TemperatureAtNodesField(model);
                writer.WriteScalarField("temperature", temperatureField.CalcValuesAtVertices(solution));
            }

            // Temperature at Gauss Points
            using (var writer = new VtkPointWriter(pathTemperatureAtGaussPoints))
            {
                var temperatureField = new TemperatureAtGaussPointsField(model);
                writer.WriteScalarField("temperature", temperatureField.CalcValuesAtVertices(solution));
            }

            // Temperature field
            var conformingMesh = new ConformingOutputMesh(model);
            using (var writer = new VtkFileWriter(pathTemperatureField))
            {
                var temperatureField = new TemperatureField(model, conformingMesh);
                writer.WriteMesh(conformingMesh);
                writer.WriteScalarField("temperature", conformingMesh, temperatureField.CalcValuesAtVertices(solution));
            }

            // Heat flux at Gauss Points
            using (var writer = new VtkPointWriter(pathHeatFluxAtGaussPoints))
            {
                var fluxField = new HeatFluxAtGaussPointsField(model);
                writer.WriteVectorField("heat_flux", fluxField.CalcValuesAtVertices(solution));
            }
        }

        public static void PlotStrainsStressesAtGaussPoints(XModel<IXMultiphaseElement> model, IVectorView solution,
            string pathStrainsAtGaussPoints, string pathStressesAtGaussPoints)
        {
            // Strains at Gauss Points
            var strainStressAtGPs = new StrainsStressesAtGaussPointsField(model);
            (Dictionary<double[], double[]> strainsAtGPs, Dictionary<double[], double[]> stressesAtGPs) = 
                strainStressAtGPs.CalcTensorsAtPoints(solution);

            using (var writer = new VtkPointWriter(pathStrainsAtGaussPoints))
            {
                writer.WriteTensor2DField("strain", strainsAtGPs);
            }

            // Stresses at Gauss Points
            using (var writer = new VtkPointWriter(pathStressesAtGaussPoints))
            {
                writer.WriteTensor2DField("stress", stressesAtGPs);
            }
        }

        public static void PrintElasticityTensor(string path, IMatrix elasticity)
        {
            using (var writer = new StreamWriter(path, true))
            {
                writer.WriteLine();
                writer.WriteLine("#################################################################");
                writer.WriteLine("Date = " + DateTime.Now);
                writer.WriteLine("elasticity = ");
            }
            var matrixWriter = new FullMatrixWriter();
            matrixWriter.WriteToFile(elasticity, path, true);
        }

        public static void PrintElasticityTensor(string path, double E, double v, int dimension)
        {
            // Print the constitutive matrix of the matrix material for comparison
            using (var writer = new StreamWriter(path, true))
            {
                writer.WriteLine();
                writer.WriteLine($"elasticity of material E = {E}, v = {v}");
            }
            IContinuumMaterial material;
            if (dimension == 2)
            {
                material = new ElasticMaterial2D(StressState2D.PlaneStress) { YoungModulus = E, PoissonRatio = v };
            }
            else
            {
                material = new ElasticMaterial3D() { YoungModulus = E, PoissonRatio = v };
            }
            var matrixWriter = new FullMatrixWriter();
            matrixWriter.WriteToFile(material.ConstitutiveMatrix, path, true);
        }

        public static void WriteNodalTemperatures(XModel<IXMultiphaseElement> model, IVectorView solution, string path)
        {
            var temperatureField = new TemperatureAtNodesField(model);
            Dictionary<double[], double> nodalValues = temperatureField.CalcValuesAtVertices(solution);
            using (var writer = new StreamWriter(path))
            {
                foreach (var pair in nodalValues)
                {
                    double[] point = pair.Key;
                    double val = pair.Value;
                    if (point.Length == 2)
                    {
                        writer.WriteLine($"{point[0]} {point[1]} 0.0 {val}");
                    }
                    else if (point.Length == 3)
                    {
                        writer.WriteLine($"{point[0]} {point[1]} {point[2]} {val}");
                    }
                    else throw new NotImplementedException();
                }
            }
        }
    }
}
