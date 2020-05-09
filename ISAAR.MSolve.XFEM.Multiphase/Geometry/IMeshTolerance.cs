using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.XFEM_OLD.Multiphase.Elements;

namespace ISAAR.MSolve.XFEM_OLD.Multiphase.Geometry
{
    public interface IMeshTolerance
    {
        double CalcTolerance(IXFiniteElement element);
    }
}
