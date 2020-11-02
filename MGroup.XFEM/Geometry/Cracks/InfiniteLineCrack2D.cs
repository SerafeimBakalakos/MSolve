using System;
using System.Collections.Generic;
using System.Text;
using MGroup.XFEM.Entities;
using MGroup.XFEM.Geometry.Primitives;

namespace MGroup.XFEM.Geometry.Cracks
{
    public class InfiniteLineCrack2D : ICrack2D
    {
        private readonly Line2D line;

        public InfiniteLineCrack2D(double[] point0, double[] point1)
        {
            this.line = new Line2D(point0, point1);
        }

        public TipCoordinateSystem TipSystem => null;

        public double SignedDistanceFromBody(XNode node)
        {
            return line.SignedDistanceOf(node.Coordinates);
        }

        public double SignedDistanceFromBody(XPoint point)
        {
            bool hasCartesian = point.Coordinates.TryGetValue(CoordinateSystem.GlobalCartesian, out double[] coords);
            if (!hasCartesian)
            {
                coords = point.MapCoordinates(point.ShapeFunctions, point.Element.Nodes);
                point.Coordinates[CoordinateSystem.GlobalCartesian] = coords;
            }
            return line.SignedDistanceOf(coords);
        }
    }
}
