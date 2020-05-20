using System;
using System.Collections.Generic;
using System.Text;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Geometry.Primitives;

namespace MGroup.XFEM.Geometry.LSM
{
    public interface IImplicitSurface3D : IImplicitGeometry
    {
        IElementSurfaceIntersection3D Intersect(IXFiniteElement element);
    }
}
