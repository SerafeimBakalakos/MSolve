using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Mesh;
using ISAAR.MSolve.Geometry.Coordinates;
using MGroup.XFEM.Entities;

namespace MGroup.XFEM.Elements
{
    public class ElementEdge
    {
        public ElementEdge()
        {
        }

        public ElementEdge(IReadOnlyList<XNode> nodes, IReadOnlyList<NaturalPoint> nodesNatural, int start, int end)
        {
            CellType = CellType.Line;
            Nodes = new XNode[] { nodes[start], nodes[end] };
            NodesNatural = new NaturalPoint[] { nodesNatural[start], nodesNatural[end] };
        }

        public CellType CellType { get; set; }

        /// <summary>
        /// Their order is the same as defined in <see cref="CellType"/>.
        /// </summary>
        public XNode[] Nodes { get; set; }

        /// <summary>
        /// Their order is the same as defined in <see cref="CellType"/>.
        /// </summary>
        public NaturalPoint[] NodesNatural { get; set; }
    }
}
