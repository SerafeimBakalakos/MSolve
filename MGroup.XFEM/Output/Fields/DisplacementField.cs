﻿using System;
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
using MGroup.XFEM.Output.Mesh;
using MGroup.XFEM.Output.Vtk;

namespace MGroup.XFEM.Output.Fields
{
    public class DisplacementField
    {
        private readonly XModel<IXMultiphaseElement> model;
        private readonly ConformingOutputMesh outMesh;

        public DisplacementField(XModel<IXMultiphaseElement> model, ConformingOutputMesh outMesh)
        {
            this.model = model;
            this.outMesh = outMesh;
        }

        public IEnumerable<double[]> CalcValuesAtVertices(IVectorView systemSolution)
        {
            if (model.Subdomains.Count != 1) throw new NotImplementedException();
            XSubdomain subdomain = model.Subdomains.First().Value;

            var outDisplacements = new Dictionary<VtkPoint, double[]>();
            foreach (IXFiniteElement element in subdomain.Elements)
            {

                IEnumerable<ConformingOutputMesh.Subcell> subtriangles = outMesh.GetSubcellsForOriginal(element);
                if (subtriangles.Count() == 0)
                {
                    IList<double[]> elementDisplacements = 
                        ExtractElementDisplacementsStandard(model.Dimension, element, subdomain, systemSolution);
                    Debug.Assert(outMesh.GetOutCellsForOriginal(element).Count() == 1);
                    VtkCell outCell = outMesh.GetOutCellsForOriginal(element).First();
                    for (int n = 0; n < element.Nodes.Count; ++n)
                    {
                        outDisplacements[outCell.Vertices[n]] = elementDisplacements[n];
                    }
                }
                else
                {
                    IList<double[]> elementDisplacements = 
                        Utilities.ExtractElementDisplacements(element, subdomain, systemSolution);
                    foreach (ConformingOutputMesh.Subcell subcell in subtriangles)
                    {
                        Debug.Assert(subcell.OutVertices.Count == 3 || subcell.OutVertices.Count == 4); //TODO: Not sure what happens for 2nd order elements

                        // We must interpolate the nodal values, taking into account the enrichements.
                        IList<double[]> temperatureAtVertices = CalcDisplacementFieldInSubtriangle(element,
                            subcell.OriginalSubcell, elementDisplacements);

                        for (int v = 0; v < subcell.OutVertices.Count; ++v)
                        {
                            VtkPoint vertexOut = subcell.OutVertices[v];
                            outDisplacements[vertexOut] = temperatureAtVertices[v];
                        }
                    }
                }
            }
            return outMesh.OutVertices.Select(v => outDisplacements[v]);
        }

        /// <summary>
        /// Contrary to <see cref="Utilities.ExtractElementDisplacements(IXFiniteElement, XSubdomain, IVectorView)"/>, 
        /// this method only finds the nodal temperatures of an element that correspond to standard dofs.
        /// </summary>
        /// <param name="element"></param>
        /// <param name="subdomain"></param>
        /// <param name="solution"></param>
        /// <returns></returns>
        public static IList<double[]> ExtractElementDisplacementsStandard(int dimension,
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

        private IList<double[]> CalcDisplacementFieldInSubtriangle(IXFiniteElement element, IElementSubcell subcell,
            IList<double[]> elementDisplacements)
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
            int dimension = centroidNatural.Length;
            var centroid = new XPoint(dimension);
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

            // u(x) = sum_over_nodes(Ni(x) * u_i) + sum_over_enriched_nodes( N_j(x) * (psi(x) - psi_j)*a_j )
            var displacementsAtVertices = new List<double[]>(subcell.VerticesNatural.Count);
            for (int v = 0; v < subcell.VerticesNatural.Count; ++v)
            {
                double[] N = shapeFunctionsAtVertices[v];
                var sums = new double[dimension];
                for (int n = 0; n < element.Nodes.Count; ++n)
                {
                    double[] u = elementDisplacements[n];

                    // Standard temperatures
                    int dof = 0;
                    for (int d = 0; d < dimension; ++d)
                    {
                        sums[d] += N[n] * u[dof++];
                    }

                    // Eniched temperatures
                    foreach (IEnrichmentFunction enrichment in element.Nodes[n].EnrichmentFuncs.Keys)
                    {
                        double psiVertex = enrichmentValues[enrichment];
                        double psiNode = element.Nodes[n].EnrichmentFuncs[enrichment];
                        for (int d = 0; d < dimension; ++d)
                        {
                            sums[d] += N[n] * (psiVertex - psiNode) * u[dof++];
                        }
                    }
                }
                displacementsAtVertices.Add(sums);
            }
            return displacementsAtVertices;
        }
    }
}