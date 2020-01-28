using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.XFEM.Multiphase.Elements;

namespace ISAAR.MSolve.XFEM.Multiphase.Geometry
{
    public interface IMeshTolerance
    {
        double CalcTolerance(IXFiniteElement element);
    }
}
