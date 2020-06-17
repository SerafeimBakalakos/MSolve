using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;
using MGroup.XFEM.Entities;

namespace MGroup.XFEM.Elements
{
    public interface IElementGeometry3D
    {
        double CalcVolume(IReadOnlyList<XNode> nodes);

        (ElementEdge[], ElementFace[]) FindEdgesFaces(IReadOnlyList<XNode> nodes);
    }
}
