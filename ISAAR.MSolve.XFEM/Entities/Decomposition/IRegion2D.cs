using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.XFEM_OLD.Elements;

// TODO: Should this be here or in Geometry?
namespace ISAAR.MSolve.XFEM_OLD.Entities.Decomposition
{
    public enum NodePosition
    {
        Internal, Boundary, External
    }

    interface IRegion2D
    {
        NodePosition FindRelativePosition(XNode node);
    }
}
