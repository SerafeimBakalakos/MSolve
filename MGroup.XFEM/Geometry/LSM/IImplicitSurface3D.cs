using System;
using System.Collections.Generic;
using System.Text;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Geometry.Primitives;

//TODO: Perhaps can be joined with the 2D case.
namespace MGroup.XFEM.Geometry.LSM
{
    public interface IImplicitSurface3D : IImplicitGeometry
    {
        IElementSurfaceIntersection3D Intersect(IXFiniteElement element);
    }
}
