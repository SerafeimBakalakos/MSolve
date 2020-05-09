using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.XFEM_OLD.Elements;

namespace ISAAR.MSolve.XFEM_OLD.CrackGeometry.Implicit.MeshInteraction
{
    interface IMeshInteraction
    {
        CrackElementPosition FindRelativePositionOf(XContinuumElement2D element);
    }
}
