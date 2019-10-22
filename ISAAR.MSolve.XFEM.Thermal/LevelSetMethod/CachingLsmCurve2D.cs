using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.Geometry.Shapes;
using ISAAR.MSolve.XFEM.Thermal.Elements;
using ISAAR.MSolve.XFEM.Thermal.Entities;
using ISAAR.MSolve.XFEM.Thermal.LevelSetMethod.MeshInteraction;

namespace ISAAR.MSolve.XFEM.Thermal.LevelSetMethod
{
    public class CachingLsmCurve2D : ILsmCurve2D
    {
        private readonly SimpleLsmCurve2D lsm;
        private readonly Dictionary<IXFiniteElement, CurveElementIntersection> affectedElements;

        public CachingLsmCurve2D(double interfaceThickness = 1.0)
        {
            this.lsm = new SimpleLsmCurve2D(interfaceThickness);
            this.affectedElements = new Dictionary<IXFiniteElement, CurveElementIntersection>();
        }

        public double Thickness => lsm.Thickness;

        public void InitializeGeometry(IEnumerable<XNode> nodes, IEnumerable<IXFiniteElement> elements, ICurve2D discontinuity)
        {
            lsm.InitializeGeometry(nodes, discontinuity);
            foreach (IXFiniteElement element in elements)
            {
                CurveElementIntersection intersection = lsm.IntersectElement(element);
                if (intersection.RelativePosition != RelativePositionCurveElement.Disjoint)
                {
                    affectedElements[element] = intersection;
                }
            }
        }

        public CurveElementIntersection IntersectElement(IXFiniteElement element)
        {
            bool isCached = affectedElements.TryGetValue(element, out CurveElementIntersection intersection);
            if (isCached) return intersection;
            else return new CurveElementIntersection(RelativePositionCurveElement.Disjoint, new NaturalPoint[0], new XNode[0]);
        }

        public double SignedDistanceOf(XNode node) => lsm.SignedDistanceOf(node);

        public double SignedDistanceOf(IXFiniteElement element, double[] shapeFunctionsAtNaturalPoint)
            => lsm.SignedDistanceOf(element, shapeFunctionsAtNaturalPoint);
    }
}
