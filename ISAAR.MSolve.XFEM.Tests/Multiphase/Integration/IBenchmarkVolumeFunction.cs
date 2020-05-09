using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Integration;
using ISAAR.MSolve.XFEM_OLD.Multiphase.Elements;
using ISAAR.MSolve.XFEM_OLD.Multiphase.Geometry;

namespace ISAAR.MSolve.XFEM_OLD.Tests.Multiphase.Integration
{
    public interface IBenchmarkVolumeFunction
    {
        double Evaluate(GaussPoint point, IXFiniteElement element);
        double GetExpectedIntegral(BenchmarkDomain.GeometryType geometryType);
        CurveElementIntersection[] GetIntersectionSegments();
        bool IsInValidRegion(GaussPoint point);

    }
}
