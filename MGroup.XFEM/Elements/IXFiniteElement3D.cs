using System.Collections.Generic;
using ISAAR.MSolve.Discretization.Integration;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.Discretization.Mesh;
using ISAAR.MSolve.FEM.Interpolation;
using ISAAR.MSolve.Geometry.Coordinates;
using MGroup.XFEM.Entities;
using MGroup.XFEM.Geometry;
using MGroup.XFEM.Geometry.ConformingMesh;
using MGroup.XFEM.Integration;

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

        IReadOnlyList<ElementFace> Faces { get; }

        IIsoparametricInterpolation3D Interpolation { get; }

        List<IElementSurfaceIntersection3D> Intersections { get; }

        //Dictionary<PhaseBoundary, IElementSurfaceIntersection3D> PhaseIntersections { get; }

        //HashSet<IPhase3D> Phases { get; }

        double CalcVolume();


        //Dictionary<PhaseBoundary3D, (IReadOnlyList<GaussPoint>, IReadOnlyList<ThermalInterfaceMaterial>)> 
        //    GetMaterialsForBoundaryIntegration();
    }
}