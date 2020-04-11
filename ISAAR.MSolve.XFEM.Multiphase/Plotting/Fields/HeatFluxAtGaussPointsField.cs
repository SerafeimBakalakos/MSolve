using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Discretization.Integration;
using ISAAR.MSolve.FEM.Interpolation;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.XFEM.Multiphase.Elements;
using ISAAR.MSolve.XFEM.Multiphase.Entities;
using ISAAR.MSolve.XFEM.Multiphase.Materials;

//TODO: Also calculate heat flux at nodes. It needs averaging over the elements. It also needs to specify the material since it 
//      is not explicitly stored as in elements.
namespace ISAAR.MSolve.XFEM.Multiphase.Plotting.Fields
{
    public class HeatFluxAtGaussPointsField
    {
        private readonly XModel model;

        public HeatFluxAtGaussPointsField(XModel model)
        {
            this.model = model;
        }

        public Dictionary<CartesianPoint, double[]> CalcValuesAtVertices(IVectorView solution)
        {
            if (model.Subdomains.Count != 1) throw new NotImplementedException();
            XSubdomain subdomain = model.Subdomains.First().Value;
            DofTable dofTable = subdomain.FreeDofOrdering.FreeDofs;

            var result = new Dictionary<CartesianPoint, double[]>();
            foreach (IXFiniteElement element in model.Elements)
            {
                //#region debug
                //if (element.PhaseIntersections.Count >= 1) continue;
                //#endregion
                (IReadOnlyList<GaussPoint> gaussPoints, IReadOnlyList<ThermalMaterial> materials) 
                    = element.GetMaterialsForVolumeIntegration();
                double[] nodalTemperatures = Utilities.ExtractNodalTemperatures(element, subdomain, solution);
                for (int i = 0; i < gaussPoints.Count; ++i)
                {
                    GaussPoint pointNatural = gaussPoints[i];
                    EvalInterpolation2D evalInterpolation = 
                        element.InterpolationStandard.EvaluateAllAt(element.Nodes, pointNatural);
                    CartesianPoint pointCartesian = evalInterpolation.TransformPointNaturalToGlobalCartesian();
                    double[] gradientTemperature = 
                        Utilities.CalcTemperatureGradientAt(pointCartesian, evalInterpolation, element, nodalTemperatures);

                    double conductivity = materials[i].ThermalConductivity;
                    gradientTemperature[0] *= -conductivity;
                    gradientTemperature[1] *= -conductivity;
                    result[pointCartesian] = gradientTemperature;
                }
            }
            return result;
        }
    }
}
