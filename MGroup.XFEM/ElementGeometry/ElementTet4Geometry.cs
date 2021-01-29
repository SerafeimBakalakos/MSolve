using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MGroup.XFEM.Entities;
using MGroup.XFEM.Geometry;

namespace MGroup.XFEM.ElementGeometry
{
    public class ElementTet4Geometry : IElementGeometry
    {
        public double CalcBulkSizeCartesian(IReadOnlyList<XNode> nodes)
            => Utilities.CalcTetrahedronVolume(nodes.Select(n => n.Coordinates).ToArray());

        public double CalcBulkSizeNatural() => 1.0 / 6;


        public (ElementEdge[], ElementFace[]) FindEdgesFaces(IReadOnlyList<int> nodeIDs)
        {
            throw new NotImplementedException();
        }
    }
}
