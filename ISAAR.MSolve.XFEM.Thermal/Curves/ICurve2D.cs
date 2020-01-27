using System.Collections.Generic;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.XFEM.ThermalOLD.Elements;
using ISAAR.MSolve.XFEM.ThermalOLD.Entities;
using ISAAR.MSolve.XFEM.ThermalOLD.Curves.MeshInteraction;

//TODO: Curves should have IDs
//TODO: Duplication between this and Geometry.Shapes.ICurve2D
namespace ISAAR.MSolve.XFEM.ThermalOLD.Curves
{
    public interface ICurve2D
    {
        double Thickness { get; } //TODO: Probably delete this

        ISet<NaturalPoint> FindConformingTriangleVertices(IXFiniteElement element, CurveElementIntersection intersection);

        CurveElementIntersection IntersectElement(IXFiniteElement element);

        double SignedDistanceOf(XNode node);

        double SignedDistanceOf(IXFiniteElement element, double[] shapeFunctionsAtNaturalPoint);
    }
}
