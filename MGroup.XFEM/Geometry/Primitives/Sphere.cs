using System;
using System.Collections.Generic;
using System.Text;

namespace MGroup.XFEM.Geometry.Primitives
{
    public class Sphere : ISurface3D
    {
        public Sphere(double centerX, double centerY, double centerZ, double radius)
        {
            this.Center = new double[] { centerX, centerY, centerZ };
            this.Radius = radius;
        }

        public double[] Center { get; }
        public double Radius { get; }

        public IElementDiscontinuityInteraction IntersectPolygon(IList<double[]> nodes)
        {
            throw new NotImplementedException();
        }

        public double SignedDistanceOf(double[] point) => Utilities.Distance3D(Center, point) - Radius;
    }
}
