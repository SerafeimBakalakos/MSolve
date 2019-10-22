using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Integration;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.XFEM.Thermal.Elements;
using ISAAR.MSolve.XFEM.Thermal.Entities;
using ISAAR.MSolve.XFEM.Thermal.LevelSetMethod.MeshInteraction;

namespace ISAAR.MSolve.XFEM.Thermal.LevelSetMethod
{
    public interface ILsmCurve2D
    {
        double Thickness { get; } //TODO: Probably delete this

        CurveElementIntersection IntersectElement(IXFiniteElement element);

        double SignedDistanceOf(XNode node);

        double SignedDistanceOf(IXFiniteElement element, double[] shapeFunctionsAtNaturalPoint);

        /// <summary>
        /// If the curve intersects the element, it is divided into subtriangles that are not intersected by the curve and
        /// true is returned. If the curve has no common points with the element or is tangent to it, fals is returned and no 
        /// triangulation takes place. The triangles are defined in the natural system of the element.
        /// </summary>
        /// <param name="element"></param>
        /// <param name="intersection"></param>
        /// <param name="subtriangles">
        /// Will be null if the curve has no common points with the element or is tangent to it.
        /// </param>
        bool TryConformingTriangulation(IXFiniteElement element, CurveElementIntersection intersection,
            out IReadOnlyList<ElementSubtriangle> subtriangles);
    }
}
