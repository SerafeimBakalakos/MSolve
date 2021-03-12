using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Vectors;

//TODO: Consider exposing Vector instead of double[]
namespace MGroup.XFEM.Geometry.HybridFries
{
    /// <summary>
    /// See "Crack propagation with the XFEM and a hybrid explicit-implicit crack description, Fries & Baydoun, 2012", 
    /// section 3.2.4
    /// </summary>
    public class CrackFrontSystem3D
    {
        public CrackFrontSystem3D(Vertex3D vertex, Vertex3D previous, Vertex3D next)
        {
            // Normal
            var normal = Vector.CreateZero(3);
            double totalArea = 0.0;
            foreach (TriangleCell3D triangle in vertex.Cells)
            {
                normal.AxpyIntoThis(Vector.CreateFromArray(triangle.Normal), triangle.Area);
                totalArea += triangle.Area;
            }
            normal.ScaleIntoThis(1.0 / normal.Norm2());
            this.Normal = normal.RawData;

            // Tangent
            var v0 = Vector.CreateFromArray(previous.CoordsGlobal);
            var v1 = Vector.CreateFromArray(vertex.CoordsGlobal);
            var v2 = Vector.CreateFromArray(next.CoordsGlobal);
            Vector q0 = v1 - v0;
            Vector q1 = v2 - v1;
            double l0 = q0.Norm2();
            double l1 = q1.Norm2();
            Vector tangent = (1 / (l0 + l1)) * (l0 * q0 + l1 * q1);
            tangent.ScaleIntoThis(1.0 / tangent.Norm2());
            this.Tangent = tangent.RawData;

            // Extension
            Vector extension = tangent.CrossProduct(normal);
            this.Extension = extension.RawData;
        }

        /// <summary>
        /// This is not necessarily a unit vector. It is orthogonal to <see cref="Normal"/> and <see cref="Tangent"/>.
        /// </summary>
        public double[] Extension { get; }

        /// <summary>
        /// Unit vector. Orthogonal to <see cref="Extension"/> and linearly independent to <see cref="Tangent"/>.
        /// </summary>
        public double[] Normal { get; }

        /// <summary>
        /// Unit vector. Orthogonal to <see cref="Extension"/> and linearly independent to <see cref="Normal"/>.
        /// </summary>
        public double[] Tangent { get; }
    }
}
