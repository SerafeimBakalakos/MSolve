using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.XFEM_OLD.CrackGeometry.CrackTip;
using ISAAR.MSolve.XFEM_OLD.Elements;
using ISAAR.MSolve.XFEM_OLD.FreedomDegrees.Ordering;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.XFEM_OLD.Entities;

namespace ISAAR.MSolve.XFEM_OLD.CrackPropagation
{
    public interface IPropagator
    {
        PropagationLogger Logger { get; }

        (double growthAngle, double growthLength) Propagate(Dictionary<int, Vector> totalFreeDisplacements, CartesianPoint crackTip, 
            TipCoordinateSystem tipSystem, IReadOnlyList<XContinuumElement2D> tipElements);
    }
}
