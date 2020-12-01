using System;
using System.Collections.Generic;
using System.Text;
using MGroup.XFEM.Integration;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Entities;
using MGroup.XFEM.Geometry;
using MGroup.XFEM.Plotting.Writers;

//TODO: Standardize the writer classes and their input. Also simplify them as much as possible (e.g. plot just points, without 
//      having attached values).
namespace MGroup.XFEM.Plotting
{
    public class IntegrationPlotter_OLD
    {
        private readonly XModel<IXMultiphaseElement> physicalModel;

        public IntegrationPlotter_OLD(XModel<IXMultiphaseElement> physicalModel)
        {
            this.physicalModel = physicalModel;
        }
        
        public void PlotBoundaryIntegrationPoints(string path, int order)
        {
            var integrationPoints = new Dictionary<double[], double>();
            foreach (IXFiniteElement element in physicalModel.Elements)
            {
                foreach (IElementDiscontinuityInteraction interaction in element.InteractingDiscontinuities.Values)
                {
                    IReadOnlyList<GaussPoint> gaussPoints = interaction.GetBoundaryIntegrationPoints(order);
                    foreach (GaussPoint gp in gaussPoints)
                    {
                        double[] point = element.Interpolation.TransformNaturalToCartesian(element.Nodes, gp.Coordinates);
                        integrationPoints.Add(point, element.ID);
                    }
                }
            }
            using (var writer = new VtkPointWriter(path))
            {
                writer.WriteScalarField("elem_ids_of_boundary_gauss_points", integrationPoints);
            }
        }


        public void PlotBulkIntegrationPoints(string path)
        {
            var integrationPoints = new Dictionary<double[], double>();
            foreach (IXFiniteElement element in physicalModel.Elements)
            {
                IReadOnlyList<GaussPoint> elementGPs = element.IntegrationBulk.GenerateIntegrationPoints(element);
                foreach (GaussPoint gp in element.IntegrationBulk.GenerateIntegrationPoints(element))
                {
                    double[] point = element.Interpolation.TransformNaturalToCartesian(element.Nodes, gp.Coordinates);
                    integrationPoints.Add(point, element.ID);
                }
            }
            using (var writer = new VtkPointWriter(path))
            {
                writer.WriteScalarField("element_ids_of_gauss_points", integrationPoints);
            }
        }
    }
}
