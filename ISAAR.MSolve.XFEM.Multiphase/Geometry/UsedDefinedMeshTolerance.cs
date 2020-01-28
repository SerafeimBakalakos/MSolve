using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.XFEM.Multiphase.Elements;

namespace ISAAR.MSolve.XFEM.Multiphase.Geometry
{
    public class UsedDefinedMeshTolerance : IMeshTolerance
    {
        private readonly double tolerance;

        public UsedDefinedMeshTolerance(double elementSize, double coeff = 1E-8)
        {
            this.tolerance = elementSize * coeff;
        }

        public double CalcTolerance(IXFiniteElement element) => tolerance;
    }
}
