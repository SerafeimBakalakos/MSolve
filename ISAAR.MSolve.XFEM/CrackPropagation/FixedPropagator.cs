using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.XFEM.CrackGeometry.CrackTip;
using ISAAR.MSolve.XFEM.Elements;
using ISAAR.MSolve.XFEM.FreedomDegrees.Ordering;
using ISAAR.MSolve.Geometry.Coordinates;

namespace ISAAR.MSolve.XFEM.CrackPropagation
{
    /// <summary>
    /// Only for propagation from one tip for now.
    /// </summary>
    public class FixedPropagator: IPropagator
    {
        /// <summary>
        /// In the local polar coordinate system defined at the crack tip.
        /// </summary>
        private int iteration;

        public FixedPropagator(double[] growthAngles, double[] growthLengths, double[] sifsMode1, double[] sifsMode2)
        {
            Logger = new PropagationLogger();
            for (int i = 0; i < growthAngles.Length; ++i)
            {
                Logger.GrowthAngles.Add(growthAngles[i]);
                Logger.GrowthLengths.Add(growthLengths[i]);
                Logger.SIFsMode1.Add(sifsMode1[i]);
                Logger.SIFsMode2.Add(sifsMode2[i]);
            }
            iteration = 0;
        }


        public PropagationLogger Logger { get; }

        public (double growthAngle, double growthLength) Propagate(Dictionary<int, Vector> totalFreeDisplacements, 
            CartesianPoint crackTip, TipCoordinateSystem tipSystem, IReadOnlyList<XContinuumElement2D> tipElements)
        {
            if (iteration >= Logger.GrowthLengths.Count) throw new IndexOutOfRangeException(
                $"Only {Logger.GrowthLengths.Count} iterations have been recorder.");
            double angle = Logger.GrowthAngles[iteration];
            double length = Logger.GrowthLengths[iteration];

            ++iteration;
            return (angle, length);
        }
    }
}
