using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Integration;
using ISAAR.MSolve.XFEM_OLD.Multiphase.Elements;
using ISAAR.MSolve.XFEM_OLD.Multiphase.Geometry;

namespace ISAAR.MSolve.XFEM_OLD.Multiphase.Integration
{
    public interface IBoundaryIntegration
    {
        IReadOnlyList<GaussPoint> GenerateIntegrationPoints(IXFiniteElement element, CurveElementIntersection intersection);
    }
}
