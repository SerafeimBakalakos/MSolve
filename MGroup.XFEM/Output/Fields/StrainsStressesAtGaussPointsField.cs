using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.LinearAlgebra;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Materials.Interfaces;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Enrichment;
using MGroup.XFEM.Entities;
using MGroup.XFEM.Geometry.Primitives;
using MGroup.XFEM.Integration;
using MGroup.XFEM.Interpolation;
using MGroup.XFEM.Phases;

namespace MGroup.XFEM.Output.Fields
{
    public class StrainsStressesAtGaussPointsField
    {
        private readonly XModel<IXMultiphaseElement> model;

        public StrainsStressesAtGaussPointsField(XModel<IXMultiphaseElement> model)
        {
            this.model = model;
        }

        public (Dictionary<double[], double[]> strains, Dictionary<double[], double[]> stresses) 
            CalcTensorsAtPoints(IVectorView solution)
        {
            if (model.Subdomains.Count != 1) throw new NotImplementedException();
            XSubdomain subdomain = model.Subdomains.First().Value;
            DofTable dofTable = subdomain.FreeDofOrdering.FreeDofs;

            var strains = new Dictionary<double[], double[]>();
            var stresses = new Dictionary<double[], double[]>();
            foreach (IXStructuralMultiphaseElement element in model.Elements)
            {
                IEnumerable<GaussPoint> gaussPoints = element.BulkIntegrationPoints;
                IList<double[]> elementDisplacements = Utilities.ExtractElementDisplacements(element, subdomain, solution);
                foreach (GaussPoint pointNatural in gaussPoints)
                {
                    EvalInterpolation evalInterpolation =
                        element.Interpolation.EvaluateAllAt(element.Nodes, pointNatural.Coordinates);
                    double[] coordsCartesian = 
                        Utilities.TransformNaturalToCartesian(evalInterpolation.ShapeFunctions, element.Nodes);
                    var point = new XPoint(coordsCartesian.Length);
                    point.Coordinates[CoordinateSystem.ElementNatural] = pointNatural.Coordinates;
                    point.Element = element;
                    point.ShapeFunctions = evalInterpolation.ShapeFunctions;
                    point.ShapeFunctionDerivatives = evalInterpolation.ShapeGradientsCartesian;

                    // Strains
                    double[,] gradient = CalcDisplacementsGradientAt(point, element, elementDisplacements);
                    double[] strain;
                    if (point.Dimension == 2)
                    {
                        strain = new double[] { gradient[0, 0], gradient[1, 1], gradient[0, 1] + gradient[1, 0] };
                    }
                    else throw new NotImplementedException();
                    strains[coordsCartesian] = strain;

                    // Stresses
                    IContinuumMaterial elasticity = FindMaterialAt(element, point);
                    stresses[coordsCartesian] = elasticity.ConstitutiveMatrix.Multiply(strain);
                }
            }
            return (strains, stresses);
        }

        /// <summary>
        /// The gradient is in 2D [Ux,x Ux,y; Uy,x Uy,y] and in 3D [Ux,x Ux,y Ux,z; Uy,x Uy,y Uy,z; Uz,x Uz,y Uz,z].
        /// </summary>
        /// <param name="point"></param>
        /// <param name="evalInterpolation"></param>
        /// <param name="element"></param>
        /// <param name="elementDisplacements"></param>
        /// <returns></returns>
        public static double[,] CalcDisplacementsGradientAt(
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

        //TODO: Do not use this for gauss points, since this work is already done by the element itself
        public static IContinuumMaterial FindMaterialAt(IXStructuralMultiphaseElement element, XPoint point)
        {
            // Find the phase at this integration point.
            IPhase phase = null;
            Debug.Assert(element.Phases.Count != 0);
            if (element.Phases.Count == 1)
            {
                phase = element.Phases.First();
                point.PhaseID = phase.ID;
            }
            else
            {
                phase = element.FindPhaseAt(point);
                point.PhaseID = phase.ID;
            }

            // Find the material for this phase
            return element.MaterialField.FindMaterialAt(phase);
        }
    }
}
