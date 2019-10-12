using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.Discretization.Mesh;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.XFEM.Thermal.Entities;

namespace ISAAR.MSolve.XFEM.Thermal.Elements
{
    public interface IXFiniteElement : IElement, IElementType, ICell<XNode>
    {
        IReadOnlyList<(XNode node1, XNode node2)> EdgeNodes { get; }
        IReadOnlyList<(NaturalPoint node1, NaturalPoint node2)> EdgesNodesNatural { get; }

        IReadOnlyList<XNode> Nodes { get; }
        XSubdomain Subdomain { get; set; }
    }
}