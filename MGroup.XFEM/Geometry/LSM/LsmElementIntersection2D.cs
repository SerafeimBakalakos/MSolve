using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Integration;
using ISAAR.MSolve.Geometry.Coordinates;
using MGroup.XFEM.Elements;

namespace MGroup.XFEM.Geometry.LSM
{
    public class LsmElementIntersection2D : IElementCurveIntersection2D
    {
        private readonly NaturalPoint start;
        private readonly NaturalPoint end;

        public LsmElementIntersection2D(RelativePositionCurveElement relativePosition, IXFiniteElement element,
            NaturalPoint start, NaturalPoint end)
        {
            this.RelativePosition = relativePosition;
            this.Element = element;
            this.start = start;
            this.end = end;
        }

        public RelativePositionCurveElement RelativePosition { get; }

        public IXFiniteElement Element { get; } //TODO: Perhaps this should be defined in the interface

        public List<double[]> ApproximateGlobalCartesian()
        {
            var points = new List<double[]>(2);
            points.Add(Element.InterpolationStandard.TransformNaturalToCartesian(Element.Nodes, start).Coordinates);
            points.Add(Element.InterpolationStandard.TransformNaturalToCartesian(Element.Nodes, end).Coordinates);
            return points;
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
