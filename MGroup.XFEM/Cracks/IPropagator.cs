using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Geometry.Coordinates;
using MGroup.XFEM.Cracks.Geometry;
using MGroup.XFEM.Elements;

namespace MGroup.XFEM.Cracks
{
    public interface IPropagator
    {
        PropagationLogger Logger { get; }

        (double growthAngle, double growthLength) Propagate(Dictionary<int, Vector> totalFreeDisplacements, 
            double[] globalCrackTip, TipCoordinateSystem tipSystem, IReadOnlyList<XCrackElement2D> tipElements);
    }
}
