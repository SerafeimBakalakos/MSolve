﻿namespace ISAAR.MSolve.XFEM_OLD.Thermal.Enrichments.Functions
{
    public interface IStepFunctionFunction : IEnrichmentFunction
    {
        double EvaluateAt(double signedDistance);

        double[] EvaluateAtSubtriangleVertices(double[] signedDistancesAtVertices, double signedDistanceAtCentroid);

        EvaluatedFunction EvaluateAllAt(double signedDistance);

        EvaluatedFunction[] EvaluateAllAtSubtriangleVertices(double[] signedDistancesAtVertices, double signedDistanceAtCentroid);
    }
}
