using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Vectors;

namespace MGroup.XFEM.Geometry.HybridFries
{
    public class TriangleCell3D
    {
        /// <summary>
        /// Create a new triangle cell of the surface mesh.
        /// </summary>
        /// <param name="vertices">The order must be consistent in all triangles of the surface mesh.</param>
        public TriangleCell3D(Vertex3D[] vertices)
        {
            if (vertices.Length != 3) throw new ArgumentException();
            this.Vertices = vertices;

            var p0 = Vector.CreateFromArray(Vertices[0].CoordsGlobal);
            var p1 = Vector.CreateFromArray(Vertices[1].CoordsGlobal);
            var p2 = Vector.CreateFromArray(Vertices[2].CoordsGlobal);
            Vector p0p1 = p0 - p1;
            Vector p0p2 = p0 - p2;
            Vector n = p0p1.CrossProduct(p0p2);
            double area2 = n.Norm2();
            this.Area = 0.5 * area2;
            n.ScaleIntoThis(1.0 / area2);
            this.Normal = n.RawData;
        }

        public double Area { get; }

        public Vertex3D[] Vertices { get; }

        /// <summary>
        /// Unit normal vector. The normal vectors of all triangles of the surface mesh must be consistent
        /// </summary>
        public double[] Normal { get; }
    }
}
