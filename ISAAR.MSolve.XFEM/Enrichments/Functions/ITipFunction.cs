using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ISAAR.MSolve.XFEM_OLD.CrackGeometry.CrackTip;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.XFEM_OLD.Utilities;

namespace ISAAR.MSolve.XFEM_OLD.Enrichments.Functions
{
    public interface ITipFunction: IEnrichmentFunction2D
    {
        double EvaluateAt(PolarPoint2D point);
        EvaluatedFunction2D EvaluateAllAt(PolarPoint2D point, TipJacobians jacobian);
    }
}
