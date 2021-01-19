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


//MODIFICATION NEEDED: This needs splitting up. At the very least separate thermal from structural
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
        /// <summary>
        /// 
        /// </summary>
        /// <param name="point"></param>
        /// <param name="element"></param>
        /// <param name="elementDisplacements">
        /// The order of dofs per node is enrichment major - axis minor.</param>
        /// <returns></returns>
        internal static double[] CalcDisplacementsAt(XPoint point, IXFiniteElement element, IList<double[]> elementDisplacements)
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

        /// <summary>
        /// The gradient is in 2D [Ux,x Ux,y; Uy,x Uy,y] and in 3D [Ux,x Ux,y Ux,z; Uy,x Uy,y Uy,z; Uz,x Uz,y Uz,z].
        /// </summary>
        /// <param name="point"></param>
        /// <param name="evalInterpolation"></param>
        /// <param name="element"></param>
        /// <param name="elementDisplacements"></param>
        /// <returns></returns>
        internal static double[,] CalcDisplacementsGradientAt(
            XPoint point, IXFiniteElement element, IList<double[]> elementDisplacements)
        {
            //TODO: Extend this to 3D
            int dimension = point.Dimension;
            if (point.Dimension != 2) throw new NotImplementedException();
            var gradient = new double[dimension, dimension];
            for (int n = 0; n < element.Nodes.Count; ++n)
            {
                double[] u = elementDisplacements[n];
                double N = point.ShapeFunctions[n];
                var dN = new double[dimension];
                for (int d = 0; d < dimension; ++d)
                {
                    dN[d] = point.ShapeFunctionDerivatives[n, d];
                }

                // Standard displacements
                double ux = u[0];
                double uy = u[1];
                gradient[0, 0] += dN[0] * ux;
                gradient[0, 1] += dN[1] * ux;
                gradient[1, 0] += dN[0] * uy;
                gradient[1, 1] += dN[1] * uy;

                // Eniched displacements
                int dof = 2;
                foreach (IEnrichmentFunction enrichment in element.Nodes[n].EnrichmentFuncs.Keys)
                {
                    ux = u[dof++];
                    uy = u[dof++];
                    EvaluatedFunction evalEnrichment = enrichment.EvaluateAllAt(point);
                    double psi = evalEnrichment.Value;
                    double[] dPsi = evalEnrichment.CartesianDerivatives;
                    double psiNode = element.Nodes[n].EnrichmentFuncs[enrichment];

                    double Bx = (psi - psiNode) * dN[0] + dPsi[0] * N;
                    double By = (psi - psiNode) * dN[1] + dPsi[1] * N;
                    gradient[0, 0] += Bx * ux;
                    gradient[0, 1] += By * ux;
                    gradient[1, 0] += Bx * uy;
                    gradient[1, 1] += By * uy;
                }
            }
            return gradient;
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

        internal static IList<double[]> ExtractElementDisplacements(IXFiniteElement element, XSubdomain subdomain, 
            IVectorView solution)
        {
            var nodalDisplacements = new List<double[]>(element.Nodes.Count);
            IReadOnlyList<IReadOnlyList<IDofType>> nodalDofs = element.GetElementDofTypes(element);
            for (int n = 0; n < element.Nodes.Count; ++n)
            {
                XNode node = element.Nodes[n];
                double[] displacementsOfNode = new double[nodalDofs[n].Count];
                for (int i = 0; i < displacementsOfNode.Length; ++i)
                {
                    IDofType dof = nodalDofs[n][i];
                    bool isFreeDof = subdomain.FreeDofOrdering.FreeDofs.TryGetValue(node, dof, out int idx);
                    if (isFreeDof) displacementsOfNode[i] = solution[idx];
                    else displacementsOfNode[i] = node.Constraints.Find(con => con.DOF == dof).Amount;
                }
                nodalDisplacements.Add(displacementsOfNode);
            }
            return nodalDisplacements.ToArray();
        }

        /// <summary>
        /// Contrary to <see cref="ExtractElementDisplacements(IXFiniteElement, XSubdomain, IVectorView)"/>, this method only
        /// finds the nodal temperatures of an element that correspond to standard dofs.
        /// </summary>
        /// <param name="element"></param>
        /// <param name="subdomain"></param>
        /// <param name="solution"></param>
        /// <returns></returns>
        internal static IList<double[]> ExtractElementDisplacementsStandard(int dimension,
            IXFiniteElement element, XSubdomain subdomain, IVectorView solution)
        {
            var dofsPerNode = new IDofType[dimension];
            dofsPerNode[0] = StructuralDof.TranslationX;
            if (dimension >= 2) dofsPerNode[1] = StructuralDof.TranslationY;
            if (dimension == 3) dofsPerNode[2] = StructuralDof.TranslationZ;

            var elementDisplacements = new List<double[]>(element.Nodes.Count);
            for (int n = 0; n < element.Nodes.Count; ++n)
            {
                XNode node = element.Nodes[n];
                var displacementsOfNode = new double[dimension];
                for (int d = 0; d < dimension; ++d)
                {
                    bool isFreeDof = subdomain.FreeDofOrdering.FreeDofs.TryGetValue(node, dofsPerNode[d], out int idx);
                    if (isFreeDof) displacementsOfNode[d] = solution[idx];
                    else displacementsOfNode[d] = node.Constraints.Find(con => con.DOF == dofsPerNode[d]).Amount;
                }
                elementDisplacements.Add(displacementsOfNode);
            }
            return elementDisplacements;
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

        /// <summary>
        /// Contrary to <see cref="ExtractNodalTemperatures(IXFiniteElement, XSubdomain, IVectorView)"/>, this method only
        /// finds the nodal temperatures of an element that correspond to standard dofs.
        /// </summary>
        /// <param name="element"></param>
        /// <param name="subdomain"></param>
        /// <param name="solution"></param>
        /// <returns></returns>
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
            int dim = nodes[0].Coordinates.Length;
            var result = new double[dim];

            for (int i = 0; i < nodes.Count; ++i)
            {
                for (int d = 0; d < dim; ++d)
                {
                    result[d] += shapeFunctionsAtPoint[i] * nodes[i].Coordinates[d];
                }
            }
            return result;
        }
    }
}
