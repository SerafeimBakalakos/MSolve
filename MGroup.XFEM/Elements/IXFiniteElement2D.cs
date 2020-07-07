using MGroup.XFEM.Geometry.ConformingMesh;

//TODO: LSM/element interactions should probably be stored in a GeometricModel class
//TODO: Unify 2D and 3D interpolation classes and use that one.
namespace MGroup.XFEM.Elements
{
    public interface IXFiniteElement2D : IXFiniteElement
    {
        /// <summary>
        /// Will be null for elements not intersected by any interfaces
        /// </summary>
        ElementSubtriangle2D[] ConformingSubtriangles { get; set; }

    }
}