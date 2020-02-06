using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Discretization.Integration;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.XFEM.Multiphase.Elements;
using ISAAR.MSolve.XFEM.Multiphase.Enrichment;
using ISAAR.MSolve.XFEM.Multiphase.Entities;

namespace ISAAR.MSolve.XFEM.Multiphase.Plotting.Fields
{
    public class TemperatureAtGaussPointsField
    {
        private readonly XModel model;

        public TemperatureAtGaussPointsField(XModel model)
        {
            this.model = model;
        }

        public Dictionary<CartesianPoint, double> CalcValuesAtVertices(IVectorView solution)
        {
            if (model.Subdomains.Count != 1) throw new NotImplementedException();
            XSubdomain subdomain = model.Subdomains.First().Value;

            var result = new Dictionary<CartesianPoint, double>();
            foreach (IXFiniteElement element in model.Elements)
            {
                (IReadOnlyList<GaussPoint> gaussPoints, _) = element.GetMaterialsForVolumeIntegration();
                double[] nodalTemperatures = Utilities.ExtractNodalTemperatures(element, subdomain, solution);
                foreach (GaussPoint pointNatural in gaussPoints)
                {
                    double[] shapeFunctions = element.InterpolationStandard.EvaluateFunctionsAt(pointNatural);
                    CartesianPoint pointCartesian = Utilities.TransformNaturalToCartesian(shapeFunctions, element.Nodes);
                    double temperature = Utilities.CalcTemperatureAt(pointCartesian, shapeFunctions, element, nodalTemperatures);
                    result[pointCartesian] = temperature;
                }
            }
            return result;
        }

    }
}
