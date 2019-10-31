using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using ISAAR.MSolve.Discretization;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.FEM.Interpolation;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Logging.VTK;
using ISAAR.MSolve.Materials;
using ISAAR.MSolve.XFEM.Thermal.Elements;
using ISAAR.MSolve.XFEM.Thermal.Enrichments;
using ISAAR.MSolve.XFEM.Thermal.Enrichments.Items;
using ISAAR.MSolve.XFEM.Thermal.Entities;
using ISAAR.MSolve.XFEM.Thermal.LevelSetMethod;
using ISAAR.MSolve.XFEM.Thermal.LevelSetMethod.MeshInteraction;
using ISAAR.MSolve.XFEM.Thermal.Output.Mesh;


//TODO: Code duplication between this and TemperatureField2D
namespace ISAAR.MSolve.XFEM.Thermal.Output.Fields
{
    public class HeatFluxField2D
    {
        private readonly XModel model;
        private readonly ILsmCurve2D discontinuity;
        private readonly ConformingOutputMesh2D outMesh;

        public HeatFluxField2D(XModel model, ILsmCurve2D discontinuity, ConformingOutputMesh2D outMesh)
        {
            this.model = model;
            this.discontinuity = discontinuity;
            this.outMesh = outMesh;
        }

        public IEnumerable<double[]> CalcValuesAtVertices(IVectorView systemSolution)
        {
            if (model.Subdomains.Count != 1) throw new NotImplementedException();
            XSubdomain subdomain = model.Subdomains.First().Value;

            var outFlux = new Dictionary<VtkPoint, double[]>();
            foreach (IXFiniteElement e in subdomain.Elements)
            {
                var element = (XThermalElement2D)e;

                var intersection = discontinuity.IntersectElement(element);
                if (intersection.RelativePosition == RelativePositionCurveElement.Disjoint) //TODO: perhaps decide based on outMesh.GetOutCellsForOriginal(element)
                {
                    double[] nodalTemperaturesStd = ExtractNodalTemperaturesStandard(element, subdomain, systemSolution);
                    Debug.Assert(outMesh.GetOutCellsForOriginal(element).Count() == 1);
                    VtkCell outCell = outMesh.GetOutCellsForOriginal(element).First();
                    IList<double[]> nodalFluxes = CalcHeatFluxInStdElementNodes(element, nodalTemperaturesStd);
                    for (int n = 0; n < element.Nodes.Count; ++n) outFlux[outCell.Vertices[n]] = nodalFluxes[n];
                }
                else
                {
                    double[] nodalTemperatures = ExtractNodalTemperatures(element, subdomain, systemSolution);
                    foreach (ConformingOutputMesh2D.Subtriangle subtriangle in outMesh.GetSubtrianglesForOriginal(element))
                    {
                        Debug.Assert(subtriangle.OutVertices.Count == 3); //TODO: Not sure what happens for 2nd order elements

                        // We must interpolate the nodal values, taking into account the enrichements.
                        IList<double[]> fluxAtVertices = CalcHeatFluxFieldInSubtriangle(element,
                            subtriangle.OriginalTriangle.VerticesNatural, nodalTemperatures);

                        for (int v = 0; v < 3; ++v)
                        {
                            VtkPoint vertexOut = subtriangle.OutVertices[v];
                            outFlux[vertexOut] = fluxAtVertices[v];
                        }
                    }
                }
            }
            return outMesh.OutVertices.Select(v => outFlux[v]);
        }

        private IList<double[]> CalcHeatFluxInStdElementNodes(XThermalElement2D element, double[] nodalTemperaturesStd)
        {
            var fluxAtVertices = new List<double[]>();
            foreach (NaturalPoint vertex in element.StandardInterpolation.NodalNaturalCoordinates)
            {
                EvalInterpolation2D shapeFunctions = element.StandardInterpolation.EvaluateAllAt(element.Nodes, vertex);
                ThermalMaterial material = element.MaterialField.GetMaterialAt(element, shapeFunctions.ShapeFunctions);
                double c = material.ThermalConductivity;

                // flux_x = - conductivity * sum_over_nodes_i(N,x(x) * t_i)
                var flux = new double[2];
                for (int n = 0; n < element.Nodes.Count; ++n)
                {
                    flux[0] += shapeFunctions.ShapeGradientsCartesian[n, 0] * nodalTemperaturesStd[n];
                    flux[1] += shapeFunctions.ShapeGradientsCartesian[n, 1] * nodalTemperaturesStd[n];
                }

                fluxAtVertices.Add(new double[] { -c * flux[0], -c * flux[1] });
            }
            return fluxAtVertices;
        }

