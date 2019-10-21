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
    }
}
