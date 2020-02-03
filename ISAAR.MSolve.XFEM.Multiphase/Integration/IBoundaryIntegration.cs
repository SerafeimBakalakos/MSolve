using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Integration;
using ISAAR.MSolve.XFEM.Multiphase.Elements;
using ISAAR.MSolve.XFEM.Multiphase.Geometry;

namespace ISAAR.MSolve.XFEM.Multiphase.Integration
{
    public interface IBoundaryIntegration
    {
        IReadOnlyList<GaussPoint> GenerateIntegrationPoints(IXFiniteElement element, CurveElementIntersection intersection);
    }
}
