using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Geometry.Coordinates;

namespace MGroup.XFEM.Geometry.ConformingMesh
{
    public interface ITriangulator3D
    {
        IList<TetrahedronCell3D> CreateMesh(IEnumerable<IPoint> points);
    }
}
