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

        double[] EvaluateAtSubtriangleVertices(double[] signedDistancesAtVertices, double signedDistanceAtCentroid);

        EvaluatedFunction EvaluateAllAt(double signedDistance);

        EvaluatedFunction[] EvaluateAllAtSubtriangleVertices(double[] signedDistancesAtVertices, double signedDistanceAtCentroid);
    }
}
