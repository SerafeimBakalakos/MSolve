using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Integration;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.XFEM.Multiphase.Elements;

namespace ISAAR.MSolve.XFEM.Tests.Multiphase.Integration
{
    public interface IBenchmarkBoundaryFunction
    {
        double Evaluate(GaussPoint point, IXFiniteElement element);
        double GetExpectedIntegral(CartesianPoint start, CartesianPoint end);
    }
}
