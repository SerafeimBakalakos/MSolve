using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.FEM.Interpolation;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.Geometry.Shapes;
using MGroup.XFEM.Entities;

namespace MGroup.XFEM.Elements
{
    public class ElementQuad4Geometry : IElementGeometry2D
    {
        public double CalcArea(IReadOnlyList<XNode> nodes)
        {
            return ConvexPolygon2D.CreateUnsafe(nodes).ComputeArea();
        }

        public ElementEdge[] FindEdges(IReadOnlyList<XNode> nodes)
        {
            IReadOnlyList<NaturalPoint> nodesNatural = InterpolationQuad4.UniqueInstance.NodalNaturalCoordinates;
            var edges = new ElementEdge[4];
            edges[0] = new ElementEdge(nodes, nodesNatural, 0, 1);
            edges[1] = new ElementEdge(nodes, nodesNatural, 1, 2);
            edges[2] = new ElementEdge(nodes, nodesNatural, 2, 3);
            edges[3] = new ElementEdge(nodes, nodesNatural, 3, 0);
            return edges;
        }
    }
}
