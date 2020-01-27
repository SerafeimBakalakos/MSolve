using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Vectors;

namespace ISAAR.MSolve.Geometry.Shapes
{
    public class Rectangle2D : ICurve2D
    {
        private readonly double halfLength0, halfLength1;
        private readonly LocalCoordinateSystem localSystem;

        private readonly Vector[] verticesLocal;

        public Rectangle2D(CartesianPoint centroid, double length0, double length1, double angle0)
        {
            if ((angle0 < 0.0) || (angle0 >= Math.PI)) throw new ArgumentException("Angle of axis 0 must belong to [0, pi)");
            this.Centroid = centroid;
            this.halfLength0 = length0 / 2;
            this.halfLength1 = length1 / 2;
            this.Axis0Angle = angle0;

            this.localSystem = new LocalCoordinateSystem(centroid, angle0);

            this.verticesLocal = new Vector[4];
            verticesLocal[0] = Vector.CreateFromArray(new double[] { -halfLength0, -halfLength1 });
            verticesLocal[1] = Vector.CreateFromArray(new double[] { halfLength0, -halfLength1 });
            verticesLocal[2] = Vector.CreateFromArray(new double[] { halfLength0, halfLength1 });
            verticesLocal[3] = Vector.CreateFromArray(new double[] { -halfLength0, halfLength1 });

            this.Vertices = new CartesianPoint[4];
            for (int v = 0; v < 4; ++v)
            {
                Vector vertex = localSystem.MapLocalToGlobal(verticesLocal[v]);
                Vertices[v] = new CartesianPoint(vertex[0], vertex[1]);
            }
        }

        public double Axis0Angle { get; }

        public CartesianPoint Centroid { get; }

        public double LengthAxis0 => 2 * halfLength0;
        public double LengthAxis1 => 2 * halfLength1;


        public CartesianPoint[] Vertices { get; }

        public bool IsDisjointFrom(Rectangle2D other)
        {
            for (int i = 0; i < 4; ++i)
            {
                var thisEdge = new LineSegment2D(this.Vertices[i], this.Vertices[(i + 1) % 4]);
                for (int j = 0; j < 4; ++j)
                {
                    var otherEdge = new LineSegment2D(other.Vertices[j], other.Vertices[(j + 1) % 4]);

                    LineSegment2D.SegmentSegmentPosition position = 
                        thisEdge.IntersectionWith(otherEdge, out CartesianPoint intersection);
                    if ((position == LineSegment2D.SegmentSegmentPosition.Intersecting)
                        || (position == LineSegment2D.SegmentSegmentPosition.Overlapping)) return false;
                }
            }
            return true;
        }

        public Vector2 NormalVectorThrough(CartesianPoint point)
        {
            throw new NotImplementedException();
        }

        public double SignedDistanceOf(CartesianPoint point)
        {
            Vector pointLocal = localSystem.MapGlobalToLocal(point);
            if (pointLocal[0] > halfLength0)
            {
                if (pointLocal[1] > halfLength1) return (pointLocal - verticesLocal[2]).Norm2();
                if (pointLocal[1] < -halfLength1) return (pointLocal - verticesLocal[1]).Norm2(); ;
                return pointLocal[0] - halfLength0;
            }
            if (pointLocal[0] < -halfLength0)
            {
                if (pointLocal[1] > halfLength1) return (pointLocal - verticesLocal[3]).Norm2();
                if (pointLocal[1] < -halfLength1) return (pointLocal - verticesLocal[0]).Norm2();
                return -pointLocal[0] - halfLength0;
            }
            else
            {
                if (pointLocal[1] > halfLength1) return pointLocal[1] - halfLength1;
                if (pointLocal[1] < -halfLength1) return -pointLocal[1] - halfLength1;
                return -Math.Min(halfLength0 - Math.Abs(pointLocal[0]), halfLength1 - Math.Abs(pointLocal[1]));
            }
        }

        private class LocalCoordinateSystem
        {
            private readonly Vector centroidGlobal;
            private readonly Vector originLocal;
            private readonly Matrix Q;

            public LocalCoordinateSystem(CartesianPoint centroidGlobal, double angle)
            {
                double cosa = Math.Cos(angle);
                double sina = Math.Sin(angle);
                this.Q = Matrix.CreateZero(2, 2);
                this.Q[0, 0] = cosa;  this.Q[0, 1] = sina;
                this.Q[1, 0] = -sina; this.Q[1, 1] = cosa;

                this.centroidGlobal = Vector.CreateFromArray(new double[] { centroidGlobal.X, centroidGlobal.Y });
                this.originLocal = this.Q * this.centroidGlobal.Scale(-1);
            }

            public Vector MapGlobalToLocal(CartesianPoint pointGlobal)
                => MapGlobalToLocal(Vector.CreateFromArray(new double[] { pointGlobal.X, pointGlobal.Y }));

            public Vector MapGlobalToLocal(Vector pointGlobal)
            {
                Vector pointLocal = Q * pointGlobal;
                pointLocal.AddIntoThis(originLocal);
                return pointLocal;
            }

            public Vector MapLocalToGlobal(Vector pointLocal)
            {
                Vector pointGlobal = Q.Multiply(pointLocal, true);
                pointGlobal.AddIntoThis(centroidGlobal);
                return pointGlobal;
            }
        }
    }
}
