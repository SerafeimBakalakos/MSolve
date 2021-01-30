﻿using System;
using System.Collections.Generic;
using System.Linq;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Enrichment;
using MGroup.XFEM.Entities;
using MGroup.XFEM.Geometry.Primitives;
using MGroup.XFEM.Integration;
using MGroup.XFEM.Interpolation;
using MGroup.XFEM.Materials;

//TODO: Also calculate heat flux at nodes. It needs averaging over the elements. It also needs to specify the material since it 
//      is not explicitly stored as in elements.
namespace MGroup.XFEM.Output.Fields
{
    public class HeatFluxAtGaussPointsField
    {
        private readonly XModel<IXMultiphaseElement> model;

        public HeatFluxAtGaussPointsField(XModel<IXMultiphaseElement> model)
        {
            this.model = model;
        }

        public Dictionary<double[], double[]> CalcValuesAtVertices(IVectorView solution)
        {
            if (model.Subdomains.Count != 1) throw new NotImplementedException();
            XSubdomain subdomain = model.Subdomains.First().Value;
            DofTable dofTable = subdomain.FreeDofOrdering.FreeDofs;

            var result = new Dictionary<double[], double[]>();
            foreach (IXThermalElement element in model.Elements)
            {
                (IReadOnlyList<GaussPoint> gaussPoints, IReadOnlyList<ThermalMaterial> materials)
                    = element.GetMaterialsForBulkIntegration();
                double[] nodalTemperatures = Utilities.ExtractNodalTemperatures(element, subdomain, solution);
                for (int i = 0; i < gaussPoints.Count; ++i)
                {
                    GaussPoint pointNatural = gaussPoints[i];
                    EvalInterpolation evalInterpolation =
                        element.Interpolation.EvaluateAllAt(element.Nodes, pointNatural.Coordinates);
                    double[] coordsCartesian = 
                        Utilities.TransformNaturalToCartesian(evalInterpolation.ShapeFunctions, element.Nodes);
                    var point = new XPoint(coordsCartesian.Length);
                    point.Coordinates[CoordinateSystem.ElementNatural] = pointNatural.Coordinates;
                    point.Element = element;
                    point.ShapeFunctions = evalInterpolation.ShapeFunctions;
                    double[] gradientTemperature =
                        CalcTemperatureGradientAt(point, evalInterpolation, element, nodalTemperatures);

                    double conductivity = materials[i].ThermalConductivity;
                    for (int d = 0; d < gradientTemperature.Length; d++)
                    {
                        gradientTemperature[d] *= -conductivity;
                    }
                    result[coordsCartesian] = gradientTemperature;
                }
            }
            return result;
        }

        public static double[] CalcTemperatureGradientAt(XPoint point, EvalInterpolation evalInterpolation,
            IXFiniteElement element, double[] nodalTemperatures)
        {
            int dimension = evalInterpolation.ShapeGradientsCartesian.NumColumns;
            var gradient = new double[dimension];
            int idx = 0;
            for (int n = 0; n < element.Nodes.Count; ++n)
            {
                // Standard temperatures
                double stdTi = nodalTemperatures[idx++];
                for (int i = 0; i < dimension; ++i)
                {
                    gradient[i] += evalInterpolation.ShapeGradientsCartesian[n, i] * stdTi;
                }

                // Eniched temperatures
                foreach (IEnrichmentFunction enrichment in element.Nodes[n].EnrichmentFuncs.Keys)
                {
                    double psiVertex = enrichment.EvaluateAt(point);
                    double psiNode = element.Nodes[n].EnrichmentFuncs[enrichment];
                    double enrTij = nodalTemperatures[idx++];

                    for (int i = 0; i < dimension; ++i)
                    {
                        gradient[i] += evalInterpolation.ShapeGradientsCartesian[n, i] * (psiVertex - psiNode) * enrTij;
                    }
                }
            }
            return gradient;
        }
    }
}
