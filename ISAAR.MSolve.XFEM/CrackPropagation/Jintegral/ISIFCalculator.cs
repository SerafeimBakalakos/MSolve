using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ISAAR.MSolve.XFEM_OLD.CrackPropagation.Jintegral
{
    public interface ISIFCalculator
    {
        double CalculateSIF(double interactionIntegral);
    }
}
