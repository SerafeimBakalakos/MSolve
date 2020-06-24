using System;
using System.Collections.Generic;
using System.Text;
using MGroup.XFEM.Integration;
using ISAAR.MSolve.Geometry.Coordinates;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Entities;
using MGroup.XFEM.Geometry;
using MGroup.XFEM.Integ;
using MGroup.XFEM.Plotting.Writers;

namespace MGroup.XFEM.Plotting
{
    public class IntegrationPlotter3D
    {
        private readonly XModel physicalModel;

        public IntegrationPlotter3D(XModel physicalModel)
        {
            this.physicalModel = physicalModel;
        }

        //public void PlotBoundaryIntegrationMesh(string pathCells, string pathVertices)
        //{
        //    var integrationMesh = new BoundaryIntegrationMesh2D(physicalModel);
        //    using (var writer = new Writers.VtkFileWriter(pathCells))
        //    {
        //        writer.WriteMesh(integrationMesh);
        //    }
        //    using (var writer = new Logging.VTK.VtkPointWriter(pathVertices))
        //    {
        //        writer.WritePoints(integrationMesh.OutVertices);
        //    }
        //}

        public void PlotBoundaryIntegrationPoints(string path, int order)
        {
            var integrationPoints = new Dictionary<double[], double>();
            foreach (IXFiniteElement element in physicalModel.Elements)
            {
                var element3D = (IXFiniteElement3D)element;
                foreach (IElementSurfaceIntersection3D intersection in element3D.Intersections)
                {
                    IReadOnlyList<GaussPoint> gaussPoints = intersection.GetIntegrationPoints(order);
                    foreach (GaussPoint gp in gaussPoints)
                    {
                        double[] point = element3D.Interpolation.TransformNaturalToCartesian(element.Nodes, gp.Coordinates);
                        integrationPoints.Add(point, element.ID);
                    }
                }
            }
            using (var writer = new VtkPointWriter(path))
            {
                writer.WriteScalarField("elem_ids_of_boundary_gauss_points", integrationPoints);
            }
        }

        //public void PlotVolumeIntegrationMesh(string path)
        //{
        //    var integrationMesh = new VolumeIntegrationMesh2D(physicalModel, geometricModel);
        //    using (var writer = new Writers.VtkFileWriter(path))
        //    {
        //        writer.WriteMesh(integrationMesh);
        //    }
        //}

        public void PlotBulkIntegrationPoints(string path)
        {
            var integrationPoints = new Dictionary<double[], double>();
            foreach (IXFiniteElement element in physicalModel.Elements)
            {
                var element3D = (IXFiniteElement3D)element;
                IReadOnlyList<GaussPoint> elementGPs = element.IntegrationBulk.GenerateIntegrationPoints(element);
                foreach (GaussPoint gp in element.IntegrationBulk.GenerateIntegrationPoints(element))
                {
                    double[] point = element3D.Interpolation.TransformNaturalToCartesian(element.Nodes, gp.Coordinates);
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
