using System.Collections.Generic;
using System.Linq;
using ISAAR.MSolve.Geometry.Coordinates;
using MGroup.XFEM.Entities;
using MGroup.XFEM.Geometry;
using MGroup.XFEM.Interpolation;

namespace MGroup.XFEM.Elements
{
    public class ElementTri3Geometry : IElementGeometry
    {
        public double CalcBulkSize(IReadOnlyList<XNode> nodes) 
            => Utilities.CalcPolygonArea(nodes.Select(n => n.Coordinates).ToArray());

        public (ElementEdge[], ElementFace[]) FindEdgesFaces(IReadOnlyList<XNode> nodes)
        {
            IReadOnlyList<double[]> nodesNatural = InterpolationTri3.UniqueInstance.NodalNaturalCoordinates;
            var edges = new ElementEdge[3];
            edges[0] = new ElementEdge(0, nodes, nodesNatural, 0, 1);
            edges[1] = new ElementEdge(1, nodes, nodesNatural, 1, 2);
            edges[2] = new ElementEdge(2, nodes, nodesNatural, 2, 0);
            return (edges, new ElementFace[0]);
        }
    }
}
