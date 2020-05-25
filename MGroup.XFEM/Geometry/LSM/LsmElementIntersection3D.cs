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
        private readonly IntersectionMesh<NaturalPoint> intersectionMesh;

        public LsmElementIntersection3D(RelativePositionCurveElement relativePosition, IXFiniteElement element,
            IntersectionMesh<NaturalPoint> intersectionMesh)
        {
            this.RelativePosition = relativePosition;
            this.Element = element;
            this.intersectionMesh = intersectionMesh;
        }

        public RelativePositionCurveElement RelativePosition { get; }

        public IXFiniteElement Element { get; } //TODO: Perhaps this should be defined in the interface

        public IntersectionMesh<CartesianPoint> ApproximateGlobalCartesian()
        {
            var meshCartesian = new IntersectionMesh<CartesianPoint>();
            NaturalPoint[] verticesNatural = intersectionMesh.GetVerticesList();
            foreach (NaturalPoint vertexNatural in verticesNatural)
            {
                CartesianPoint vertexCartesian = ((MockElement)Element).Interpolation3D.TransformNaturalToCartesian(
                    Element.Nodes, vertexNatural);
                meshCartesian.AddVertex(vertexCartesian);
            }
            meshCartesian.Cells = intersectionMesh.Cells;
            return meshCartesian;
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
