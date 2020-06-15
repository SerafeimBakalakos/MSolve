using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;
using MGroup.XFEM.Entities;

namespace MGroup.XFEM.Elements
{
    interface IElementGeometry2D
    {
        double CalcArea(IReadOnlyList<XNode> nodes);
        ElementEdge[] FindEdges(IReadOnlyList<XNode> nodes);
    }
}
