﻿using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Integration;
using ISAAR.MSolve.Geometry.Coordinates;

namespace MGroup.XFEM.Geometry
{
    public class NullElementIntersection2D : IElementCurveIntersection2D
    {
        public RelativePositionCurveElement RelativePosition => RelativePositionCurveElement.Disjoint;

        public List<double[]> ApproximateGlobalCartesian() => new List<double[]>(0);

        public IReadOnlyList<GaussPoint> GetIntegrationPoints(int numPoints)
        {
            return new GaussPoint[0];
        }

        public NaturalPoint[] GetPointsForTriangulation()
        {
            return new NaturalPoint[0];
        }
    }
}
