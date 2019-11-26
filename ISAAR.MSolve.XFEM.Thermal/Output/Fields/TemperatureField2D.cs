using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Logging.VTK;
using ISAAR.MSolve.XFEM.Thermal.Elements;
using ISAAR.MSolve.XFEM.Thermal.Enrichments.Items;
using ISAAR.MSolve.XFEM.Thermal.Entities;
using ISAAR.MSolve.XFEM.Thermal.Output.Mesh;

namespace ISAAR.MSolve.XFEM.Thermal.Output.Fields
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
                    double[] nodalTemperatures = ExtractNodalTemperaturesStandard(element, subdomain, systemSolution);
                    Debug.Assert(outMesh.GetOutCellsForOriginal(element).Count() == 1);
                    VtkCell outCell = outMesh.GetOutCellsForOriginal(element).First();
                    for (int n = 0; n < element.Nodes.Count; ++n) outTemperatures[outCell.Vertices[n]] = nodalTemperatures[n];
                }
                else
                {
                    double[] nodalTemperatures = ExtractNodalTemperatures(element, subdomain, systemSolution);
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
                shapeFunctionsAtVertices.Add(element.StandardInterpolation.EvaluateFunctionsAt(vertex));
            }

            // Locate centroid
            double centroidXi = 0.0, centroidEta = 0.0, centroidZeta = 0.0;
            foreach (NaturalPoint vertex in subtriangleVertices)
            {
                centroidXi += vertex.Xi;
                centroidEta += vertex.Eta;
                centroidZeta += vertex.Zeta;
            }
            NaturalPoint centroid = new NaturalPoint(centroidXi / 3.0, centroidEta / 3.0, centroidZeta / 3.0);
            double[] shapeFunctionsAtCentroid = element.StandardInterpolation.EvaluateFunctionsAt(centroid);

            // Evaluate enrichment functions
            var enrichments = new HashSet<IEnrichmentItem>();
            foreach (XNode node in element.Nodes) enrichments.UnionWith(node.EnrichmentItems.Keys);
            var enrichmentValues = new Dictionary<IEnrichmentItem, IList<double[]>>();
            foreach (IEnrichmentItem enrichment in enrichments)
            {
                enrichmentValues[enrichment] = enrichment.EvaluateFunctionsAtSubtriangleVertices(
                    element, shapeFunctionsAtVertices, shapeFunctionsAtCentroid);
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
                    foreach (IEnrichmentItem enrichment in element.Nodes[n].EnrichmentItems.Keys)
                    {
                        double[] psiVertex = enrichmentValues[enrichment][v];
                        double[] psiNode = element.Nodes[n].EnrichmentItems[enrichment];
                        for (int e = 0; e < psiVertex.Length; ++e)
                        {
                            sum += N[n] * (psiVertex[e] - psiNode[e]) * nodalTemperatures[idx++];
                        }
                    }
                }
                temperaturesAtVertices[v] = sum;
            }
            return temperaturesAtVertices;
        }

        private double[] ExtractNodalTemperatures(XThermalElement2D element, XSubdomain subdomain, IVectorView solution)
        {
            var nodalTemperatures = new List<double>(element.Nodes.Count);
            IReadOnlyList<IReadOnlyList<IDofType>> nodalDofs = element.OrderDofsNodeMajor();
            for (int n = 0; n < element.Nodes.Count; ++n)
            {
                XNode node = element.Nodes[n];
                foreach (IDofType dof in nodalDofs[n])
                {
                    bool isFreeDof = subdomain.FreeDofOrdering.FreeDofs.TryGetValue(node, dof, out int idx);
                    if (isFreeDof) nodalTemperatures.Add(solution[idx]);
                    else nodalTemperatures.Add(node.Constraints.Find(con => con.DOF == dof).Amount);
                }
            }
            return nodalTemperatures.ToArray();
        }

        private double[] ExtractNodalTemperaturesStandard(IXFiniteElement element, XSubdomain subdomain, IVectorView solution)
        {
            //TODO: Could this be done using FreeDofOrdering.ExtractVectorElementFromSubdomain(...)? What about enriched dofs?
            var nodalTemperatures = new double[element.Nodes.Count];
            for (int n = 0; n < element.Nodes.Count; ++n)
            {
                XNode node = element.Nodes[n];
                bool isFreeDof = subdomain.FreeDofOrdering.FreeDofs.TryGetValue(node, ThermalDof.Temperature, out int idx);
                if (isFreeDof) nodalTemperatures[n] = solution[idx];
                else nodalTemperatures[n] = node.Constraints.Find(con => con.DOF == ThermalDof.Temperature).Amount;
            }
            return nodalTemperatures;
        }

        
    }
}
