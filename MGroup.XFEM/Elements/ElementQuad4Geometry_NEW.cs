using System;
using System.Collections.Generic;
using System.Linq;
using ISAAR.MSolve.Geometry.Coordinates;
using MGroup.XFEM.Entities;
using MGroup.XFEM.Geometry;
using MGroup.XFEM.Interpolation;

namespace MGroup.XFEM.Elements
{
    public class ElementQuad4Geometry_NEW //: IElementGeometry // MODIFICATION NEEDED: change the interface as well
    {
        // MODIFICATION NEEDED: Probably this should take coordinates as parameters. Or move the area/volume computations elsewhere
        public double CalcBulkSizeCartesian(IReadOnlyList<XNode> nodes) 
            => Utilities.CalcPolygonArea(nodes.Select(n => n.Coordinates).ToArray());

        public double CalcBulkSizeNatural() => 4.0;

        public (ElementEdge_NEW[], ElementFace[]) FindEdgesFaces(IReadOnlyList<int> nodeIDs)
        {
            IReadOnlyList<double[]> nodesNatural = InterpolationQuad4.UniqueInstance.NodalNaturalCoordinates;
            var edges = new ElementEdge_NEW[4];
            edges[0] = new ElementEdge_NEW(0, nodeIDs, nodesNatural, 0, 1);
            edges[1] = new ElementEdge_NEW(1, nodeIDs, nodesNatural, 1, 2);
            edges[2] = new ElementEdge_NEW(2, nodeIDs, nodesNatural, 2, 3);
            edges[3] = new ElementEdge_NEW(3, nodeIDs, nodesNatural, 3, 0);
            return (edges, new ElementFace[0]);
        }
    }
}
