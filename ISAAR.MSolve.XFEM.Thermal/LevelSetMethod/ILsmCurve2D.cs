using System.Collections.Generic;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.XFEM.Thermal.Elements;
using ISAAR.MSolve.XFEM.Thermal.Entities;
using ISAAR.MSolve.XFEM.Thermal.LevelSetMethod.MeshInteraction;

//TODO: Curves should have IDs
namespace ISAAR.MSolve.XFEM.Thermal.LevelSetMethod
{
    public interface ILsmCurve2D
    {
        double Thickness { get; } //TODO: Probably delete this

        ISet<NaturalPoint> FindConformingTriangleVertices(IXFiniteElement element, CurveElementIntersection intersection);

        CurveElementIntersection IntersectElement(IXFiniteElement element);

        double SignedDistanceOf(XNode node);

        double SignedDistanceOf(IXFiniteElement element, double[] shapeFunctionsAtNaturalPoint);
    }
}
