using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Entities;
using MGroup.XFEM.Geometry;
using MGroup.XFEM.Geometry.ConformingMesh;
using MGroup.XFEM.Geometry.LSM;
using MGroup.XFEM.Geometry.Tolerances;
using MGroup.XFEM.Plotting.Fields;
using MGroup.XFEM.Plotting.Mesh;
using MGroup.XFEM.Plotting.Writers;

namespace MGroup.XFEM.Tests.Utilities
{
    public class Plotting
    {
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
