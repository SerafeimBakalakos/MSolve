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
    public class LsmElementIntersectionsPlotter
    {
        public void PlotIntersections(string path, IEnumerable<IElementGeometryIntersection> intersections)
        {
            var intersectionMesh = new LsmIntersectionSegmentsMesh(intersections);
            using (var writer = new VtkFileWriter(path))
            {
                writer.WriteMesh(intersectionMesh);
                writer.WriteScalarField("elementID", intersectionMesh, intersectionMesh.ParentElementIDsOfVertices);
            }
        }
    }
}
