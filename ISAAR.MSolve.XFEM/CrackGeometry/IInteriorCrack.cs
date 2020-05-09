using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.XFEM_OLD.Elements;
using ISAAR.MSolve.XFEM_OLD.Enrichments.Items;

namespace ISAAR.MSolve.XFEM_OLD.CrackGeometry
{
    interface IInteriorCrack: ISingleCrack
    {
        CrackTipEnrichments2D StartTipEnrichments { get; }
        CrackTipEnrichments2D EndTipEnrichments { get; }

        void InitializeGeometry(CartesianPoint startTip, CartesianPoint endTip);

        //TODO: remove it. It is obsolete and should be handled by ICrackGeometry.Propagate()
        void UpdateGeometry(double localGrowthAngleStart, double growthLengthStart,
            double localGrowthAngleEnd, double growthLengthEnd); // Perhaps the global angle should be passed in
    }
}
