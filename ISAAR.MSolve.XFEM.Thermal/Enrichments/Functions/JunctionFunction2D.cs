using System;

namespace ISAAR.MSolve.XFEM.ThermalOLD.Enrichments.Functions
{
    public class JunctionFunction2D : IEnrichmentFunction
    {
        private readonly int numPhases;

        public JunctionFunction2D(int numPhases)
        {
            this.numPhases = numPhases;
        }

        public EvaluatedFunction EvaluateAllAt(int phase)
        {
            double[] derivatives = { 0.0, 0.0 };
            return new EvaluatedFunction(phase, derivatives);
        }

        public EvaluatedFunction[] EvaluateAllAtSubtriangleVertices(double[] signedDistancesAtVertices, double signedDistanceAtCentroid)
        {
            throw new NotImplementedException();
        }

        public double EvaluateAt(int phase)
        {
            return phase;
        }

        public double[] EvaluateAtSubtriangleVertices(double[] signedDistancesAtVertices, double signedDistanceAtCentroid)
        {
            throw new NotImplementedException();
        }
    }
}
