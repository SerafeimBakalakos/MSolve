using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using ISAAR.MSolve.Discretization.Integration;
using ISAAR.MSolve.Discretization.Mesh;
using ISAAR.MSolve.Discretization.Mesh.Generation;
using ISAAR.MSolve.Geometry.Coordinates;
using MGroup.XFEM.Elements;

namespace MGroup.XFEM.Geometry.LSM
{
    public class LsmElementIntersection3DSingleTriangle : IElementSurfaceIntersection3D
    {
        private readonly NaturalPoint[] vertices;

        public LsmElementIntersection3DSingleTriangle(RelativePositionCurveElement relativePosition, IXFiniteElement element,
            IEnumerable<NaturalPoint> vertices)
        {
            this.RelativePosition = relativePosition;
            this.Element = element;
            this.vertices = vertices.ToArray(); //TODO: Perhaps some reordering should be applied
            Debug.Assert(this.vertices.Length == 3);
        }

        public RelativePositionCurveElement RelativePosition { get; }

        public IXFiniteElement Element { get; } //TODO: Perhaps this should be defined in the interface

        public IntersectionMesh ApproximateGlobalCartesian()
        {
            var mesh = new IntersectionMesh();
            mesh.Vertices = vertices;
            mesh.CellTypes = new CellType[] { CellType.Tri3 };
            mesh.CellConnectivities = new List<int[]>();
            mesh.CellConnectivities.Add(new int[] { 0, 1, 2 });
            return mesh;
        }

        public GaussPoint[] GetIntegrationPoints(int numPoints)
        {
            throw new NotImplementedException();
        }

        public IList<double[]> GetPointsForTriangulation()
        {
            throw new NotImplementedException();
        }
    }
}
