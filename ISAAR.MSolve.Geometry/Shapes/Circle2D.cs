using System;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.LinearAlgebra.Vectors;

namespace ISAAR.MSolve.Geometry.Shapes
{
    public enum CirclePointPosition
    {
        Inside, On, Outside
    }

    public class Circle2D : ICurve2D
    {
        public CartesianPoint Center { get; }
        public double Radius { get; }

        public Circle2D(CartesianPoint center, double radius)
        {
            this.Center = new CartesianPoint(center.X, center.Y); // Copy it for extra safety
            this.Radius = radius;
        }

        public CirclePointPosition FindRelativePositionOfPoint(CartesianPoint point)
        {
            double distanceX = point.X - Center.X;
            double distanceY = point.Y - Center.Y;
            double distance = Math.Sqrt(distanceX * distanceX + distanceY * distanceY);

            if (distance > Radius) return CirclePointPosition.Outside;
            else if (distance < Radius) return CirclePointPosition.Inside;
            else return CirclePointPosition.On;
        }

        public double SignedDistanceOf(CartesianPoint point) => point.CalculateDistanceFrom(Center) - Radius;

        public Vector2 NormalVectorThrough(CartesianPoint point)
        {
            throw new NotImplementedException();
        }
    }
}
