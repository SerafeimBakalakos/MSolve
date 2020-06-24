using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using MGroup.XFEM.Integration;

using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Entities;
using MGroup.XFEM.Geometry.Primitives;
using MGroup.XFEM.Materials;
using ISAAR.MSolve.Discretization.Integration;
using ISAAR.MSolve.FEM.Interpolation;

//TODO: Also calculate heat flux at nodes. It needs averaging over the elements. It also needs to specify the material since it 
//      is not explicitly stored as in elements.
namespace MGroup.XFEM.Plotting.Fields
{
    public class HeatFluxAtGaussPointsField3D
    {
        private readonly XModel model;

        public HeatFluxAtGaussPointsField3D(XModel model)
        {
            this.model = model;
        }

        public Dictionary<double[], double[]> CalcValuesAtVertices(IVectorView solution)
        {
            if (model.Subdomains.Count != 1) throw new NotImplementedException();
            XSubdomain subdomain = model.Subdomains.First().Value;
            DofTable dofTable = subdomain.FreeDofOrdering.FreeDofs;

            var result = new Dictionary<double[], double[]>();
            foreach (IXFiniteElement element in model.Elements)
            {
                var element3D = (IXFiniteElement3D)element;
                //#region debug
                //if (element.PhaseIntersections.Count >= 1) continue;
                //#endregion
                (IReadOnlyList<GaussPoint> gaussPoints, IReadOnlyList<ThermalMaterial> materials)
                    = element.GetMaterialsForBulkIntegration();
                double[] nodalTemperatures = Utilities.ExtractNodalTemperatures(element, subdomain, solution);
                for (int i = 0; i < gaussPoints.Count; ++i)
                {
                    GaussPoint pointNatural = gaussPoints[i];
                    EvalInterpolation3D evalInterpolation =
                        element3D.Interpolation.EvaluateAllAt(element.Nodes, pointNatural);
                    double[] coordsCartesian = 
                        Utilities.TransformNaturalToCartesian(evalInterpolation.ShapeFunctions, element.Nodes);
                    var point = new XPoint();
                    point.Coordinates[CoordinateSystem.ElementNatural] = pointNatural.Coordinates;
                    point.Element = element;
                    point.ShapeFunctions = evalInterpolation.ShapeFunctions;
                    double[] gradientTemperature =
                        Utilities.CalcTemperatureGradientAt(point, evalInterpolation, element, nodalTemperatures);

                    double conductivity = materials[i].ThermalConductivity;
                    gradientTemperature[0] *= -conductivity;
                    gradientTemperature[1] *= -conductivity;
                    gradientTemperature[2] *= -conductivity;
                    result[coordsCartesian] = gradientTemperature;
                }
            }
            return result;
        }
    }
}
