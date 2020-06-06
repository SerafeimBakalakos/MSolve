﻿using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Geometry.Coordinates;
using MGroup.XFEM.Geometry.Primitives;

namespace MGroup.XFEM.Geometry.ConformingMesh
{
    public interface ITriangulator3D
    {
        IList<Tetrahedron3D> CreateMesh(IEnumerable<IPoint> points);
    }
}
