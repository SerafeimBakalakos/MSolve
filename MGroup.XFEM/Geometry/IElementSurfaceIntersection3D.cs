using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Integration;

//TODO: Perhaps this can be joined with the 2D interface
namespace MGroup.XFEM.Geometry
{
    /// <summary>
    /// A surface resulting from the intersection of a parent surface with a 3D finite element.
    /// Degenerate cases are also possible: null, single point or single curve.
    /// </summary>
    public interface IElementSurfaceIntersection3D
    {
        RelativePositionCurveElement RelativePosition { get; }

        IntersectionMesh ApproximateGlobalCartesian();

        GaussPoint[] GetIntegrationPoints(int numPoints);

        IList<double[]> GetPointsForTriangulation(); 
    }
}
