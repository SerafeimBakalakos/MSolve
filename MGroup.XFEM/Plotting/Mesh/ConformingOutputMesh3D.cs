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
    public class ConformingOutputMesh3D : IOutputMesh
    {
        private readonly List<VtkCell> outCells;
        private readonly List<VtkPoint> outVertices;
        private readonly Dictionary<IXFiniteElement, HashSet<VtkCell>> original2OutCells;
        private readonly Dictionary<IXFiniteElement, HashSet<Subtetrahedron>> originalCells2Subtriangles;
        //private readonly Dictionary<XNode, HashSet<VtkPoint>> original2OutVertices;

        public ConformingOutputMesh3D(IReadOnlyList<XNode> originalVertices, IReadOnlyList<IXFiniteElement> originalCells,
            Dictionary<IXFiniteElement, ElementSubtetrahedron3D[]> triangulation)
        {
            this.outVertices = new List<VtkPoint>(originalVertices.Count);
            this.outCells = new List<VtkCell>(originalCells.Count);

            //this.original2OutVertices = new Dictionary<XNode, HashSet<VtkPoint>>();
            //foreach (XNode vertex in originalVertices) original2OutVertices[vertex] = new HashSet<VtkPoint>();
            this.original2OutCells = new Dictionary<IXFiniteElement, HashSet<VtkCell>>();
            this.originalCells2Subtriangles = new Dictionary<IXFiniteElement, HashSet<Subtetrahedron>>();

            int outVertexID = 0;
            for (int c = 0; c < originalCells.Count; ++c)
            {
                IXFiniteElement element = originalCells[c];
                var element3D = (IXFiniteElement3D)element;
                CellType cellType = ((IElementType)element).CellType;
                original2OutCells[element] = new HashSet<VtkCell>();

                bool isIntersected = triangulation.TryGetValue(element, out ElementSubtetrahedron3D[] subtetrahedra);
                if (isIntersected)
                {
                    originalCells2Subtriangles[element] = new HashSet<Subtetrahedron>();
                    foreach (ElementSubtetrahedron3D tetra in subtetrahedra)
                    {
                        VtkPoint[] subvertices = tetra.GetVerticesCartesian(element3D).
                            Select(v => new VtkPoint(outVertexID++, v.X, v.Y, v.Z)).
                            ToArray();
                        outVertices.AddRange(subvertices);

                        //TODO: The resulting triangle is Tetra4 only for 1st order elements. Extend this.
                        Debug.Assert(cellType == CellType.Tet4 || cellType == CellType.Hexa8);
                        var outCell = new VtkCell(CellType.Tet4, subvertices);
                        outCells.Add(outCell);
                        original2OutCells[element].Add(outCell);
                        originalCells2Subtriangles[element].Add(new Subtetrahedron(element, tetra, subvertices));
                    }
                }
                else
                {
                    var verticesOfCell = new VtkPoint[element.Nodes.Count];
                    for (int i = 0; i < element.Nodes.Count; ++i)
                    {
                        XNode originalVertex = element.Nodes[i];
                        var outVertex = new VtkPoint(outVertexID++, originalVertex.X, originalVertex.Y, originalVertex.Z);
                        outVertices.Add(outVertex);
                        //original2OutVertices[originalVertex].Add(outVertex);
                        verticesOfCell[i] = outVertex;
                    }
                    var outCell = new VtkCell(cellType, verticesOfCell);
                    outCells.Add(outCell);
                    original2OutCells[element].Add(outCell);
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
        public IEnumerable<Subtetrahedron> GetSubtetrahedraForOriginal(IXFiniteElement originalCell)
        {
            bool isIntersected = originalCells2Subtriangles.TryGetValue(originalCell, out HashSet<Subtetrahedron> subtriangles);
            if (isIntersected) return subtriangles;
            else return new Subtetrahedron[0];
        }

        //TODO: This is could be derived from VtkCell. Right now there is both a Subtriangle and a VtkCell and they store the 
        //      same data.
        public class Subtetrahedron
        {
            public Subtetrahedron(IXFiniteElement parentElement, ElementSubtetrahedron3D originalTriangle, 
                IReadOnlyList<VtkPoint> outVertices)
            {
                this.ParentElement = parentElement;
                this.OriginalTetra = originalTriangle;
                this.OutVertices = outVertices;
            }

            public ElementSubtetrahedron3D OriginalTetra { get; }

            /// <summary>
            /// Same order as <see cref="ElementSubtriangle.VerticesNatural"/> of <see cref="OriginalTetra"/>.
            /// </summary>
            public IReadOnlyList<VtkPoint> OutVertices { get; }

            public IXFiniteElement ParentElement { get; }
        }
    }
}
