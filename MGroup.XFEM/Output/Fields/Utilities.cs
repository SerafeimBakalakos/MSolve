using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Enrichment;
using MGroup.XFEM.Entities;
using MGroup.XFEM.Geometry.ConformingMesh;
using MGroup.XFEM.Geometry.Primitives;
using MGroup.XFEM.Interpolation;
using MGroup.XFEM.Phases;

namespace MGroup.XFEM.Output.Fields
{
    public static class Utilities
    {
        public static Dictionary<int, double> CalcBulkSizeOfEachPhase(XModel<IXMultiphaseElement> physicalModel,
            PhaseGeometryModel geometryModel)
        {
            var bulkSizes = new Dictionary<int, double>();
            foreach (IPhase phase in geometryModel.Phases.Values) bulkSizes[phase.ID] = 0.0;

            foreach (IXMultiphaseElement element in physicalModel.Elements)
            {
                if ((element.ConformingSubcells == null) || (element.ConformingSubcells.Length == 0))
                {
                    System.Diagnostics.Debug.Assert(element.Phases.Count == 1);
                    IPhase phase = element.Phases.First();
                    double elementBulkSize = element.CalcBulkSizeCartesian();
                    bulkSizes[phase.ID] += elementBulkSize;
                }
                else
                {
                    foreach (IElementSubcell subcell in element.ConformingSubcells)
                    {
                        double[] centroidNatural = subcell.FindCentroidNatural();
                        var centroid = new XPoint(centroidNatural.Length);
                        centroid.Coordinates[CoordinateSystem.ElementNatural] = centroidNatural;
                        centroid.Element = element;
                        centroid.ShapeFunctions =
                            element.Interpolation.EvaluateFunctionsAt(centroid.Coordinates[CoordinateSystem.ElementNatural]);
                        IPhase phase = element.FindPhaseAt(centroid);
                        centroid.PhaseID = phase.ID;

                        (_, double subcellBulk) = subcell.FindCentroidAndBulkSizeCartesian(element);

                        bulkSizes[phase.ID] += subcellBulk;
                    }
                }
            }

            return bulkSizes;
        }

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
                foreach (IEnrichmentFunction enrichment in element.Nodes[n].EnrichmentFuncs.Keys)
                {
                    double psiVertex = enrichment.EvaluateAt(point);
                    double psiNode = element.Nodes[n].EnrichmentFuncs[enrichment];
                    sum += point.ShapeFunctions[n] * (psiVertex - psiNode) * nodalTemperatures[idx++];
                }
            }
            return sum;
        }

        internal static double[] CalcTemperatureGradientAt(XPoint point, EvalInterpolation evalInterpolation,
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
