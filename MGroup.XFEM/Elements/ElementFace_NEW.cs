using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Mesh;
using ISAAR.MSolve.Geometry.Coordinates;
using MGroup.XFEM.Entities;

namespace MGroup.XFEM.Elements
{
    public class ElementFace_NEW
    {
        public int ID { get; set; }

        public CellType CellType { get; set; }

        /// <summary>
        /// Their order is the same as defined in <see cref="CellType"/>.
        /// </summary>
        public int[] NodeIDs { get; set; }

        /// <summary>
        /// Their order is the same as defined in <see cref="CellType"/>.
        /// </summary>
        public IReadOnlyList<double[]> NodesNatural { get; set; }

        public ElementEdge_NEW[] Edges { get; set; }
    }
}
