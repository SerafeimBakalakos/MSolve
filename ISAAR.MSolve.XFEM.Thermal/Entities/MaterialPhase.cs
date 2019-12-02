using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using ISAAR.MSolve.XFEM.Thermal.Elements;
using ISAAR.MSolve.XFEM.Thermal.Curves;

namespace ISAAR.MSolve.XFEM.Thermal.Entities
{
    public class MaterialPhase
    {
        private List<(ICurve2D curve, int signIfInside)> boundaries;

        public MaterialPhase(int id)
        {
            this.ID = id;
            this.boundaries = new List<(ICurve2D curve, int signIfInside)>();
        }

        public int ID { get; }

        public void AddBoundary(ICurve2D curve, int signIfInside) => boundaries.Add((curve, signIfInside));

        public bool IsInside(XNode node)
        {
            foreach ((ICurve2D curve, int signIfInside) in boundaries)
            {
                double signedDistance = curve.SignedDistanceOf(node);
                Debug.Assert(signedDistance != 0.0);
                if (Math.Sign(signedDistance) != signIfInside) return false;
            }
            return true;
        }

        public bool IsInside(IXFiniteElement element, double[] shapeFunctionsAtNaturalPoint)
        {
            foreach ((ICurve2D curve, int signIfInside) in boundaries)
            {
                double signedDistance = curve.SignedDistanceOf(element, shapeFunctionsAtNaturalPoint);
                Debug.Assert(signedDistance == 0.0);
                if (Math.Sign(signedDistance) != signIfInside) return false;
            }
            return true;
        }
    }
}
