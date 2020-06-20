using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using ISAAR.MSolve.Geometry.Coordinates;
using MGroup.XFEM.Entities;
using MGroup.XFEM.Geometry.Primitives;

namespace MGroup.XFEM.Elements
{
    public static class XElementExtensions
    {
        public static void FindPhaseAt(this IXFiniteElement element, XPoint point)
        {
            IPhase defaultPhase = null;
            foreach (IPhase phase in element.Phases)
            {
                // Avoid searching for the point in the default phase, since its shape is higly irregular.
                if (phase is DefaultPhase)
                {
                    defaultPhase = phase;
                    continue;
                }
                else if (phase.Contains(point))
                {
                    point.Phase = phase;
                    return;
                }
            }

            // If the point is not contained in any other phases, it must be in the default phase 
            if (defaultPhase == null)
            {
                throw new ArgumentException("The provided point does not belong to any of this element's phases");
            }
            point.Phase = defaultPhase;
        }

        private static void PreparePoint(IXFiniteElement element, XPoint point)
        {
            if (point.Element == null) point.Element = element;
            else if (point.Element != element) throw new ArgumentException("The provided point does not belong to this element");

            if (point.ShapeFunctions == null)
            {
                bool hasNatural = point.Coordinates.TryGetValue(CoordinateSystem.ElementNatural, out double[] natural);
                if (!hasNatural)
                {
                    throw new ArgumentException("Either the natural coordinates of the point or"
                        + " the shape functions of the element evaluated at it must be provided.");
                }

                if (element is IXFiniteElement2D element2D)
                {
                    var pointNatural = new NaturalPoint(natural[0], natural[1]);
                    point.ShapeFunctions = element2D.Interpolation.EvaluateFunctionsAt(pointNatural);
                }
                else if (element is IXFiniteElement3D element3D)
                {
                    var pointNatural = new NaturalPoint(natural[0], natural[1], natural[2]);
                    point.ShapeFunctions = element3D.Interpolation.EvaluateFunctionsAt(pointNatural);
                }
            }
        }
    }
}
