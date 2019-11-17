using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.Discretization.Mesh;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.Logging.VTK;
using ISAAR.MSolve.XFEM.Thermal.Elements;
using ISAAR.MSolve.XFEM.Thermal.Entities;
using ISAAR.MSolve.XFEM.Thermal.LevelSetMethod;
using ISAAR.MSolve.XFEM.Thermal.LevelSetMethod.MeshInteraction;

//TODO: Needs tidying up.
namespace ISAAR.MSolve.XFEM.Thermal.Output.Mesh
{
    public class ConformingOutputMesh2D : IOutputMesh<XNode>
    {
        private readonly List<VtkCell> outCells;
        private readonly List<VtkPoint> outVertices;
        private readonly Dictionary<IXFiniteElement, HashSet<VtkCell>> original2OutCells;
        private readonly Dictionary<IXFiniteElement, HashSet<Subtriangle>> originalCells2Subtriangles;
        //private readonly Dictionary<XNode, HashSet<VtkPoint>> original2OutVertices;

        public ConformingOutputMesh2D(GeometricModel2D geometricModel, 
            IReadOnlyList<XNode> originalVertices, IReadOnlyList<IXFiniteElement> originalCells)
        {
            this.outVertices = new List<VtkPoint>(originalVertices.Count);
            this.outCells = new List<VtkCell>(originalCells.Count);

            //this.original2OutVertices = new Dictionary<XNode, HashSet<VtkPoint>>();
            //foreach (XNode vertex in originalVertices) original2OutVertices[vertex] = new HashSet<VtkPoint>();
            this.original2OutCells = new Dictionary<IXFiniteElement, HashSet<VtkCell>>();
            this.originalCells2Subtriangles = new Dictionary<IXFiniteElement, HashSet<Subtriangle>>();
            
            int outVertexID = 0;
            for (int c = 0; c < originalCells.Count; ++c)
            {
                IXFiniteElement originalCell = originalCells[c];
                original2OutCells[originalCell] = new HashSet<VtkCell>();

                bool success = geometricModel.TryConformingTriangulation(originalCell, 
                    out IReadOnlyList<ElementSubtriangle> subtriangles);
                if (success)
                {
                    originalCells2Subtriangles[originalCell] = new HashSet<Subtriangle>();
                    foreach (ElementSubtriangle triangle in subtriangles)
                    {
                        VtkPoint[] subvertices = triangle.GetVerticesCartesian(originalCell).
                            Select(v => new VtkPoint(outVertexID++, v.X, v.Y, v.Z)).
                            ToArray();
                        outVertices.AddRange(subvertices);

                        //TODO: The resulting triangle is Tri3 only for 1st order elements. Extend this.
                        Debug.Assert(originalCell.StandardInterpolation == FEM.Interpolation.InterpolationQuad4.UniqueInstance
                            || originalCell.StandardInterpolation == FEM.Interpolation.InterpolationTri3.UniqueInstance);
                        var outCell = new VtkCell(CellType.Tri3, subvertices);
                        outCells.Add(outCell);
                        original2OutCells[originalCell].Add(outCell);

                        originalCells2Subtriangles[originalCell].Add(new Subtriangle(triangle, subvertices));
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
                    var outCell = new VtkCell(((IElementType)originalCell).CellType, verticesOfCell);
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

        public IEnumerable<Subtriangle> GetSubtrianglesForOriginal(IXFiniteElement originalCell)
            => originalCells2Subtriangles[originalCell];

        //TODO: This is could be derived from VtkCell. Right now there is both a Subtriangle and a VtkCell and they store the 
        //      same data.
        public class Subtriangle
        {
            public Subtriangle(ElementSubtriangle originalTriangle, IReadOnlyList<VtkPoint> outVertices)
            {
                this.OriginalTriangle = originalTriangle;
                this.OutVertices = outVertices;
            }

            public ElementSubtriangle OriginalTriangle { get; }

            /// <summary>
            /// Same order as <see cref="ElementSubtriangle.VerticesNatural"/> of <see cref="OriginalTriangle"/>.
            /// </summary>
            public IReadOnlyList<VtkPoint> OutVertices { get; }
        }
    }
}
