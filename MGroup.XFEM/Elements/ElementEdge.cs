﻿using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Mesh;
using ISAAR.MSolve.Geometry.Coordinates;
using MGroup.XFEM.Entities;

namespace MGroup.XFEM.Elements
{
    public class ElementEdge
    {
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
