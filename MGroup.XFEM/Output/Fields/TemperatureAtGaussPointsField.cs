﻿using System;
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
using MGroup.XFEM.Enrichment;

namespace MGroup.XFEM.Output.Fields
{
    public class TemperatureAtGaussPointsField
    {
        private readonly XModel<IXMultiphaseElement> model;

        public TemperatureAtGaussPointsField(XModel<IXMultiphaseElement> model)
        {
            this.model = model;
        }

        public Dictionary<double[], double> CalcValuesAtVertices(IVectorView solution)
        {
            if (model.Subdomains.Count != 1) throw new NotImplementedException();
            XSubdomain subdomain = model.Subdomains.First().Value;

            var result = new Dictionary<double[], double>();
            foreach (IXThermalElement element in model.Elements)
            {
                (IReadOnlyList<GaussPoint> gaussPoints, _) = element.GetMaterialsForBulkIntegration();
                double[] nodalTemperatures = Utilities.ExtractNodalTemperatures(element, subdomain, solution);
                foreach (GaussPoint pointNatural in gaussPoints)
                {
                    var point = new XPoint(pointNatural.Coordinates.Length);
                    point.Element = element;
                    point.Coordinates[CoordinateSystem.ElementNatural] = pointNatural.Coordinates;
                    point.ShapeFunctions = element.Interpolation.EvaluateFunctionsAt(pointNatural.Coordinates);
                    double[] coordsCartesian = Utilities.TransformNaturalToCartesian(point.ShapeFunctions, element.Nodes);
                    double temperature = CalcTemperatureAt(point, element, nodalTemperatures);
                    result[coordsCartesian] = temperature;
                }
            }
            return result;
        }

        //TODO: Perhaps this should be implemented by the element itself, where a lot of optimizations can be employed.
        public static double CalcTemperatureAt(XPoint point, IXFiniteElement element, double[] nodalTemperatures)
        {
            double sum = 0.0;
            int idx = 0;
            for (int n = 0; n < element.Nodes.Count; ++n)
            {
                // Standard temperatures
                sum += point.ShapeFunctions[n] * nodalTemperatures[idx++];

                // Eniched temperatures
                foreach (IEnrichmentFunction enrichment in element.Nodes[n].EnrichmentFuncs.Keys)
                {
                    double psiVertex = enrichment.EvaluateAt(point);
                    double psiNode = element.Nodes[n].EnrichmentFuncs[enrichment];
                    sum += point.ShapeFunctions[n] * (psiVertex - psiNode) * nodalTemperatures[idx++];
                }
            }
            return sum;
        }
    }
}
