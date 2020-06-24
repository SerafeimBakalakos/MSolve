using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.FEM.Interpolation;
using ISAAR.MSolve.Geometry.Coordinates;
using MGroup.XFEM.Entities;

namespace MGroup.XFEM.Elements
{
    public class ElementTri3Geometry : IElementGeometry
    {
        public double CalcBulkSize(IReadOnlyList<XNode> nodes)
        {
            var triangle = new Geometry.Primitives.Triangle2D();
            triangle.Vertices[0] = new double[] { nodes[0].X, nodes[0].Y};
            triangle.Vertices[1] = new double[] { nodes[1].X, nodes[1].Y };
            triangle.Vertices[2] = new double[] { nodes[2].X, nodes[2].Y };
            return triangle.CalcArea();
        }

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
