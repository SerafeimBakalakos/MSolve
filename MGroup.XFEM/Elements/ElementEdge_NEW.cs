using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Mesh;
using ISAAR.MSolve.Geometry.Coordinates;
using MGroup.XFEM.Entities;

namespace MGroup.XFEM.Elements
{
    public class ElementEdge_NEW
    {
        public ElementEdge_NEW(int id)
        {
            this.ID = id;
        }

        public ElementEdge_NEW(int id, IReadOnlyList<int> nodes, IReadOnlyList<double[]> nodesNatural, int start, int end)
        {
            this.ID = id;
            CellType = CellType.Line;
            NodeIDs = new int[] { nodes[start], nodes[end] };
            NodesNatural = new double[][] { nodesNatural[start], nodesNatural[end] };
        }

        public CellType CellType { get; set; }

        public int ID { get; }

        /// <summary>
        /// Their order is the same as defined in <see cref="CellType"/>.
        /// </summary>
        public int[] NodeIDs { get; set; }

        /// <summary>
        /// Their order is the same as defined in <see cref="CellType"/>.
        /// </summary>
        public IReadOnlyList<double[]> NodesNatural { get; set; }
    }
}
