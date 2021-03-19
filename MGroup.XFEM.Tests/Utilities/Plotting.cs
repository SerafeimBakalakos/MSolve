using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Vectors;
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
    }
}
