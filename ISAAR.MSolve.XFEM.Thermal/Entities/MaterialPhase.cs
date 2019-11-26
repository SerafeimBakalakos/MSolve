using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using ISAAR.MSolve.XFEM.Thermal.Elements;
using ISAAR.MSolve.XFEM.Thermal.LevelSetMethod;

namespace ISAAR.MSolve.XFEM.Thermal.Entities
{
    public class MaterialPhase
    {
        private List<(ILsmCurve2D curve, int signIfInside)> boundaries;

        public MaterialPhase(int id)
        {
            this.ID = id;
            this.boundaries = new List<(ILsmCurve2D curve, int signIfInside)>();
        }

        public int ID { get; }

        public void AddBoundary(ILsmCurve2D curve, int signIfInside) => boundaries.Add((curve, signIfInside));

        public bool IsInside(XNode node)
        {
            foreach ((ILsmCurve2D curve, int signIfInside) in boundaries)
            {
                double signedDistance = curve.SignedDistanceOf(node);
                Debug.Assert(signedDistance == 0.0);
                if (Math.Sign(signedDistance) != signIfInside) return false;
            }
            return true;
        }

        public bool IsInside(IXFiniteElement element, double[] shapeFunctionsAtNaturalPoint)
        {
            foreach ((ILsmCurve2D curve, int signIfInside) in boundaries)
            {
                double signedDistance = curve.SignedDistanceOf(element, shapeFunctionsAtNaturalPoint);
                Debug.Assert(signedDistance == 0.0);
                if (Math.Sign(signedDistance) != signIfInside) return false;
            }
            return true;
        }
    }
}
