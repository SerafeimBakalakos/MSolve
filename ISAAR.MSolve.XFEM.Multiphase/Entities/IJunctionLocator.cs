using System;
using System.Collections.Generic;
using System.Text;

namespace ISAAR.MSolve.XFEM_OLD.Multiphase.Entities
{
    public interface IJunctionLocator
    {
        List<PhaseJunction> FindJunctions(XModel physicalModel);
    }
}
