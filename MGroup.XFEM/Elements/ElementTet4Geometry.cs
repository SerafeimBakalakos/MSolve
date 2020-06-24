using System;
using System.Collections.Generic;
using System.Text;
using MGroup.XFEM.Entities;
using MGroup.XFEM.Geometry.Primitives;

namespace MGroup.XFEM.Elements
{
    public class ElementTet4Geometry : IElementGeometry
    {
        public double CalcBulkSize(IReadOnlyList<XNode> nodes)
        {
            var tetra = new Tetrahedron3D();
            tetra.Vertices[0] = nodes[0].Coordinates;
            tetra.Vertices[1] = nodes[1].Coordinates;
            tetra.Vertices[2] = nodes[2].Coordinates;
            tetra.Vertices[3] = nodes[3].Coordinates;
            return tetra.CalcVolume();
        }

        public (ElementEdge[], ElementFace[]) FindEdgesFaces(IReadOnlyList<XNode> nodes)
        {
            throw new NotImplementedException();
        }
    }
}
