using System.Collections.Generic;
using ISAAR.MSolve.Geometry.Coordinates;

namespace ISAAR.MSolve.Geometry.Triangulation
{
    public interface ITriangulator2D<TVertex> where TVertex : IPoint
    {
        List<Triangle2D<TVertex>> CreateMesh(IEnumerable<TVertex> points);
        List<Triangle2D<TVertex>> CreateMesh(IEnumerable<TVertex> points, double maxTriangleArea);
    }
}
