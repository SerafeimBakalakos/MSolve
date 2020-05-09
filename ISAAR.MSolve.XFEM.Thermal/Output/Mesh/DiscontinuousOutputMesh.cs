using System.Collections.Generic;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.Discretization.Mesh;
using ISAAR.MSolve.Logging.VTK;

namespace ISAAR.MSolve.XFEM_OLD.Thermal.Output.Mesh
{
    public class DiscontinuousOutputMesh<TNode> : IOutputMesh<TNode> where TNode: INode
    {
        private readonly VtkCell[] outCells;
        private readonly List<VtkPoint> outVertices;
        private readonly Dictionary<ICell<TNode>, VtkCell> original2OutCells;
        private readonly Dictionary<TNode, HashSet<VtkPoint>> original2OutVertices;

        public DiscontinuousOutputMesh(IReadOnlyList<TNode> originalVertices, IReadOnlyList<ICell<TNode>> originalCells)
        {
            this.outVertices = new List<VtkPoint>(originalVertices.Count);
            this.outCells = new VtkCell[originalCells.Count];

            this.original2OutVertices = new Dictionary<TNode, HashSet<VtkPoint>>();
            foreach (TNode vertex in originalVertices) original2OutVertices[vertex] = new HashSet<VtkPoint>();
            this.original2OutCells = new Dictionary<ICell<TNode>, VtkCell>();

            int outVertexID = 0;
            for (int c = 0; c < originalCells.Count; ++c)
            {
                ICell<TNode> originalCell = originalCells[c];
                var cellVertices = new VtkPoint[originalCell.Nodes.Count];
                for (int i = 0; i < originalCell.Nodes.Count; ++i)
                {
                    TNode originalVertex = originalCell.Nodes[i];
                    var outVertex = new VtkPoint(outVertexID++, originalVertex.X, originalVertex.Y, originalVertex.Z);
                    outVertices.Add(outVertex);
                    original2OutVertices[originalVertex].Add(outVertex);
                    cellVertices[i] = outVertex;
                }
                var outCell = new VtkCell(originalCell.CellType, cellVertices);
                outCells[c] = outCell;
                original2OutCells[originalCell] = outCell;
            }

            this.NumOutVertices = outVertexID;
            this.NumOutCells = originalCells.Count;
        }

        public int NumOutCells { get; }

        public int NumOutVertices { get; }

        public IEnumerable<VtkCell> OutCells => outCells;

        public IEnumerable<VtkPoint> OutVertices => outVertices;

        public IEnumerable<VtkCell> GetOutCellsForOriginal(ICell<TNode> originalCell)
            => new VtkCell[] { original2OutCells[originalCell] };

        public IEnumerable<VtkPoint> GetOutVerticesForOriginal(TNode originalVertex)
            => original2OutVertices[originalVertex];
    }
}
