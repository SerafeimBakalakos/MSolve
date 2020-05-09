using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ISAAR.MSolve.XFEM_OLD.CrackGeometry.CrackTip;
using ISAAR.MSolve.Geometry.Coordinates;

namespace ISAAR.MSolve.XFEM_OLD.CrackPropagation.Jintegral
{
    public interface IAuxiliaryStates
    {
        AuxiliaryStatesTensors ComputeTensorsAt(CartesianPoint globalIntegrationPoint, 
            TipCoordinateSystem tipCoordinateSystem);
    }
}
