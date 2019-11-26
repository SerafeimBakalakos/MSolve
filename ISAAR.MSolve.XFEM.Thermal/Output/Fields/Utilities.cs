using System.Collections.Generic;
using System.Diagnostics;
using ISAAR.MSolve.FEM.Interpolation;
using ISAAR.MSolve.Geometry.Coordinates;

//TODO: Perhaps the interpolation extensions should be included in the original classes, e.g. in abstract classes or as default 
//      interface implementations.
namespace ISAAR.MSolve.XFEM.Thermal.Output.Fields
{
    public static class Utilities
    {
        public static double InterpolateNodalScalars(this IIsoparametricInterpolation2D interpolation, NaturalPoint point, 
            IReadOnlyList<double> nodalScalars)
        {
            double[] shapeFunctions = interpolation.EvaluateFunctionsAt(point);
            Debug.Assert(shapeFunctions.Length == nodalScalars.Count);
            double result = 0.0;
            for (int n = 0; n < shapeFunctions.Length; ++n) result += shapeFunctions[n] * nodalScalars[n];
            return result;
        }

        public static double[] InterpolateNodalVectors(this IIsoparametricInterpolation2D interpolation, NaturalPoint point,
            IReadOnlyList<double[]> nodalVectors)
        {
            double[] shapeFunctions = interpolation.EvaluateFunctionsAt(point);
            Debug.Assert(shapeFunctions.Length == nodalVectors.Count);
            int numComponents = nodalVectors[0].Length;
            var result = new double[numComponents];
            for (int n = 0; n < shapeFunctions.Length; ++n)
            {
                for (int i = 0; i < numComponents; ++i) result[i] += shapeFunctions[n] * nodalVectors[n][i];
            }
            return result;
        }
    }
}
