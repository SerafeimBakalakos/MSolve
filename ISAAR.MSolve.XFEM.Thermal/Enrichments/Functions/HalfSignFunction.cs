using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ISAAR.MSolve.LinearAlgebra.Vectors;

namespace ISAAR.MSolve.XFEM.Thermal.Enrichments.Functions
{
    public class HalfSignFunction : IEnrichmentFunction
    {
        public HalfSignFunction()
        {
        }

        public double EvaluateAt(double signedDistance)
        {
            if (signedDistance > 0.0) return 0.5;
            else if (signedDistance < 0.0) return -0.5;
            else return 0.0;
        }

        public EvaluatedFunction EvaluateAllAt(double signedDistance)
        {
            double[] derivatives = { 0.0, 0.0 };
            if (signedDistance > 0) return new EvaluatedFunction(0.5, derivatives);
            else if (signedDistance < 0) return new EvaluatedFunction(-0.5, derivatives);
            else return new EvaluatedFunction(0.0, derivatives);
        }

        public override string ToString()
        {
            return "0.5*Heaviside";
        }
    }
}
