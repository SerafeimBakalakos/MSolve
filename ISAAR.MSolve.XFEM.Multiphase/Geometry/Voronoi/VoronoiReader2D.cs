using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using ISAAR.MSolve.Geometry.Coordinates;

namespace ISAAR.MSolve.XFEM_OLD.Multiphase.Geometry.Voronoi
{
    public class VoronoiReader2D
    {
        public VoronoiDiagram2D ReadMatlabVoronoiDiagram(string pathVertices, string pathCells)
        {
            // Vertices
            var vertices = new List<CartesianPoint>();
            using (var reader = new StreamReader(pathVertices))
            {
                string line = reader.ReadLine(); // Discard the first line, since it contains infinity
                while ((line = reader.ReadLine()) != null)
                {
                    string[] words = line.Split(',');
                    Debug.Assert(words.Length == 2);
                    double x = double.Parse(words[0]);
                    double y = double.Parse(words[1]);
                    vertices.Add(new CartesianPoint(x, y));
                }
            }

            // Cells
            var cells = new List<int[]>();
            using (var reader = new StreamReader(pathCells))
            {
                string line = null;
                while ((line = reader.ReadLine()) != null)
                {
                    string[] words = line.Trim(' ').Split(' ');
                    Debug.Assert(words.Length > 2);
                    bool hasInfinityVertex = false;
                    var cell = new int[words.Length];
                    for (int i = 0; i < words.Length; ++i)
                    {
                        int index = int.Parse(words[i]);
                        if (index >= 2)
                        {
                            cell[i] = index - 2;
                        }
                        else
                        {
                            hasInfinityVertex = true;
                            break;
                        }
                    }
                    if (!hasInfinityVertex)
                    {
                        cells.Add(cell);
                    }
                }
            }

            return new VoronoiDiagram2D(vertices, cells);
        }
    }
}
