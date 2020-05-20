using System;
using System.Collections.Generic;
using System.Text;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Entities;
using MGroup.XFEM.Geometry.Primitives;

namespace MGroup.XFEM.Geometry.LSM
{
    public interface IImplicitCurve2D : IImplicitGeometry
    {
        IElementCurveIntersection2D Intersect(IXFiniteElement element);
    }
}
