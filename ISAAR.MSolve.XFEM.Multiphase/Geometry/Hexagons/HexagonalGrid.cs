using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using ISAAR.MSolve.Geometry.Coordinates;

namespace ISAAR.MSolve.XFEM_OLD.Multiphase.Geometry.Hexagons
{
    public class HexagonalGrid
    {
        public const int externalSpaceID = int.MinValue;

        private readonly double hexSize, hexHeight, hexWidth;
        private readonly int numHexagonsX, numHexagonsY;
        private readonly int numVerticesX, numVerticesY;
        private readonly double minX, maxY;

        public HexagonalGrid(double hexagonSize, int numHexagonsX, int numHexagonsY, double minX, double maxY)
        {
            this.hexSize = hexagonSize;
            this.numHexagonsX = numHexagonsX;
            this.numHexagonsY = numHexagonsY;
            this.minX = minX;
            this.maxY = maxY;

            this.numVerticesX = numHexagonsX + 1;
            this.numVerticesY = 2 + 2 * numHexagonsY;
            this.hexHeight = 2.0 * hexSize;
            this.hexWidth = Math.Sqrt(3) * hexSize;

            CreateVertices();
            CreateCells();
            CreateEdges();
        }

        public List<Edge> Edges { get; private set; }

        public List<int[]> Cells { get; private set; }

        public List<CartesianPoint> Vertices { get; private set; }

        private void CreateCells()
        {
            Cells = new List<int[]>(numHexagonsX * numHexagonsY);
            for (int j = 0; j < numHexagonsY; ++j)
            {
                // The rows of vertices where the vertices of this cell lie
                int[] vertexRows = { 2 * j, 2 * j + 1, 2 * j + 2, 2 * j + 3 }; // top-to-bottom

                // The top & bottom of some cells start from the first vertex of the corresponding vertex rows
                int topOffset = (j % 2 == 0) ? 0 : 1;

                for (int i = 0; i < numHexagonsX; ++i)
                {
                    var connectivity = new int[6];
                    connectivity[0] = vertexRows[0] * numVerticesX + topOffset + i;    // top vertex
                    connectivity[1] = vertexRows[1] * numVerticesX + i;                // upper left vertex
                    connectivity[2] = vertexRows[2] * numVerticesX + i;                // lower left vertex
                    connectivity[3] = vertexRows[3] * numVerticesX + topOffset + i;    // bottom vertex
                    connectivity[4] = vertexRows[2] * numVerticesX + i + 1;            // lower right vertex
                    connectivity[5] = vertexRows[1] * numVerticesX + i + 1;            // upper right vertex
                    Cells.Add(connectivity);
                }
            }
        }

