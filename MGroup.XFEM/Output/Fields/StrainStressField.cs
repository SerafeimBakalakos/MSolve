using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Discretization.Mesh;
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
using MGroup.XFEM.Phases;

//TODO: Remove duplication between this and StrainsStressesAtGaussPointsField
//TODO: Refactor these methods. They are too large.
namespace MGroup.XFEM.Output.Fields
{
    public class StrainStressField
    {
        private const double offsetTol = 1E-6;

        private readonly int dimension;
        private readonly XModel<IXMultiphaseElement> model;
        private readonly ConformingOutputMesh outMesh;

        public StrainStressField(XModel<IXMultiphaseElement> model, ConformingOutputMesh outMesh)
        {
            this.model = model;
            this.dimension = model.Dimension;
            this.outMesh = outMesh;
        }

        public (IEnumerable<double[]> strains, IEnumerable<double[]> stresses) CalcTensorsAtVertices(IVectorView systemSolution)
        {
            if (model.Subdomains.Count != 1) throw new NotImplementedException();
            XSubdomain subdomain = model.Subdomains.First().Value;

            var outTensors = new Dictionary<VtkPoint, (double[] strains, double[] stresses)>();
            foreach (IXStructuralMultiphaseElement element in subdomain.Elements)
            {
                IList<double[]> elementDisplacements =
                        Utilities.ExtractElementDisplacements(element, subdomain, systemSolution);
                HashSet<IEnrichmentFunction> elementEnrichments = element.FindEnrichments();

                IEnumerable<ConformingOutputMesh.Subcell> subtriangles = outMesh.GetSubcellsForOriginal(element);
                if (subtriangles.Count() == 0)
                {
                    //TODO: This is incorrect for blending elements: 1) enriched dofs are not irrelevant, 2) using the
                    //      FEM interpolation is not accurate, 3) they should be subdivided into subcells to accurately 
                    //      calculate the displacement at their vertices and then let ParaView interpolate that (inaccurately).
                    //      However some enrichments (step, ridge) do not produce blending elements (although in code, there will
                    //      be elements where only some nodes are enriched). Thus the user should choose for now.
                    Debug.Assert(outMesh.GetOutCellsForOriginal(element).Count() == 1);
                    VtkCell outCell = outMesh.GetOutCellsForOriginal(element).First();
                    for (int n = 0; n < element.Nodes.Count; ++n)
                    {
                        double[] nodeNatural = element.Interpolation.NodalNaturalCoordinates[n];
                        (double[] strains, double[] stresses) tensors = CalcStrainsStressesAt(
                                nodeNatural, element, elementDisplacements, elementEnrichments); //TODO: optimizations are possible for nodes
                        VtkPoint vertexOut = outCell.Vertices[n];
                        outTensors[vertexOut] = tensors;
                    }
                }
                else
                {
                    foreach (ConformingOutputMesh.Subcell subcell in subtriangles)
                    {
                        //TODO: Not sure what happens for 2nd order elements
                        Debug.Assert(subcell.OriginalSubcell.CellType == CellType.Tri3
                            || subcell.OriginalSubcell.CellType == CellType.Quad4
                            || subcell.OriginalSubcell.CellType == CellType.Tet4
                            || subcell.OriginalSubcell.CellType == CellType.Hexa8);

                        // Find centroid of subcell
                        IList<double[]> verticesNatural = subcell.OriginalSubcell.VerticesNatural;
                        double[] centroid = subcell.OriginalSubcell.FindCentroidNatural();

                        for (int v = 0; v < verticesNatural.Count; ++v)
                        {
                            // Slightly offset point towards its centroid
                            double[] vertex = verticesNatural[v];
                            var vertexOffset = new double[dimension];
                            for (int d = 0; d < dimension; ++d)
                            {
                                vertexOffset[d] = vertex[d] + offsetTol * (centroid[d] - vertex[d]);
                            }

                            // Interpolate the nodal values, taking into account the enrichments.
                            (double[] strains, double[] stresses) tensors = CalcStrainsStressesAt(
                                vertexOffset, element, elementDisplacements, elementEnrichments);
                            VtkPoint vertexOut = subcell.OutVertices[v];
                            outTensors[vertexOut] = tensors;
                        }
                    }
                }
            }
            return (outMesh.OutVertices.Select(v => outTensors[v].strains), outMesh.OutVertices.Select(v => outTensors[v].stresses));
        }


