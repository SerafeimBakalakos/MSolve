using System;
using System.Collections.Generic;
using System.Text;

namespace ISAAR.MSolve.XFEM_OLD.Entities.Decomposition
{
    interface IDomainDecomposer
    {
        XCluster2D CreateSubdomains();
        void UpdateSubdomains(XCluster2D cluster);
    }
}
