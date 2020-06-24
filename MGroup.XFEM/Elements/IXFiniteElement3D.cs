using System.Collections.Generic;
using ISAAR.MSolve.FEM.Interpolation;
using MGroup.XFEM.Geometry;
using MGroup.XFEM.Geometry.ConformingMesh;

//TODO: LSM/element interactions should probably be stored in a GeometricModel class
//TODO: Unify 2D and 3D interpolation classes and use that one.
namespace MGroup.XFEM.Elements
{
    public interface IXFiniteElement3D : IXFiniteElement
    {
        /// <summary>
        /// Will be null for elements not intersected by any interfaces
        /// </summary>
        ElementSubtetrahedron3D[] ConformingSubtetrahedra { get; set; }

        List<IElementSurfaceIntersection3D> Intersections { get; }
    }
}