        /// <summary>
        /// 2D displacement gradient: [Ux,x Ux,y; Uy,x Uy,y].
        /// 3D displacement gradient: [Ux,x Ux,y Ux,z; Uy,x Uy,y Uy,z; Uz,x Uz,y Uz,z].
        /// </summary>
        /// <returns></returns>
        private double[,] CalcDisplacementGradient2DAt(XPoint point,  
            IList<double[]> elementDisplacements, IEnumerable<IEnrichmentFunction> elementEnrichments) //TODO: Extend this to 3D
        {
            IXFiniteElement element = point.Element;

            // Enrichment functions and derivatives at this point
            var enrichmentValues = new Dictionary<IEnrichmentFunction, EvaluatedFunction>();
            foreach (IEnrichmentFunction enrichment in elementEnrichments)
            {
                enrichmentValues[enrichment] = enrichment.EvaluateAllAt(point);
            }

            // u,x(x) = sum_over_nodes(Ni,x(x) * u_i) 
            //  + sum_over_enriched_nodes( (N_j,x(x) * (psi(x) - psi_j) N_j(x) * + psi,x(x) )*a_j )
            var gradient = new double[dimension, dimension];
            for (int n = 0; n < element.Nodes.Count; ++n)
            {
                double[] uStd = elementDisplacements[n];
                double N = point.ShapeFunctions[n];
                var dN = new double[dimension];
                for (int d = 0; d < dimension; ++d)
                {
                    dN[d] = point.ShapeFunctionDerivatives[n, d];
                }

                // Standard displacements
                double ux = uStd[0];
                double uy = uStd[1];
                gradient[0, 0] += dN[0] * ux;
                gradient[0, 1] += dN[1] * ux;
                gradient[1, 0] += dN[0] * uy;
                gradient[1, 1] += dN[1] * uy;

                // Eniched displacements
                int dof = 2;
                foreach (IEnrichmentFunction enrichment in element.Nodes[n].EnrichmentFuncs.Keys)
                {
                    ux = uStd[dof++];
                    uy = uStd[dof++];
                    EvaluatedFunction evalEnrichment = enrichmentValues[enrichment];
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

        /// <summary>
        /// 2D strains: [ex; ey; exy] = [Ux,x; Uy,y; Ux,y + Uy,x].
        /// 3D strains: [ex; ey; ez; exy; eyz; ezx] = [Ux,x; Uy,y; Uz,z; Ux,y + Uy,x; Uy,z + Uz,y; Uz,x + Ux,z].
        /// </summary>
        /// <returns></returns>
        private double[] CalcStrains2DAt(XPoint point,
            IList<double[]> elementDisplacements, IEnumerable<IEnrichmentFunction> elementEnrichments)
        {
            double[,] gradient = CalcDisplacementGradient2DAt(point, elementDisplacements, elementEnrichments);
            double[] strains =
            {
                gradient[0, 0],
                gradient[1, 1],
                gradient[0, 1] + gradient[1, 0]
            };
            return strains;
        }

        //TODO: extend to 3D
        //TODO: Investigate using Bstd, Benr
        private (double[] strains, double[] stresss) CalcStrainsStressesAt(double[] pointNatural,
            IXStructuralMultiphaseElement element, IList<double[]> elementDisplacements, 
            IEnumerable<IEnrichmentFunction> elementEnrichments)
        {
            // Prepare this point
            var point = new XPoint(dimension);
            point.Element = element;
            point.Coordinates[CoordinateSystem.ElementNatural] = pointNatural;
            EvalInterpolation interpolation = element.Interpolation.EvaluateAllAt(element.Nodes, pointNatural);
            point.ShapeFunctions = interpolation.ShapeFunctions;
            point.ShapeFunctionDerivatives = interpolation.ShapeGradientsCartesian;

            // Calculate strains
            double[] strains = CalcStrains2DAt(point, elementDisplacements, elementEnrichments);

            // Material
            IPhase phase = element.FindPhaseAt(point);
            IContinuumMaterial material = element.MaterialField.FindMaterialAt(phase);

            // Stresses
            double[] stresses = material.ConstitutiveMatrix.Multiply(strains);

            return (strains, stresses);
        }
    }
}
