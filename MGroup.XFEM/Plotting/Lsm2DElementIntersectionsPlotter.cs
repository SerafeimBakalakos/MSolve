using System;
using System.Collections.Generic;
using System.Text;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Entities;
using MGroup.XFEM.Geometry;
using MGroup.XFEM.Geometry.LSM;
using MGroup.XFEM.Plotting.Mesh;
using MGroup.XFEM.Plotting.Writers;

namespace MGroup.XFEM.Plotting
{
    public class Lsm2DElementIntersectionsPlotter
    {
        private readonly XModel model;
        private readonly IEnumerable<SimpleLsm2D> curves;

        public Lsm2DElementIntersectionsPlotter(XModel model, IEnumerable<SimpleLsm2D> curves)
        {
            this.model = model;
            this.curves = curves;
        }

        public void PlotIntersections(string path)
        {
            List<LsmElementIntersection2D> intersections = CalcIntersections();
            var intersectionMesh = new LsmIntersectionSegmentsMesh2D(intersections);
            using (var writer = new VtkFileWriter(path))
            {
                writer.WriteMesh(intersectionMesh);
                writer.WriteScalarField("elementID", intersectionMesh, intersectionMesh.ParentElementIDsOfVertices);
            }
        }

        // TODO: Perhaps these can be stored somewhere else.
        private List<LsmElementIntersection2D> CalcIntersections()
        {
            var intersections = new List<LsmElementIntersection2D>();
            foreach (IImplicitCurve2D curve in curves)
            {
                foreach (IXFiniteElement element in model.Elements)
                {
                    IElementCurveIntersection2D intersection = curve.Intersect(element);
                    if (intersection.RelativePosition != RelativePositionCurveElement.Disjoint)
                    {
                        intersections.Add((LsmElementIntersection2D)intersection);
                    }
                }
            }
            return intersections;
        }
    }
}
