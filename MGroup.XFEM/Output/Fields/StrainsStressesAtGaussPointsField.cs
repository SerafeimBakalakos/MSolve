using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.LinearAlgebra;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Materials.Interfaces;
using MGroup.XFEM.Elements;
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
                    double[,] gradient = Utilities.CalcDisplacementsGradientAt(point, element, elementDisplacements);
                    double[] strain;
                    if (point.Dimension == 2)
                    {
                        strain = new double[] { gradient[0, 0], gradient[1, 1], gradient[0, 1] + gradient[1, 0] };
                    }
                    else throw new NotImplementedException();
                    strains[coordsCartesian] = strain;

                    // Stresses
                    IContinuumMaterial2D elasticity = FindMaterialAt(element, point);
                    stresses[coordsCartesian] = elasticity.ConstitutiveMatrix.Multiply(strain);
                }
            }
            return (strains, stresses);
        }

        //TODO: Do not use this for gauss points, since this work is already done by the element itself
        private IContinuumMaterial2D FindMaterialAt(IXStructuralMultiphaseElement element, XPoint point)
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
