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
using MGroup.XFEM.Integration;
using MGroup.XFEM.Enrichment;

namespace MGroup.XFEM.Output.Fields
{
    public class DisplacementsAtGaussPointsField
    {
        private readonly XModel<IXMultiphaseElement> model;

        public DisplacementsAtGaussPointsField(XModel<IXMultiphaseElement> model)
        {
            this.model = model;
        }

        public Dictionary<double[], double[]> CalcValuesAtVertices(IVectorView solution)
        {
            if (model.Subdomains.Count != 1) throw new NotImplementedException();
            XSubdomain subdomain = model.Subdomains.First().Value;

            var result = new Dictionary<double[], double[]>();
            foreach (IXMultiphaseElement element in model.Elements)
            {
                IEnumerable<GaussPoint> gaussPoints = element.BulkIntegrationPoints;
                IList<double[]> elementDisplacements = Utilities.ExtractElementDisplacements(element, subdomain, solution);
                foreach (GaussPoint pointNatural in gaussPoints)
                {
                    var point = new XPoint(pointNatural.Coordinates.Length);
                    point.Element = element;
                    point.Coordinates[CoordinateSystem.ElementNatural] = pointNatural.Coordinates;
                    point.ShapeFunctions = element.Interpolation.EvaluateFunctionsAt(pointNatural.Coordinates);
                    double[] coordsCartesian = Utilities.TransformNaturalToCartesian(point.ShapeFunctions, element.Nodes);
                    double[] displacements = CalcDisplacementsAt(point, element, elementDisplacements);
                    result[coordsCartesian] = displacements;
                }
            }
            return result;
        }

        //TODO: Perhaps this should be implemented by the element itself, where a lot of optimizations can be employed.
        /// <summary>
        /// 
        /// </summary>
        /// <param name="point"></param>
        /// <param name="element"></param>
        /// <param name="elementDisplacements">
        /// The order of dofs per node is enrichment major - axis minor.</param>
        /// <returns></returns>
        public static double[] CalcDisplacementsAt(XPoint point, IXFiniteElement element, IList<double[]> elementDisplacements)
        {
            int dim = point.Dimension;
            var displacements = new double[dim];
            for (int n = 0; n < element.Nodes.Count; ++n)
            {
                double[] u = elementDisplacements[n];
                double N = point.ShapeFunctions[n];

                // Standard displacements
                int currentDof = 0;
                for (int d = 0; d < dim; ++d)
                {
                    displacements[d] += N * u[currentDof++];
                }

                // Eniched displacements
                foreach (IEnrichmentFunction enrichment in element.Nodes[n].EnrichmentFuncs.Keys)
                {
                    double psiVertex = enrichment.EvaluateAt(point);
                    double psiNode = element.Nodes[n].EnrichmentFuncs[enrichment];
                    for (int d = 0; d < dim; ++d)
                    {
                        displacements[d] += N * (psiVertex - psiNode) * u[currentDof++];
                    }
                }
            }
            return displacements;
        }
    }
}
