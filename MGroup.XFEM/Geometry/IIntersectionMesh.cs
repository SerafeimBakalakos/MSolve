using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Mesh;

namespace MGroup.XFEM.Geometry
{
    public interface IIntersectionMesh
    {
        int Dimension { get; }

        IList<double[]> Vertices { get; }

        IList<(CellType type, int[] connectivity)> Cells { get; }
    }
}
