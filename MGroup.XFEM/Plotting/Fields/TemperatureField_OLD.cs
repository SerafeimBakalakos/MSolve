using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Enrichment;
using MGroup.XFEM.Entities;
using MGroup.XFEM.Geometry.ConformingMesh;
using MGroup.XFEM.Geometry.Primitives;
using MGroup.XFEM.Plotting.Mesh;

namespace MGroup.XFEM.Plotting.Fields
{
    public class TemperatureField_OLD
    {
        private readonly XModel<IXMultiphaseElement> model;
        private readonly ConformingOutputMesh_OLD outMesh;

        public TemperatureField_OLD(XModel<IXMultiphaseElement> model, ConformingOutputMesh_OLD outMesh)
        {
            this.model = model;
            this.outMesh = outMesh;
        }

        public IEnumerable<double> CalcValuesAtVertices(IVectorView systemSolution)
        {
            if (model.Subdomains.Count != 1) throw new NotImplementedException();
            XSubdomain subdomain = model.Subdomains.First().Value;

            var outTemperatures = new Dictionary<VtkPoint, double>();
            foreach (IXFiniteElement element in subdomain.Elements)
            {

                IEnumerable<ConformingOutputMesh_OLD.Subcell> subtriangles = outMesh.GetSubcellsForOriginal(element);
                if (subtriangles.Count() == 0)
                {
                    double[] nodalTemperatures = Utilities.ExtractNodalTemperaturesStandard(element, subdomain, systemSolution);
                    Debug.Assert(outMesh.GetOutCellsForOriginal(element).Count() == 1);
                    VtkCell outCell = outMesh.GetOutCellsForOriginal(element).First();
                    for (int n = 0; n < element.Nodes.Count; ++n) outTemperatures[outCell.Vertices[n]] = nodalTemperatures[n];
                }
                else
                {
                    double[] nodalTemperatures = Utilities.ExtractNodalTemperatures(element, subdomain, systemSolution);
                    foreach (ConformingOutputMesh_OLD.Subcell subcell in subtriangles)
                    {
                        Debug.Assert(subcell.OutVertices.Count == 3 || subcell.OutVertices.Count == 4); //TODO: Not sure what happens for 2nd order elements

                        // We must interpolate the nodal values, taking into account the enrichements.
                        double[] temperatureAtVertices = CalcTemperatureFieldInSubtriangle(element,
                            subcell.OriginalSubcell, nodalTemperatures);

                        for (int v = 0; v < subcell.OutVertices.Count; ++v)
                        {
                            VtkPoint vertexOut = subcell.OutVertices[v];
                            outTemperatures[vertexOut] = temperatureAtVertices[v];
                        }
                    }
                }
            }
            return outMesh.OutVertices.Select(v => outTemperatures[v]);
        }

        private double[] CalcTemperatureFieldInSubtriangle(IXFiniteElement element, IElementSubcell subcell,
            double[] nodalTemperatures)
        {
            // Evaluate shape functions
            var shapeFunctionsAtVertices = new List<double[]>(subcell.VerticesNatural.Count);
            for (int v = 0; v < subcell.VerticesNatural.Count; ++v)
            {
                double[] vertex = subcell.VerticesNatural[v];
                shapeFunctionsAtVertices.Add(element.Interpolation.EvaluateFunctionsAt(vertex));
            }

            // Locate centroid
            double[] centroidNatural = subcell.FindCentroidNatural();
            var centroid = new XPoint(centroidNatural.Length);
            centroid.Element = element;
            centroid.ShapeFunctions = element.Interpolation.EvaluateFunctionsAt(centroidNatural);

            // Evaluate enrichment functions at triangle centroid and assume it also holds for its vertices
            var enrichments = new HashSet<IEnrichmentFunction>();
            foreach (XNode node in element.Nodes) enrichments.UnionWith(node.EnrichmentFuncs.Keys);
            var enrichmentValues = new Dictionary<IEnrichmentFunction, double>();
            foreach (IEnrichmentFunction enrichment in enrichments)
            {
                enrichmentValues[enrichment] = enrichment.EvaluateAt(centroid);
                //enrichmentValues[enrichment] = EvaluateFunctionsAtSubtriangleVertices(
                //    element, shapeFunctionsAtVertices, shapeFunctionsAtCentroid);
            }

            // t(x) = sum_over_nodes(Ni(x) * t_i) + sum_over_enriched_nodes( N_j(x) * (psi(x) - psi_j)*a_j )
            var temperaturesAtVertices = new double[subcell.VerticesNatural.Count];
            for (int v = 0; v < subcell.VerticesNatural.Count; ++v)
            {
                double[] N = shapeFunctionsAtVertices[v];
                double sum = 0.0;
                int idx = 0;
                for (int n = 0; n < element.Nodes.Count; ++n)
                {
                    // Standard temperatures
                    sum += N[n] * nodalTemperatures[idx++];

                    // Eniched temperatures
                    foreach (IEnrichmentFunction enrichment in element.Nodes[n].EnrichmentFuncs.Keys)
                    {
                        double psiVertex = enrichmentValues[enrichment];
                        double psiNode = element.Nodes[n].EnrichmentFuncs[enrichment];
                        sum += N[n] * (psiVertex - psiNode) * nodalTemperatures[idx++];
                    }
                }
                temperaturesAtVertices[v] = sum;
            }
            return temperaturesAtVertices;
        }
    }
}
