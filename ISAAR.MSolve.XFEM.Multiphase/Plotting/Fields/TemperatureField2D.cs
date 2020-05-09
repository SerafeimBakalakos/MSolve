using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Logging.VTK;
using ISAAR.MSolve.XFEM_OLD.Multiphase.Elements;
using ISAAR.MSolve.XFEM_OLD.Multiphase.Enrichment;
using ISAAR.MSolve.XFEM_OLD.Multiphase.Entities;
using ISAAR.MSolve.XFEM_OLD.Multiphase.Plotting.Mesh;

namespace ISAAR.MSolve.XFEM_OLD.Multiphase.Plotting.Fields
{
    public class TemperatureField2D
    {
        private readonly XModel model;
        private readonly ConformingOutputMesh2D outMesh;

        public TemperatureField2D(XModel model, ConformingOutputMesh2D outMesh)
        {
            this.model = model;
            this.outMesh = outMesh;
        }

        public IEnumerable<double> CalcValuesAtVertices(IVectorView systemSolution)
        {
            if (model.Subdomains.Count != 1) throw new NotImplementedException();
            XSubdomain subdomain = model.Subdomains.First().Value;

            var outTemperatures = new Dictionary<VtkPoint, double>();
            foreach (IXFiniteElement e in subdomain.Elements)
            {
                var element = (XThermalElement2D)e;

                IEnumerable<ConformingOutputMesh2D.Subtriangle> subtriangles = outMesh.GetSubtrianglesForOriginal(element);
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
                    foreach (ConformingOutputMesh2D.Subtriangle subtriangle in subtriangles)
                    {
                        Debug.Assert(subtriangle.OutVertices.Count == 3); //TODO: Not sure what happens for 2nd order elements

                        // We must interpolate the nodal values, taking into account the enrichements.
                        double[] temperatureAtVertices = CalcTemperatureFieldInSubtriangle(element,
                            subtriangle.OriginalTriangle.VerticesNatural, nodalTemperatures);

                        for (int v = 0; v < 3; ++v)
                        {
                            VtkPoint vertexOut = subtriangle.OutVertices[v];
                            outTemperatures[vertexOut] = temperatureAtVertices[v];
                        }
                    }
                }
            }
            return outMesh.OutVertices.Select(v => outTemperatures[v]);
        }

        private double[] CalcTemperatureFieldInSubtriangle(XThermalElement2D element, IList<NaturalPoint> subtriangleVertices, 
            double[] nodalTemperatures)
        {
            // Evaluate shape functions
            var shapeFunctionsAtVertices = new List<double[]>(subtriangleVertices.Count);
            for (int v = 0; v < subtriangleVertices.Count; ++v)
            {
                NaturalPoint vertex = subtriangleVertices[v];
                shapeFunctionsAtVertices.Add(element.InterpolationStandard.EvaluateFunctionsAt(vertex));
            }

            // Locate centroid
            double centroidXi = 0.0, centroidEta = 0.0, centroidZeta = 0.0;
            foreach (NaturalPoint vertex in subtriangleVertices)
            {
                centroidXi += vertex.Xi;
                centroidEta += vertex.Eta;
                centroidZeta += vertex.Zeta;
            }
            NaturalPoint centroidNatural = new NaturalPoint(centroidXi / 3.0, centroidEta / 3.0, centroidZeta / 3.0);
            CartesianPoint centroid = element.InterpolationStandard.TransformNaturalToCartesian(element.Nodes, centroidNatural);
            //double[] shapeFunctionsAtCentroid = element.InterpolationStandard.EvaluateFunctionsAt(centroid);

            // Evaluate enrichment functions at triangle centroid and assume it also holds for its vertices
            var enrichments = new HashSet<IEnrichment>();
            foreach (XNode node in element.Nodes) enrichments.UnionWith(node.Enrichments.Keys);
            var enrichmentValues = new Dictionary<IEnrichment, double>();
            foreach (IEnrichment enrichment in enrichments)
            {
                enrichmentValues[enrichment] = enrichment.EvaluateAt(centroid);
                //enrichmentValues[enrichment] = EvaluateFunctionsAtSubtriangleVertices(
                //    element, shapeFunctionsAtVertices, shapeFunctionsAtCentroid);
            }

            // t(x) = sum_over_nodes(Ni(x) * t_i) + sum_over_enriched_nodes( N_j(x) * (psi(x) - psi_j)*a_j )
            var temperaturesAtVertices = new double[subtriangleVertices.Count];
            for (int v = 0; v < subtriangleVertices.Count; ++v)
            {
                double[] N = shapeFunctionsAtVertices[v];
                double sum = 0.0;
                int idx = 0;
                for (int n = 0; n < element.Nodes.Count; ++n)
                {
                    // Standard temperatures
                    sum += N[n] * nodalTemperatures[idx++];

                    // Eniched temperatures
                    foreach (IEnrichment enrichment in element.Nodes[n].Enrichments.Keys)
                    {
                        double psiVertex = enrichmentValues[enrichment];
                        double psiNode = element.Nodes[n].Enrichments[enrichment];
                        sum += N[n] * (psiVertex - psiNode) * nodalTemperatures[idx++];
                    }
                }
                temperaturesAtVertices[v] = sum;
            }
            return temperaturesAtVertices;
        }
    }
}
