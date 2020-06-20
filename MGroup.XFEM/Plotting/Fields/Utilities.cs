using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.FEM.Interpolation;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Enrichment;
using MGroup.XFEM.Entities;
using MGroup.XFEM.Geometry.Primitives;

namespace MGroup.XFEM.Plotting.Fields
{
    internal static class Utilities
    {
        //TODO: Perhaps this should be implemented by the element itself, where a lot of optimizations can be employed.
        internal static double CalcTemperatureAt(XPoint point, IXFiniteElement element, double[] nodalTemperatures)
        {
            double sum = 0.0;
            int idx = 0;
            for (int n = 0; n < element.Nodes.Count; ++n)
            {
                // Standard temperatures
                sum += point.ShapeFunctions[n] * nodalTemperatures[idx++];

                // Eniched temperatures
                foreach (IEnrichment enrichment in element.Nodes[n].Enrichments.Keys)
                {
                    double psiVertex = enrichment.EvaluateAt(point);
                    double psiNode = element.Nodes[n].Enrichments[enrichment];
                    sum += point.ShapeFunctions[n] * (psiVertex - psiNode) * nodalTemperatures[idx++];
                }
            }
            return sum;
        }

        internal static double[] CalcTemperatureGradientAt(XPoint point, EvalInterpolation2D evalInterpolation,
            IXFiniteElement element, double[] nodalTemperatures)
        {
            var gradient = new double[2];
            int idx = 0;
            for (int n = 0; n < element.Nodes.Count; ++n)
            {
                double dNdx = evalInterpolation.ShapeGradientsCartesian[n, 0];
                double dNdy = evalInterpolation.ShapeGradientsCartesian[n, 1];

                // Standard temperatures
                double stdTi = nodalTemperatures[idx++];
                gradient[0] += dNdx * stdTi;
                gradient[1] += dNdy * stdTi;

                // Eniched temperatures
                foreach (IEnrichment enrichment in element.Nodes[n].Enrichments.Keys)
                {
                    double psiVertex = enrichment.EvaluateAt(point);
                    double psiNode = element.Nodes[n].Enrichments[enrichment];
                    double enrTij = nodalTemperatures[idx++];

                    gradient[0] += dNdx * (psiVertex - psiNode) * enrTij;
                    gradient[1] += dNdy * (psiVertex - psiNode) * enrTij;
                }
            }
            return gradient;
        }

        internal static double[] CalcTemperatureGradientAt(XPoint point, EvalInterpolation3D evalInterpolation,
            IXFiniteElement element, double[] nodalTemperatures)
        {
            var gradient = new double[3];
            int idx = 0;
            for (int n = 0; n < element.Nodes.Count; ++n)
            {
                double dNdx = evalInterpolation.ShapeGradientsCartesian[n, 0];
                double dNdy = evalInterpolation.ShapeGradientsCartesian[n, 1];
                double dNdz = evalInterpolation.ShapeGradientsCartesian[n, 2];

                // Standard temperatures
                double stdTi = nodalTemperatures[idx++];
                gradient[0] += dNdx * stdTi;
                gradient[1] += dNdy * stdTi;
                gradient[2] += dNdz * stdTi;

                // Eniched temperatures
                foreach (IEnrichment enrichment in element.Nodes[n].Enrichments.Keys)
                {
                    double psiVertex = enrichment.EvaluateAt(point);
                    double psiNode = element.Nodes[n].Enrichments[enrichment];
                    double enrTij = nodalTemperatures[idx++];

                    gradient[0] += dNdx * (psiVertex - psiNode) * enrTij;
                    gradient[1] += dNdy * (psiVertex - psiNode) * enrTij;
                    gradient[2] += dNdz * (psiVertex - psiNode) * enrTij;
                }
            }
            return gradient;
        }

        internal static double[] ExtractNodalTemperatures(IXFiniteElement element, XSubdomain subdomain, IVectorView solution)
        {
            var nodalTemperatures = new List<double>(element.Nodes.Count);
            IReadOnlyList<IReadOnlyList<IDofType>> nodalDofs = element.GetElementDofTypes(element);
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

        internal static double[] ExtractNodalTemperaturesStandard(IXFiniteElement element, XSubdomain subdomain, IVectorView solution)
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

        internal static double[] TransformNaturalToCartesian(double[] shapeFunctionsAtPoint, IReadOnlyList<XNode> nodes)
        {
            double x = 0, y = 0, z = 0;
            for (int i = 0; i < nodes.Count; ++i)
            {
                x += shapeFunctionsAtPoint[i] * nodes[i].X;
                y += shapeFunctionsAtPoint[i] * nodes[i].Y;
                z += shapeFunctionsAtPoint[i] * nodes[i].Z;
            }
            return new double[] { x, y, z};
        }
    }
}
