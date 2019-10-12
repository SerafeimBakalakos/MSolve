using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ISAAR.MSolve.XFEM.Thermal.Enrichments.Functions
{
    public interface IEnrichmentFunction
    {
        double EvaluateAt(double signedDistance);
        EvaluatedFunction EvaluateAllAt(double signedDistance);
    }
}
