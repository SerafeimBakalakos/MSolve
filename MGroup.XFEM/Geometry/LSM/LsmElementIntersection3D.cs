using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using ISAAR.MSolve.Discretization.Integration;
using ISAAR.MSolve.Geometry.Coordinates;
using MGroup.XFEM.Elements;

namespace MGroup.XFEM.Geometry.LSM
{
    public class LsmElementIntersection3D : IElementSurfaceIntersection3D
    {
        private readonly IntersectionMesh intersectionMesh;

        public LsmElementIntersection3D(RelativePositionCurveElement relativePosition, IXFiniteElement element,
            IntersectionMesh intersectionMesh)
        {
            this.RelativePosition = relativePosition;
            this.Element = element;
            this.intersectionMesh = intersectionMesh;
        }

        public RelativePositionCurveElement RelativePosition { get; }

        public IXFiniteElement Element { get; } //TODO: Perhaps this should be defined in the interface

        public IntersectionMesh ApproximateGlobalCartesian() => intersectionMesh;

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
