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
            string pathDisplacementsAtNodes, string pathDisplacementsAtGaussPoints, string pathDisplacementsField)
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

            // Displacement field
            var conformingMesh = new ConformingOutputMesh(model);
            using (var writer = new VtkFileWriter(pathDisplacementsField))
            {
                var displacementField = new DisplacementField(model, conformingMesh);
                writer.WriteMesh(conformingMesh);
                writer.WriteVector2DField("displacements", conformingMesh, displacementField.CalcValuesAtVertices(solution));
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
    }
}
