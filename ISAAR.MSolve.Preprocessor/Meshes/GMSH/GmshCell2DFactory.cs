﻿using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.FEM.Entities;
using ISAAR.MSolve.Geometry.Shapes;

namespace ISAAR.MSolve.Preprocessor.Meshes.GMSH
{
    /// <summary>
    /// Converts cell types and the order of their vertices from GMSH to MSolve.
    /// Authors: Serafeim Bakalakos
    /// </summary>
    class GmshCell2DFactory
    {
        private static readonly IReadOnlyDictionary<int, CellType2D> gmshCellCodes;

        // Vertex order for cells. Index = gmsh order, value = MSolve order.
        private static readonly IReadOnlyDictionary<CellType2D, int[]> gmshCellConnectivity;

        static GmshCell2DFactory()
        {
            var codes = new Dictionary<int, CellType2D>();
            codes.Add(2, CellType2D.Tri3);
            codes.Add(3, CellType2D.Quad4);
            codes.Add(9, CellType2D.Tri6);
            codes.Add(10, CellType2D.Quad9);
            codes.Add(16, CellType2D.Quad8);
            gmshCellCodes = codes;

            var connectivity = new Dictionary<CellType2D, int[]>();
            connectivity.Add(CellType2D.Tri3, new int[] { 0, 1, 2 });
            connectivity.Add(CellType2D.Quad4, new int[] { 0, 1, 2, 3 });
            connectivity.Add(CellType2D.Tri6, new int[] { 0, 1, 2, 3, 4, 5 });
            connectivity.Add(CellType2D.Quad9, new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8 });
            connectivity.Add(CellType2D.Quad8, new int[] { 0, 1, 2, 3, 4, 5, 6, 7 });
            gmshCellConnectivity = connectivity;
        }

        private readonly IReadOnlyList<Node2D> allVertices;

        public GmshCell2DFactory(IReadOnlyList<Node2D> allVertices)
        {
            this.allVertices = allVertices;
        }

        /// <summary>
        /// Returns true and a <see cref="CellConnectivity2D"/> if the <paramref name="cellCode"/> corresponds to a valid 
        /// MSolve <see cref="CellType2D"/>. 
        /// Otherwise returns false and null.
        /// </summary>
        /// <param name="cellCode"></param>
        /// <param name="vertexIDs"> These must be 0-based</param>
        /// <param name="cell"></param>
        /// <returns></returns>
        public bool TryCreateCell(int cellCode, int[] vertexIDs, out CellConnectivity2D cell)
        {
            bool validCell = gmshCellCodes.TryGetValue(cellCode, out CellType2D type);
            if (validCell)
            {
                Node2D[] cellVertices = new Node2D[vertexIDs.Length];
                for (int i = 0; i < vertexIDs.Length; ++i)
                {
                    int msolveIndex = gmshCellConnectivity[type][i];
                    cellVertices[msolveIndex] = allVertices[vertexIDs[i]];
                }
                FixVerticesOrder(cellVertices);
                cell = new CellConnectivity2D(type, cellVertices);
                return true;
            }
            else
            {
                cell = null;
                return false;
            }
        }

        /// <summary>
        /// If the order is clockwise, it is reversed. Not sure if it sufficient or required for second order elements.
        /// </summary>
        /// <param name="cellVertices"></param>
        private void FixVerticesOrder(Node2D[] cellVertices)
        {
            // The area of the cell with clockwise vertices is negative!
            double cellArea = 0.0; // Actually double the area will be computed, but we only care about the sign here
            for (int i = 0; i < cellVertices.Length; ++i)
            {
                Node2D vertex1 = cellVertices[i];
                Node2D vertex2 = cellVertices[(i + 1) % cellVertices.Length];
                cellArea += vertex1.X * vertex2.Y - vertex2.X * vertex1.Y;
            }
            if (cellArea < 0) Array.Reverse(cellVertices);
            return;
        }
    }
}
