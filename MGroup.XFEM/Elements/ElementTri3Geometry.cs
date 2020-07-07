using System.Collections.Generic;
using System.Linq;
using ISAAR.MSolve.FEM.Interpolation;
using ISAAR.MSolve.Geometry.Coordinates;
using MGroup.XFEM.Entities;
using MGroup.XFEM.Geometry;

namespace MGroup.XFEM.Elements
{
    public class ElementTri3Geometry : IElementGeometry
    {
        public double CalcBulkSize(IReadOnlyList<XNode> nodes) 
            => Utilities.CalcPolygonArea(nodes.Select(n => n.Coordinates).ToArray());

        public (ElementEdge[], ElementFace[]) FindEdgesFaces(IReadOnlyList<XNode> nodes)
        {
            IReadOnlyList<NaturalPoint> nodesNatural = InterpolationTri3.UniqueInstance.NodalNaturalCoordinates;
            var edges = new ElementEdge[3];
            edges[0] = new ElementEdge(nodes, nodesNatural, 0, 1);
            edges[1] = new ElementEdge(nodes, nodesNatural, 1, 2);
            edges[2] = new ElementEdge(nodes, nodesNatural, 2, 0);
            return (edges, new ElementFace[0]);
        }
    }
}
