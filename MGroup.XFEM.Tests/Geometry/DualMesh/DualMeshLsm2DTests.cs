using System;
using System.Collections.Generic;
using System.Text;
using MGroup.XFEM.Geometry.Primitives;

namespace MGroup.XFEM.Tests.Geometry.DualMesh
{
    public static class DualMeshLsm2DTests
    {
        public static ICurve2D CreateGeometry(double[] minCoords, double[] maxCoords)
        {
            double centerX = 0.5 * (minCoords[0] + maxCoords[0]);
            double centerY = 0.5 * (minCoords[1] + maxCoords[1]);
            double radius = Math.Min(0.5 * (maxCoords[0] - centerX), 0.5 * (maxCoords[1] - centerY));
            return new Circle2D(centerX, centerY, radius);
        }
    }
}
