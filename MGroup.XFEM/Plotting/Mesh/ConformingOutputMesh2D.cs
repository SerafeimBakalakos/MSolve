using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.Discretization.Mesh;
using ISAAR.MSolve.Logging.VTK;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Entities;
using MGroup.XFEM.Geometry.ConformingMesh;

//TODO: Needs tidying up.
namespace MGroup.XFEM.Plotting.Mesh
{
    public class ConformingOutputMesh2D : IOutputMesh
    {
        private readonly List<VtkCell> outCells;
        private readonly List<VtkPoint> outVertices;
        private readonly Dictionary<IXFiniteElement, HashSet<VtkCell>> original2OutCells;
        private readonly Dictionary<IXFiniteElement, HashSet<Subtriangle>> originalCells2Subtriangles;
        //private readonly Dictionary<XNode, HashSet<VtkPoint>> original2OutVertices;

        public ConformingOutputMesh2D(IReadOnlyList<XNode> originalVertices, IReadOnlyList<IXFiniteElement> originalCells,
            Dictionary<IXFiniteElement, ElementSubtriangle2D[]> triangulation)
        {
            this.outVertices = new List<VtkPoint>(originalVertices.Count);
            this.outCells = new List<VtkCell>(originalCells.Count);
            this.original2OutCells = new Dictionary<IXFiniteElement, HashSet<VtkCell>>();
            this.originalCells2Subtriangles = new Dictionary<IXFiniteElement, HashSet<Subtriangle>>();

            int outVertexID = 0;
            for (int c = 0; c < originalCells.Count; ++c)
            {
                IXFiniteElement originalCell = originalCells[c];
                CellType cellType = ((IElementType)originalCell).CellType;
                original2OutCells[originalCell] = new HashSet<VtkCell>();

                bool isIntersected = triangulation.TryGetValue(originalCell, out ElementSubtriangle2D[] subtriangles);
                if (isIntersected)
                {
                    originalCells2Subtriangles[originalCell] = new HashSet<Subtriangle>();
                    foreach (ElementSubtriangle2D triangle in subtriangles)
                    {
                        VtkPoint[] subvertices = triangle.GetVerticesCartesian(originalCell).
                            Select(v => new VtkPoint(outVertexID++, v.X, v.Y, v.Z)).
                            ToArray();
                        outVertices.AddRange(subvertices);

                        //TODO: The resulting triangle is Tri3 only for 1st order elements. Extend this.
                        Debug.Assert(cellType == CellType.Tri3 || cellType == CellType.Quad4);
                        var outCell = new VtkCell(CellType.Tri3, subvertices);
                        outCells.Add(outCell);
                        original2OutCells[originalCell].Add(outCell);
                        originalCells2Subtriangles[originalCell].Add(new Subtriangle(originalCell, triangle, subvertices));
                    }
                }
                else
                {
                    var verticesOfCell = new VtkPoint[originalCell.Nodes.Count];
                    for (int i = 0; i < originalCell.Nodes.Count; ++i)
                    {
                        XNode originalVertex = originalCell.Nodes[i];
                        var outVertex = new VtkPoint(outVertexID++, originalVertex.X, originalVertex.Y, originalVertex.Z);

                        outVertices.Add(outVertex);
                        //original2OutVertices[originalVertex].Add(outVertex);
                        verticesOfCell[i] = outVertex;
                    }
                    var outCell = new VtkCell(cellType, verticesOfCell);
                    outCells.Add(outCell);
                    original2OutCells[originalCell].Add(outCell);
                }
            }

            this.NumOutVertices = outVertices.Count;
            this.NumOutCells = outCells.Count;
        }

        public int NumOutCells { get; }

        public int NumOutVertices { get; }

        public IEnumerable<VtkCell> OutCells => outCells;

        public IEnumerable<VtkPoint> OutVertices => outVertices;

        public IEnumerable<VtkCell> GetOutCellsForOriginal(IXFiniteElement originalCell)
            => original2OutCells[originalCell];

        //public IEnumerable<VtkPoint> GetOutVerticesForOriginal(XNode originalVertex)
        //    => original2OutVertices[originalVertex];

        /// <summary>
        /// If the <paramref name="originalCell"/> is not intersected by any curve, then an empty collection will be returned.
        /// </summary>
        /// <param name="originalCell"></param>
        /// <returns></returns>
        public IEnumerable<Subtriangle> GetSubtrianglesForOriginal(IXFiniteElement originalCell)
        {
            bool isIntersected = originalCells2Subtriangles.TryGetValue(originalCell, out HashSet<Subtriangle> subtriangles);
            if (isIntersected) return subtriangles;
            else return new Subtriangle[0];
        }

        //TODO: This is could be derived from VtkCell. Right now there is both a Subtriangle and a VtkCell and they store the 
        //      same data.
        public class Subtriangle
        {
            public Subtriangle(IXFiniteElement parentElement, ElementSubtriangle2D originalTriangle, 
                IReadOnlyList<VtkPoint> outVertices)
            {
                this.OriginalTriangle = originalTriangle;
                this.OutVertices = outVertices;
                this.ParentElement = parentElement;
            }

            public ElementSubtriangle2D OriginalTriangle { get; }

            /// <summary>
            /// Same order as <see cref="ElementSubtriangle.VerticesNatural"/> of <see cref="OriginalTriangle"/>.
            /// </summary>
            public IReadOnlyList<VtkPoint> OutVertices { get; }

            public IXFiniteElement ParentElement { get; }
        }
    }
}
