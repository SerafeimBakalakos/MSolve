using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Integration;
using ISAAR.MSolve.XFEM.Multiphase.Elements;
using ISAAR.MSolve.XFEM.Multiphase.Geometry;

namespace ISAAR.MSolve.XFEM.Tests.Multiphase.Integration
{
    public interface IBenchmarkVolumeFunction
    {
        double Evaluate(GaussPoint point, IXFiniteElement element);
        double GetExpectedIntegral(BenchmarkDomain.GeometryType geometryType);
        CurveElementIntersection[] GetIntersectionSegments();
        bool IsInValidRegion(GaussPoint point);

    }
}
