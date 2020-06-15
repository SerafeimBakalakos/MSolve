using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Integration;
using ISAAR.MSolve.Geometry.Coordinates;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Entities;
using MGroup.XFEM.Geometry;

//TODO: Standardize the writer classes and their input. Also simplify them as much as possible (e.g. plot just points, without 
//      having attached values).
namespace MGroup.XFEM.Plotting
{
    public class IntegrationPlotter2D
    {
        private readonly XModel physicalModel;

        public IntegrationPlotter2D(XModel physicalModel)
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
            var integrationPoints = new Dictionary<CartesianPoint, double>();
            foreach (IXFiniteElement element in physicalModel.Elements)
            {
                var element2D = (IXFiniteElement2D)element;
                foreach (IElementCurveIntersection2D intersection in element2D.Intersections)
                {
                    IList<GaussPoint> gaussPoints = intersection.GetIntegrationPoints(order);
                    foreach (GaussPoint gp in gaussPoints)
                    {
                        CartesianPoint point = element2D.Interpolation.TransformNaturalToCartesian(element.Nodes, gp);
                        integrationPoints.Add(point, element.ID);
                    }
                }
            }
            using (var writer = new ISAAR.MSolve.Logging.VTK.VtkPointWriter(path))
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
            var integrationPoints = new Dictionary<CartesianPoint, double>();
            foreach (IXFiniteElement element in physicalModel.Elements)
            {
                var element2D = (IXFiniteElement2D)element;
                IReadOnlyList<GaussPoint> elementGPs = element.IntegrationBulk.GenerateIntegrationPoints(element);
                foreach (GaussPoint gp in element.IntegrationBulk.GenerateIntegrationPoints(element))
                {
                    CartesianPoint point = element2D.Interpolation.TransformNaturalToCartesian(element.Nodes, gp);
                    integrationPoints.Add(point, element.ID);
                }
            }
            using (var writer = new ISAAR.MSolve.Logging.VTK.VtkPointWriter(path))
            {
                writer.WriteScalarField("element_ids_of_gauss_points", integrationPoints);
            }
        }
    }
}
