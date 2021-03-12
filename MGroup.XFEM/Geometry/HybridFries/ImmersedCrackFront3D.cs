using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using MGroup.XFEM.Extensions;

namespace MGroup.XFEM.Geometry.HybridFries
{
    public class ImmersedCrackFront3D : ICrackFront3D
    {
        public ImmersedCrackFront3D(CrackSurface3D crackSurface)
        {
            Edges = ExtractFrontEdges(crackSurface);

            // The vertices of the boundary are also the start vertex of each edge, since it is a closed polygon.
            Vertices = new List<Vertex3D>(Edges.Count);
            foreach (Edge3D edge in Edges) Vertices.Add(edge.Start);

            // The coordinate systems are determined by the vertices, edges and cells, without enforcing any specific movement.
            CoordinateSystems = new List<CrackFrontSystem3D>();
            for (int v = 0; v < Vertices.Count; ++v)
            {
                Vertex3D current = Vertices[v];
                Vertex3D next = Vertices[(v + 1) % Vertices.Count];
                Vertex3D previous = Vertices[v == 0 ? Vertices.Count - 1 : v - 1];

                var system = new CrackFrontSystem3D(current, previous, next);
                CoordinateSystems.Add(system);
            }
        }

        public List<Vertex3D> Vertices { get; }

        /// <summary>
        /// <see cref="Edges"/>[i] has vertices: start = <see cref="Vertices"/>[i], 
        /// end = <see cref="Vertices"/>[(i+1) % <see cref="Vertices"/>.Count]
        /// </summary>
        public List<Edge3D> Edges { get; }

        public List<CrackFrontSystem3D> CoordinateSystems { get; }

        private static List<Edge3D> ExtractFrontEdges(CrackSurface3D crackSurface)
        {
            // Find which edges belong to only 1 cell. These lie on the polyhedron boundary.
            var frontEdgesUnordered = new LinkedList<Edge3D>();
            foreach (Edge3D edge in crackSurface.Edges)
            {
                var incidentCells = new HashSet<TriangleCell3D>(edge.Start.Cells);
                incidentCells.UnionWith(edge.End.Cells);

                Debug.Assert(incidentCells.Count > 0);
                if (incidentCells.Count == 1)
                {
                    frontEdgesUnordered.AddLast(edge);
                }
            }

            // At these point, the edges have the correct orientation (same as their cell), due to the way they were originally 
            // created. However they must be placed in order.
            var frontEdges = new List<Edge3D>(frontEdgesUnordered.Count);
            frontEdges.Add(frontEdgesUnordered.First.Value); // Process the first edge, does not matter which one
            frontEdgesUnordered.RemoveFirst();
            while (frontEdges.Count > 0) // Process the remaining edges. 
            {
                // Find the edge that starts, where the last edge ended.
                Vertex3D currentEnd = frontEdges[frontEdges.Count - 1].End;
                bool exists = frontEdgesUnordered.TryExtract(edge => edge.Start == currentEnd, out Edge3D nextEdge);
                if (!exists)
                {
                    throw new ArgumentException("The boundary edges of the polyhedron do not form a closed polygon");
                }
                frontEdges.Add(nextEdge);
            }

            return frontEdges;
        }
    }
}
