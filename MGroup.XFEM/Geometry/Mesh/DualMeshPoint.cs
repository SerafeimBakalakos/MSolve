using System;
using System.Collections.Generic;
using System.Text;

namespace MGroup.XFEM.Geometry.Mesh
{
    public class DualMeshPoint
    {
        public int[] LsmElementIdx { get; set; }

        public double[] LsmNaturalCoordinates { get; set; }

        public double[] LsmShapeFunctions { get; set; }
    }
}
