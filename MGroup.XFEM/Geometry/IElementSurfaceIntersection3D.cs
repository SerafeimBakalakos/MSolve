using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Integration;
using ISAAR.MSolve.Geometry.Coordinates;

//TODO: This must be joined with the 2D interface. An intersection mesh class should be used there as well.
//      Furthermore intersection mesh class should be simplified. Its vertices should have IDs used by clients for adding 
//      vertices and cells. The coordinates should be as double[] and in many systems, which will be lazily evaluated in an 
//      accessor.
namespace MGroup.XFEM.Geometry
{
    /// <summary>
    /// A surface resulting from the intersection of a parent surface with a 3D finite element.
    /// Degenerate cases are also possible: null, single point or single curve.
    /// </summary>
    public interface IElementSurfaceIntersection3D
    {
        RelativePositionCurveElement RelativePosition { get; }

        IntersectionMesh<CartesianPoint> ApproximateGlobalCartesian();

        /// <summary>
        /// The weights of the returned <see cref="GaussPoint"/>s include the determinant of the Jacobian from the
        /// natural system of the element to the global cartesian system.
        /// </summary>
        /// <param name="order"></param>
        GaussPoint[] GetIntegrationPoints(int order);

        IList<NaturalPoint> GetPointsForTriangulation(); 
    }
}
