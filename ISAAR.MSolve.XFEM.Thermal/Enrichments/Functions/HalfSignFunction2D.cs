using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ISAAR.MSolve.LinearAlgebra.Vectors;

namespace ISAAR.MSolve.XFEM.Thermal.Enrichments.Functions
{
    public class HalfSignFunction2D : IEnrichmentFunction
    {
        private const double zeroTolerance = 1E-13; //TODO: this depends on the mesh

        public HalfSignFunction2D()
        {
        }

        public double EvaluateAt(double signedDistance)
        {
            if (signedDistance >= 0.0) return 0.5;
            else /*if (signedDistance < 0.0)*/ return -0.5;
        }

        public double[] EvaluateAtSubtriangleVertices(double[] signedDistancesAtVertices, double signedDistanceAtCentroid)
        {
            // Find on which side of the discontinuity lies the subtriangle
            int sign = Math.Sign(signedDistanceAtCentroid);

            // Force all vertices to use that sign
            var result = new double[signedDistancesAtVertices.Length];
            for (int i = 0; i < result.Length; ++i) result[i] = sign * 0.5;
            return result;
        }

        public EvaluatedFunction EvaluateAllAt(double signedDistance)
        {
            double[] derivatives = { 0.0, 0.0 };
            if (signedDistance >= 0) return new EvaluatedFunction(0.5, derivatives);
            else /*if (signedDistance < 0)*/ return new EvaluatedFunction(-0.5, derivatives);
        }

        public EvaluatedFunction[] EvaluateAllAtSubtriangleVertices(double[] signedDistancesAtVertices, 
            double signedDistanceAtCentroid)
        {
            // Find on which side of the discontinuity lies the subtriangle
            int sign = Math.Sign(signedDistanceAtCentroid);

            // Force all vertices to use that sign
            var result = new EvaluatedFunction[signedDistancesAtVertices.Length];
            for (int i = 0; i < result.Length; ++i) result[i] = new EvaluatedFunction(sign * 0.5, new double[2]); //TODO: This is why it only works for 2D.
            return result;
        }

        public override string ToString()
        {
            return "0.5*Heaviside";
        }
    }
}
