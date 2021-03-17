using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.LinearAlgebra;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Materials.Interfaces;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Enrichment;
using MGroup.XFEM.Entities;
using MGroup.XFEM.Geometry.ConformingMesh;
using MGroup.XFEM.Geometry.Primitives;
using MGroup.XFEM.Interpolation;
using MGroup.XFEM.Output.Mesh;
using MGroup.XFEM.Output.Vtk;

//TODO: Remove duplication between this and StrainsStressesAtGaussPointsField
//TODO: Refactor these methods. They are too large.
namespace MGroup.XFEM.Output.Fields
{
    public class StrainStressField
    {
        private readonly XModel<IXMultiphaseElement> model;
        private readonly ConformingOutputMesh outMesh;

        public StrainStressField(XModel<IXMultiphaseElement> model, ConformingOutputMesh outMesh)
        {
            this.model = model;
            this.outMesh = outMesh;
        }

        public (IEnumerable<double[]> strains, IEnumerable<double[]> stresses) CalcTensorsAtVertices(IVectorView systemSolution)
        {
            if (model.Subdomains.Count != 1) throw new NotImplementedException();
            XSubdomain subdomain = model.Subdomains.First().Value;

            var outTensors = new Dictionary<VtkPoint, (double[] strains, double[] stresses)>();
            foreach (IXStructuralMultiphaseElement element in subdomain.Elements)
            {
                IEnumerable<ConformingOutputMesh.Subcell> subtriangles = outMesh.GetSubcellsForOriginal(element);
                if (subtriangles.Count() == 0)
                {
                    IList<double[]> elementDisplacements = 
                        Utilities.ExtractElementDisplacements(element, subdomain, systemSolution);
                    Debug.Assert(outMesh.GetOutCellsForOriginal(element).Count() == 1);
                    VtkCell outCell = outMesh.GetOutCellsForOriginal(element).First();
                    for (int n = 0; n < element.Nodes.Count; ++n)
                    {
                        double[] nodeNatural = element.Interpolation.NodalNaturalCoordinates[n];
                        (double[] strain, double[] stress) = CalcStrainStressAt(element, nodeNatural, elementDisplacements);
                        outTensors[outCell.Vertices[n]] = (strain, stress);
                    }
                }
                else
                {
                    IList<double[]> elementDisplacements = 
                        Utilities.ExtractElementDisplacements(element, subdomain, systemSolution);
                    foreach (ConformingOutputMesh.Subcell subcell in subtriangles)
                    {
                        Debug.Assert(subcell.OutVertices.Count == 3 || subcell.OutVertices.Count == 4); //TODO: Not sure what happens for 2nd order elements

                        IList<(double[] strain, double[] stress)> tensors = CalcTensorsInSubcell(
                            element, subcell.OriginalSubcell, elementDisplacements);

                        for (int v = 0; v < subcell.OutVertices.Count; ++v)
                        {
                            VtkPoint vertexOut = subcell.OutVertices[v];
                            outTensors[vertexOut] = tensors[v];
                        }
                    }
                }
            }
            return (outMesh.OutVertices.Select(v => outTensors[v].strains), outMesh.OutVertices.Select(v => outTensors[v].stresses));
        }

        private IList<(double[] strain, double[] stress)> CalcTensorsInSubcell(
            IXStructuralMultiphaseElement element, IElementSubcell subcell, IList<double[]> elementDisplacements)
        {
            // Evaluate shape functions and their derivatives
            var shapeFunctionsAtVertices = new List<EvalInterpolation>(subcell.VerticesNatural.Count);
            for (int v = 0; v < subcell.VerticesNatural.Count; ++v)
            {
                double[] vertex = subcell.VerticesNatural[v];
                shapeFunctionsAtVertices.Add(element.Interpolation.EvaluateAllAt(element.Nodes, vertex));
            }

            // Locate centroid
            double[] centroidNatural = subcell.FindCentroidNatural();
            int dimension = centroidNatural.Length;
            if (dimension != 2) throw new NotImplementedException();
            var centroid = new XPoint(dimension);
            centroid.Element = element;
            centroid.ShapeFunctions = element.Interpolation.EvaluateFunctionsAt(centroidNatural);
            centroid.Coordinates[CoordinateSystem.ElementNatural] = centroidNatural;
            var materialCentroid = StrainsStressesAtGaussPointsField.FindMaterialAt(element, centroid);


            // Evaluate enrichment functions at triangle centroid and assume it also holds for its vertices
            var enrichments = new HashSet<IEnrichmentFunction>();
            foreach (XNode node in element.Nodes) enrichments.UnionWith(node.EnrichmentFuncs.Keys);
            var evalEnrichments = new Dictionary<IEnrichmentFunction, EvaluatedFunction>();
            foreach (IEnrichmentFunction enrichment in enrichments)
            {
                evalEnrichments[enrichment] = enrichment.EvaluateAllAt(centroid);
            }

            // u,x(x) = sum_over_nodes(Ni,x(x) * u_i) + sum_over_enriched_nodes( (N_j(x),x * (psi(x) - psi_j) + N_j(x) * 0)*a_j )
            var allStrainsStresses = new List<(double[] strains, double[] stresses)>(subcell.VerticesNatural.Count);
            for (int v = 0; v < subcell.VerticesNatural.Count; ++v)
            {
                var gradient = new double[dimension, dimension];
                for (int n = 0; n < element.Nodes.Count; ++n)
                {
                    // Shape functions
                    double[] u = elementDisplacements[n];
                    double N = shapeFunctionsAtVertices[v].ShapeFunctions[n];
                    var dN = new double[dimension];
                    for (int d = 0; d < dimension; ++d)
                    {
                        dN[d] = shapeFunctionsAtVertices[v].ShapeGradientsCartesian[n, d];
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
                        EvaluatedFunction evalEnrichment = evalEnrichments[enrichment];
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

                // Strain, stress
                double[] strain = new double[] { gradient[0, 0], gradient[1, 1], gradient[0, 1] + gradient[1, 0] };
                double[] stress = materialCentroid.ConstitutiveMatrix.Multiply(strain);
                allStrainsStresses.Add((strain, stress));
            }
            return allStrainsStresses;
        }

        private (double[] strain, double[] stress) CalcStrainStressAt(
            IXStructuralMultiphaseElement element, double[] pointNatural, IList<double[]> elementDisplacements)
        {
            EvalInterpolation evalInterpolation =
                        element.Interpolation.EvaluateAllAt(element.Nodes, pointNatural);
            double[] coordsCartesian =
                Utilities.TransformNaturalToCartesian(evalInterpolation.ShapeFunctions, element.Nodes);
            var point = new XPoint(coordsCartesian.Length);
            point.Coordinates[CoordinateSystem.ElementNatural] = pointNatural;
            point.Element = element;
            point.ShapeFunctions = evalInterpolation.ShapeFunctions;
            point.ShapeFunctionDerivatives = evalInterpolation.ShapeGradientsCartesian;

            // Strains
            double[,] gradient = StrainsStressesAtGaussPointsField.CalcDisplacementsGradientAt(
                point, element, elementDisplacements);
            double[] strain;
            if (point.Dimension == 2)
            {
                strain = new double[] { gradient[0, 0], gradient[1, 1], gradient[0, 1] + gradient[1, 0] };
            }
            else throw new NotImplementedException();

            // Stresses
            IContinuumMaterial elasticity = StrainsStressesAtGaussPointsField.FindMaterialAt(element, point);
            double[] stress = elasticity.ConstitutiveMatrix.Multiply(strain);

            return (strain, stress);
        }
    }
}
