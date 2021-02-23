using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Commons;
using MGroup.XFEM.ElementGeometry;

namespace MGroup.XFEM.Geometry.LSM.Utilities
{
    /// <summary>
    /// Data transfer object for passing properties of discontinuity - element intersection points between methods or classes.
    /// </summary>
    public class IntersectionPoint
    {
        public double[] CoordinatesNatural { get; set; }

        public ElementEdge Edge { get; set; }

        public HashSet<ElementFace> Faces { get; set; }

        public double TipLevelSet { get; set; }

        public bool CoincidesWith(IntersectionPoint otherPoint, ValueComparer comparer)
        {
            int dim = this.CoordinatesNatural.Length;
            if (otherPoint.CoordinatesNatural.Length != dim)
            {
                return false;
            }    
            for (int d = 0; d < dim; ++d)
            {
                if (!comparer.AreEqual(this.CoordinatesNatural[d], otherPoint.CoordinatesNatural[d]))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
