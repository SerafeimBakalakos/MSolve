using System;
using System.Collections.Generic;
using System.Text;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Entities;
using MGroup.XFEM.Geometry.Primitives;

namespace MGroup.XFEM.Geometry.LSM
{
    public interface IImplictCurve2D : IImplicitGeometry
    {
        IIntersectionCurve2D Intersect(IXFiniteElement element);
    }
}
