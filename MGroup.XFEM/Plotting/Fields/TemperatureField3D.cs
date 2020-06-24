using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Logging.VTK;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Enrichment;
using MGroup.XFEM.Entities;
using MGroup.XFEM.Geometry.ConformingMesh;
using MGroup.XFEM.Geometry.Primitives;
using MGroup.XFEM.Plotting.Mesh;

namespace MGroup.XFEM.Plotting.Fields
{
    public class TemperatureField3D
    {
        private readonly XModel model;
        private readonly ConformingOutputMesh3D outMesh;

        public TemperatureField3D(XModel model, ConformingOutputMesh3D outMesh)
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
                var element = (IXFiniteElement3D)e;

                IEnumerable<ConformingOutputMesh3D.Subtetrahedron> subtetrahedra = outMesh.GetSubtetrahedraForOriginal(element);
                if (subtetrahedra.Count() == 0)
                {
                    double[] nodalTemperatures = Utilities.ExtractNodalTemperaturesStandard(element, subdomain, systemSolution);
                    Debug.Assert(outMesh.GetOutCellsForOriginal(element).Count() == 1);
                    VtkCell outCell = outMesh.GetOutCellsForOriginal(element).First();
                    for (int n = 0; n < element.Nodes.Count; ++n) outTemperatures[outCell.Vertices[n]] = nodalTemperatures[n];
                }
                else
                {
                    double[] nodalTemperatures = Utilities.ExtractNodalTemperatures(element, subdomain, systemSolution);
                    foreach (ConformingOutputMesh3D.Subtetrahedron subtetra in subtetrahedra)
                    {
                        Debug.Assert(subtetra.OutVertices.Count == 4); //TODO: Not sure what happens for 2nd order elements

                        // We must interpolate the nodal values, taking into account the enrichements.
                        double[] temperatureAtVertices = CalcTemperatureFieldInSubtriangle(element,
                            subtetra.OriginalTetra, nodalTemperatures);

                        for (int v = 0; v < 4; ++v)
                        {
                            VtkPoint vertexOut = subtetra.OutVertices[v];
                            outTemperatures[vertexOut] = temperatureAtVertices[v];
                        }
                    }
                }
            }
            return outMesh.OutVertices.Select(v => outTemperatures[v]);
        }

        private double[] CalcTemperatureFieldInSubtriangle(IXFiniteElement3D element, ElementSubtetrahedron3D subtetra,
            double[] nodalTemperatures)
        {
            // Evaluate shape functions
            var shapeFunctionsAtVertices = new List<double[]>(subtetra.VerticesNatural.Length);
            for (int v = 0; v < subtetra.VerticesNatural.Length; ++v)
            {
                NaturalPoint vertex = subtetra.VerticesNatural[v];
                shapeFunctionsAtVertices.Add(element.Interpolation.EvaluateFunctionsAt(vertex.Coordinates));
            }

            // Locate centroid
            NaturalPoint centroidNatural = subtetra.FindCentroidNatural();
            var centroid = new XPoint();
            centroid.Element = element;
            centroid.ShapeFunctions = element.Interpolation.EvaluateFunctionsAt(centroidNatural.Coordinates);
            //CartesianPoint centroid = element.Interpolation.TransformNaturalToCartesian(element.Nodes, centroidNatural);
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
            var temperaturesAtVertices = new double[subtetra.VerticesNatural.Length];
            for (int v = 0; v < subtetra.VerticesNatural.Length; ++v)
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
