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
        public VoronoiDiagram2D ReadMatlabVoronoiDiagram(string pathSeeds, string pathVertices, string pathCells)
        {
            // Seeds
            var seeds = new List<CartesianPoint>();
            using (var reader = new StreamReader(pathSeeds))
            {
                string line = null;
                while ((line = reader.ReadLine()) != null)
                {
                    string[] words = line.Split(',');
                    Debug.Assert(words.Length == 2);
                    double x = double.Parse(words[0]);
                    double y = double.Parse(words[1]);
                    seeds.Add(new CartesianPoint(x, y));
                }
            }

            // Vertices
            var vertices = new List<CartesianPoint>();
            using (var reader = new StreamReader(pathVertices))
            {
                string line = reader.ReadLine(); // The first line contains infinity
                vertices.Add(new CartesianPoint(double.PositiveInfinity, double.PositiveInfinity));
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
                    var cell = new int[words.Length];
                    for (int i = 0; i < words.Length; ++i)
                    {
                        int index = int.Parse(words[i]);
                        Debug.Assert(index > 0);
                        cell[i] = index - 1;
                    }
                    cells.Add(cell);
                }
            }

            return new VoronoiDiagram2D(seeds, vertices, cells);
        }
    }
}
