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
        public ElementEdge(int id)
        {
            this.ID = id;
        }

        public ElementEdge(int id, IReadOnlyList<XNode> nodes, IReadOnlyList<double[]> nodesNatural, int start, int end)
        {
            this.ID = id;
            CellType = CellType.Line;
            Nodes = new XNode[] { nodes[start], nodes[end] };
            NodesNatural = nodesNatural;
        }

        public CellType CellType { get; set; }

        public int ID { get; }

        /// <summary>
        /// Their order is the same as defined in <see cref="CellType"/>.
        /// </summary>
        public XNode[] Nodes { get; set; }

        /// <summary>
        /// Their order is the same as defined in <see cref="CellType"/>.
        /// </summary>
        public IReadOnlyList<double[]> NodesNatural { get; set; }
    }
}
