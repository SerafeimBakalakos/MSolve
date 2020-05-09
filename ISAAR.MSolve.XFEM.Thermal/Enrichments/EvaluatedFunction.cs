﻿namespace ISAAR.MSolve.XFEM_OLD.Thermal.Enrichments
{
    /// <summary>
    /// Data transfer object to store and pass around the value and derivatives of a function, evaluated at some point.
    /// It mainly serves to avoid obscure Tuple<double, Tuple<double, double>> objects.
    /// </summary>
    public class EvaluatedFunction
    {
        public EvaluatedFunction(double value, double[] cartesianDerivatives)
        {
            this.Value = value;
            this.CartesianDerivatives = cartesianDerivatives;
        }

        public double Value { get; }
        public double[] CartesianDerivatives { get; }
    }
}
