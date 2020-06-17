using System;
using System.Collections.Generic;
using System.Text;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Entities;
using MGroup.XFEM.Geometry;
using MGroup.XFEM.Geometry.LSM;
using MGroup.XFEM.Plotting.Mesh;
using MGroup.XFEM.Plotting.Writers;

namespace MGroup.XFEM.Plotting.Writers
{
    public class Lsm3DElementIntersectionsPlotter
    {
        private readonly XModel model;
        private readonly IEnumerable<SimpleLsm3D> curves;

        public Lsm3DElementIntersectionsPlotter(XModel model, IEnumerable<SimpleLsm3D> curves)
        {
            this.model = model;
            this.curves = curves;
        }

        //public void PlotIntersections(string path)
        //{
        //    List<LsmElementIntersection3D> intersections = CalcIntersections();
        //    var intersectionMesh = new LsmIntersectionSegmentsMesh3D(intersections);
        //    using (var writer = new VtkFileWriter(path))
        //    {
        //        writer.WriteMesh(intersectionMesh);
        //        writer.WriteScalarField("elementID", intersectionMesh, intersectionMesh.ParentElementIDsOfVertices);
        //    }
        //}

        public void PlotIntersections(string path, IEnumerable<LsmElementIntersection3D> intersections)
        {
            var intersectionMesh = new LsmIntersectionSegmentsMesh3D(intersections);
            using (var writer = new VtkFileWriter(path))
            {
                writer.WriteMesh(intersectionMesh);
                writer.WriteScalarField("elementID", intersectionMesh, intersectionMesh.ParentElementIDsOfVertices);
            }
        }

        //// TODO: Perhaps these can be stored somewhere else.
        //private List<LsmElementIntersection3D> CalcIntersections()
        //{
        //    var intersections = new List<LsmElementIntersection3D>();
        //    foreach (IImplicitSurface3D curve in curves)
        //    {
        //        foreach (IXFiniteElement element in model.Elements)
        //        {
        //            IElementSurfaceIntersection3D intersection = curve.Intersect(element);
        //            if (intersection.RelativePosition != RelativePositionCurveElement.Disjoint)
        //            {
        //                intersections.Add((LsmElementIntersection3D)intersection);
        //            }
        //        }
        //    }
        //    return intersections;
        //}
    }
}
