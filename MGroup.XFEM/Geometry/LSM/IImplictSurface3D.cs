using System;
using System.Collections.Generic;
using System.Text;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Geometry.Primitives;

namespace MGroup.XFEM.Geometry.LSM
{
    public interface IImplictSurface3D : IImplicitGeometry
    {
        IIntersectionSurface3D Intersect(IXFiniteElement element);
    }
}
