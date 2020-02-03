using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Integration;
using ISAAR.MSolve.XFEM.Multiphase.Elements;

namespace ISAAR.MSolve.XFEM.Tests.Multiphase.Integration
{
    public interface IBenchmarkVolumeFunction
    {
        double Evaluate(GaussPoint point, IXFiniteElement element);
        double GetExpectedIntegral(BenchmarkDomain.GeometryType geometryType);

        bool IsInValidRegion(GaussPoint point);

    }
}
