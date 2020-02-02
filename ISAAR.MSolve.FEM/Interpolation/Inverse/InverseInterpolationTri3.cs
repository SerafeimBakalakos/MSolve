using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.FEM.Entities;
using ISAAR.MSolve.Geometry.Coordinates;

namespace ISAAR.MSolve.FEM.Interpolation.Inverse
{
    /// <summary>
    /// Inverse mapping of the isoparametric interpolation of a triangular finite element with 3 nodes. Since the original 
    /// mapping is linear, there are analytic formulas. WARNING: this assumes 
    /// ShapeFunctions(xi, eta) => new double[]{ xi, eta, 1-xi-eta };
    /// Authors: Serafeim Bakalakos
    /// </summary>
    public class InverseInterpolationTri3 : IInverseInterpolation2D
    {
        private readonly double x1, x2, x3, y1, y2, y3;
        private readonly double det;

        public InverseInterpolationTri3(IReadOnlyList<Node> nodes)
        {
            x1 = nodes[0].X;
            x2 = nodes[1].X;
            x3 = nodes[2].X;
            y1 = nodes[0].Y;
            y2 = nodes[1].Y;
            y3 = nodes[2].Y;
            det = (x1 - x3) * (y2 - y3) - (x2 - x3) * (y1 - y3);
        }

        public NaturalPoint TransformPointCartesianToNatural(CartesianPoint point)
        {
            double detXi = (point.X - x3) * (y2 - y3) - (x2 - x3) * (point.Y - y3);
            double detEta = (x1 - x3) * (point.Y - y3) - (point.X - x3) * (y1 - y3);
            return new NaturalPoint(detXi / det, detEta / det);
        }
    }
}