        private IList<double[]> CalcHeatFluxFieldInSubtriangle(XThermalElement2D element, IList<NaturalPoint> subtriangleVertices, 
            double[] nodalTemperatures)
        {
            // Evaluate shape functions and derivatives
            var shapeFunctionsAtVertices = new List<EvalInterpolation2D>(subtriangleVertices.Count);
            for (int v = 0; v < subtriangleVertices.Count; ++v)
            {
                NaturalPoint vertex = subtriangleVertices[v];
                shapeFunctionsAtVertices.Add(element.StandardInterpolation.EvaluateAllAt(element.Nodes, vertex)); 
            }

            // Locate centroid and material
            double centroidXi = 0.0, centroidEta = 0.0, centroidZeta = 0.0;
            foreach (NaturalPoint vertex in subtriangleVertices)
            {
                centroidXi += vertex.Xi;
                centroidEta += vertex.Eta;
                centroidZeta += vertex.Zeta;
            }
            NaturalPoint centroid = new NaturalPoint(centroidXi / 3.0, centroidEta / 3.0, centroidZeta / 3.0);
            double[] shapeFunctionsAtCentroid = element.StandardInterpolation.EvaluateFunctionsAt(centroid);
            ThermalMaterial material = element.MaterialField.GetMaterialAt(element, shapeFunctionsAtCentroid);

            // Evaluate enrichment functions and derivatives
            var enrichments = new HashSet<IEnrichmentItem>();
            foreach (XNode node in element.Nodes) enrichments.UnionWith(node.EnrichmentItems.Keys);
            var enrichmentValues = new Dictionary<IEnrichmentItem, IList<EvaluatedFunction[]>>();
            foreach (IEnrichmentItem enrichment in enrichments)
            {
                enrichmentValues[enrichment] = enrichment.EvaluateAllAtSubtriangleVertices(
                    element, shapeFunctionsAtVertices.Select(N => N.ShapeFunctions).ToArray(), shapeFunctionsAtCentroid);
            }

            // t,x(x) = sum_over_nodes(Ni,x(x) * t_i) 
            //  + sum_over_enriched_nodes(( N_j,x(x) * (psi(x) - psi_j) + N_j(x) * psi,x(x) )  * a_j)
            var fluxAtVertices = new List<double[]>(subtriangleVertices.Count);
            for (int v = 0; v < subtriangleVertices.Count; ++v)
            {
                double[] N = shapeFunctionsAtVertices[v].ShapeFunctions;
                Matrix gradN = shapeFunctionsAtVertices[v].ShapeGradientsCartesian;
                var flux = new double[2];
                int idx = 0;
                for (int n = 0; n < element.Nodes.Count; ++n)
                {
                    // Standard temperatures
                    flux[0] += gradN[n, 0] * nodalTemperatures[idx];
                    flux[1] += gradN[n, 1] * nodalTemperatures[idx];
                    ++idx;

                    // Eniched temperatures
                    foreach (IEnrichmentItem enrichment in element.Nodes[n].EnrichmentItems.Keys)
                    {
                        EvaluatedFunction[] psiVertex = enrichmentValues[enrichment][v];
                        double[] psiNode = element.Nodes[n].EnrichmentItems[enrichment];
                        for (int e = 0; e < psiVertex.Length; ++e)
                        {
                            flux[0] += gradN[n, 0] * ((psiVertex[e].Value - psiNode[e]) 
                                + N[n] * psiVertex[e].CartesianDerivatives[0]) * nodalTemperatures[idx];
                            flux[1] += gradN[n, 1] * ((psiVertex[e].Value - psiNode[e])
                                + N[n] * psiVertex[e].CartesianDerivatives[1]) * nodalTemperatures[idx];
                            ++idx;
                        }
                    }
                }

                fluxAtVertices.Add(new double[]
                {
                    - material.ThermalConductivity * flux[0], - material.ThermalConductivity * flux[1]
                });
            }
            return fluxAtVertices;
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
