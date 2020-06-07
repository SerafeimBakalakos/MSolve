using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Integration;
using ISAAR.MSolve.Geometry.Coordinates;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Entities;

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

        //public void PlotBoundaryIntegrationPoints(string path)
        //{
        //    var integrationPoints = new HashSet<CartesianPoint>();
        //    foreach (IXFiniteElement element in physicalModel.Elements)
        //    {
        //        foreach (CurveElementIntersection intersection in element.PhaseIntersections.Values)
        //        {
        //            IReadOnlyList<GaussPoint> gaussPoints = 
        //                element.IntegrationBoundary.GenerateIntegrationPoints(element, intersection);
        //            foreach (GaussPoint gp in gaussPoints)
        //            {
        //                CartesianPoint point = element.InterpolationStandard.TransformNaturalToCartesian(element.Nodes, gp);
        //                integrationPoints.Add(point);
        //            }
        //        }
        //    }
        //    using (var writer = new Logging.VTK.VtkPointWriter(path))
        //    {
        //        writer.WritePoints(integrationPoints);
        //    }
        //}

        //public void PlotVolumeIntegrationMesh(string path)
        //{
        //    var integrationMesh = new VolumeIntegrationMesh2D(physicalModel, geometricModel);
        //    using (var writer = new Writers.VtkFileWriter(path))
        //    {
        //        writer.WriteMesh(integrationMesh);
        //    }
        //}

        public void PlotVolumeIntegrationPoints(string path)
        {
            var integrationPoints = new Dictionary<CartesianPoint, double>();
            foreach (IXFiniteElement element in physicalModel.Elements)
            {
                IReadOnlyList<GaussPoint> elementGPs = element.IntegrationBulk.GenerateIntegrationPoints(element);
                foreach (GaussPoint gp in element.IntegrationBulk.GenerateIntegrationPoints(element))
                {
                    CartesianPoint point = element.Interpolation2D.TransformNaturalToCartesian(element.Nodes, gp);
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
