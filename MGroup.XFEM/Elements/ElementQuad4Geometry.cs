using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.FEM.Interpolation;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.Geometry.Shapes;
using MGroup.XFEM.Entities;

namespace MGroup.XFEM.Elements
{
    public class ElementQuad4Geometry : IElementGeometry
    {
        public double CalcBulkSize(IReadOnlyList<XNode> nodes)
        {
            double area = 0.0;
            for (int vertexIdx = 0; vertexIdx < nodes.Count; ++vertexIdx)
            {
                double[] vertex1 = nodes[vertexIdx].Coordinates;
                double[] vertex2 = nodes[(vertexIdx + 1) % nodes.Count].Coordinates;
                area += vertex1[0] * vertex2[1] - vertex2[0] * vertex1[1];
            }
            return Math.Abs(0.5 * area); // area would be negative if vertices were in counter-clockwise order
        }

        public (ElementEdge[], ElementFace[]) FindEdgesFaces(IReadOnlyList<XNode> nodes)
        {
            IReadOnlyList<NaturalPoint> nodesNatural = InterpolationQuad4.UniqueInstance.NodalNaturalCoordinates;
            var edges = new ElementEdge[4];
            edges[0] = new ElementEdge(nodes, nodesNatural, 0, 1);
            edges[1] = new ElementEdge(nodes, nodesNatural, 1, 2);
            edges[2] = new ElementEdge(nodes, nodesNatural, 2, 3);
            edges[3] = new ElementEdge(nodes, nodesNatural, 3, 0);
            return (edges, new ElementFace[0]);
        }
    }
}
