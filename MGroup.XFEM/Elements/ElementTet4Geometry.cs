using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MGroup.XFEM.Entities;
using MGroup.XFEM.Geometry;
using MGroup.XFEM.Geometry.Primitives;

namespace MGroup.XFEM.Elements
{
    public class ElementTet4Geometry : IElementGeometry
    {
        public double CalcBulkSize(IReadOnlyList<XNode> nodes)
            => Utilities.CalcTetrahedronVolume(nodes.Select(n => n.Coordinates).ToArray());

        public (ElementEdge[], ElementFace[]) FindEdgesFaces(IReadOnlyList<XNode> nodes)
        {
            throw new NotImplementedException();
        }
    }
}
