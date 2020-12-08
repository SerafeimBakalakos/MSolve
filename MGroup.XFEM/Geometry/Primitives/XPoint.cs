using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Entities;
using MGroup.XFEM.Phases;

namespace MGroup.XFEM.Geometry.Primitives
{
    public class XPoint
    {
        public XPoint(int dimension)
        {
            this.Dimension = dimension;
        }

        public Dictionary<CoordinateSystem, double[]> Coordinates { get; } = new Dictionary<CoordinateSystem, double[]>();

        public int Dimension { get; }

        public IXFiniteElement Element { get; set; }

        public double[] ShapeFunctions { get; set; }

        /// <summary>
        /// With respect to global cartesian system. Each row is the gradient of a shape function. Each column is the the 
        /// derivatives of all shape function with respect to the axis corresponding to that column.
        /// </summary>
        public Matrix ShapeFunctionDerivatives { get; set; }

        //MODIFICATION NEEDED: Delete this
        public IPhase Phase { get; set; }

        public double[] MapCoordinates(double[] shapeFunctions, IReadOnlyList<XNode> nodes)
        {
            int dim = nodes[0].Coordinates.Length;
            var result = new double[dim];
            for (int n = 0; n < nodes.Count; ++n)
            {
                for (int d = 0; d < dim; ++d)
                {
                    result[d] += shapeFunctions[n] * nodes[n].Coordinates[d];
                }
            }
            return result;
        }
    }
}