        private void CreateEdges()
        {
            Edges = new List<Edge>();

            // First 2 rows of vertices
            {
                // Edges with orientation: /
                for (int i = 0; i < numVerticesX; ++i)
                {
                    var edge = new Edge();
                    edge.Vertices = new int[] { numVerticesX + i, i };
                    edge.CellPositive = externalSpaceID;
                    if (i == numVerticesX - 1) edge.CellNegative = externalSpaceID;
                    else edge.CellNegative = i;
                    Edges.Add(edge);
                }

                // Edges with orientation: \
                for (int i = 0; i < numHexagonsX; ++i)
                {
                    var edge = new Edge();
                    edge.Vertices = new int[] { i, numVerticesX + i + 1 };

                    // No need to check external space, since these are all internal edges
                    edge.CellPositive = externalSpaceID;
                    edge.CellNegative = i;
                    Edges.Add(edge);
                }
            }

            // Rest: for each row of hexagons process its bottom 2 vertex rows
            for (int j = 0; j < numHexagonsY; ++j)
            {
                int[] vertexRows = { 2 * j, 2 * j + 1, 2 * j + 2, 2 * j + 3 }; // top-to-bottom

                // Edges with orientation: |
                for (int i = 0; i < numVerticesX; ++i)
                {
                    var edge = new Edge();
                    edge.Vertices = new int[] { numVerticesX * vertexRows[2] + i, numVerticesX * vertexRows[1] + i };
                    if (i == 0) edge.CellPositive = externalSpaceID;
                    else edge.CellPositive = numHexagonsX * j + i - 1;
                    if (i == numVerticesX - 1) edge.CellNegative = externalSpaceID;
                    else edge.CellNegative = numHexagonsX * j + i;
                    Edges.Add(edge);
                }

                if (j % 2 == 0)
                {
                    // Edges with orientation: \
                    for (int i = 0; i < numVerticesX; ++i)
                    {
                        var edge = new Edge();
                        edge.Vertices = new int[] { vertexRows[2] * numVerticesX + i, vertexRows[3] * numVerticesX + i };
                        if (i == 0) edge.CellNegative = externalSpaceID; 
                        else edge.CellNegative = numHexagonsX * (j + 1) + i - 1; 
                        if (i == numVerticesX - 1) edge.CellPositive = externalSpaceID;
                        else edge.CellPositive = numHexagonsX * j + i;
                        Edges.Add(edge);
                    }

                    // Edges with orientation: /
                    for (int i = 0; i < numHexagonsX; ++i)
                    {
                        var edge = new Edge();
                        edge.Vertices = new int[] { vertexRows[3] * numVerticesX + i, vertexRows[2] * numVerticesX + i + 1 };

                        // No need to check external space, since these are all internal edges
                        edge.CellPositive = numHexagonsX * j + i;
                        edge.CellNegative = numHexagonsX * (j + 1) + i;
                        Edges.Add(edge);
                    }
                }
                else
                {
                    // Edges with orientation: /
                    for (int i = 0; i < numVerticesX; ++i)
                    {
                        var edge = new Edge();
                        edge.Vertices = new int[] { vertexRows[3] * numVerticesX + i, vertexRows[2] * numVerticesX + i };
                        if (i == 0) edge.CellPositive = externalSpaceID;
                        else edge.CellPositive = numHexagonsX * j + i - 1;
                        if (i == numVerticesX - 1) edge.CellNegative = externalSpaceID;
                        else edge.CellNegative = numHexagonsX * (j + 1) + i;
                        Edges.Add(edge);
                    }

                    // Edges with orientation: \
                    for (int i = 0; i < numHexagonsX; ++i)
                    {
                        var edge = new Edge();
                        edge.Vertices = new int[] { vertexRows[2] * numVerticesX + i, vertexRows[3] * numVerticesX + i + 1 };

                        // No need to check external space, since these are all internal edges
                        edge.CellPositive = numHexagonsX * j + i;
                        edge.CellNegative = numHexagonsX * (j + 1) + i;
                        Edges.Add(edge);
                    }
                }
            }

            int numHexagonsTotal = numHexagonsX * numHexagonsY;
            var edgesToRemove = new HashSet<Edge>();
            for (int e = 0; e < Edges.Count; ++e)
            {
                Edge edge = Edges[e];
                if ((edge.CellPositive < 0) || (edge.CellPositive >= numHexagonsTotal)) edge.CellPositive = externalSpaceID;
                if ((edge.CellNegative < 0) || (edge.CellNegative >= numHexagonsTotal)) edge.CellNegative = externalSpaceID;
                if ((edge.CellPositive == externalSpaceID) && (edge.CellNegative == externalSpaceID)) edgesToRemove.Add(edge);
            }
            Edges.RemoveAll(edge => edgesToRemove.Contains(edge));
        }

        private void CreateVertices()
        {
            Vertices = new List<CartesianPoint>(numVerticesX * numVerticesY);

            for (int j = 0; j < numVerticesY; ++j)
            {
                int rowType = j % 4;
                double offsetX, offsetY;
                if (rowType == 0)
                {
                    offsetX = 0.5 * hexWidth;
                    offsetY = 0.0;
                }
                else if (rowType == 1)
                {
                    offsetX = 0.0;
                    offsetY = 0.5 * hexSize;
                }
                else if (rowType == 2)
                {
                    offsetX = 0.0;
                    offsetY = 1.5 * hexSize;
                }
                else /*(rowType == 3)*/
                {
                    offsetX = 0.5 * hexWidth;
                    offsetY = 2.0 * hexSize;
                }

                double y = maxY - offsetY - 3 * hexSize * (j / 4);
                for (int i = 0; i < numVerticesX; ++i)
                {
                    double x = minX + offsetX + hexWidth * i;
                    Vertices.Add(new CartesianPoint(x, y));
                }
            }
            Debug.Assert(Vertices.Count == numVerticesX * numVerticesY);
        }

        public class Edge
        {
            public int[] Vertices { get; set; }
            public int CellNegative { get; set; }
            public int CellPositive { get; set; }
        }
    }
}
