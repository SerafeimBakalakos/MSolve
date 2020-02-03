using System.Collections.Generic;
using System.Linq;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.Discretization.Mesh;
using ISAAR.MSolve.Logging.VTK;

namespace ISAAR.MSolve.XFEM.Multiphase.Plotting.Mesh
{
    public class ContinuousOutputMesh<TNode> : IOutputMesh<TNode> where TNode : INode
    {
        private readonly List<VtkCell> outCells;
        private readonly List<VtkPoint> outVertices;

        public ContinuousOutputMesh(IEnumerable<TNode> originalVertices, IEnumerable<ICell<TNode>> originalCells)
        {
            this.OriginalVertices = originalVertices;
            this.OriginalCells = originalCells;

            var original2OutVertices = new Dictionary<TNode, VtkPoint>();

            this.outVertices = new List<VtkPoint>();
            foreach (TNode vertex in originalVertices)
            {
                var outVertex = new VtkPoint(vertex.ID, vertex.X, vertex.Y, vertex.Z);
                outVertices.Add(outVertex);
                original2OutVertices[vertex] = outVertex;
            }

            this.outCells = new List<VtkCell>();
            foreach (ICell<TNode> cell in originalCells)
            {
                List<VtkPoint> vertices = cell.Nodes.Select(v => original2OutVertices[v]).ToList();
                outCells.Add(new VtkCell(cell.CellType, vertices));
            }
        }

        public int NumOutCells => outCells.Count;

        public int NumOutVertices => outVertices.Count;

        public IEnumerable<ICell<TNode>> OriginalCells { get; }

        /// <summary>
        /// Same order as the corresponding one in <see cref="OutVertices"/>.
        /// </summary>
        public IEnumerable<TNode> OriginalVertices { get; }

        public IEnumerable<VtkCell> OutCells => outCells;

        /// <summary>
        /// Same order as the corresponding one in <see cref="OriginalVertices"/>.
        /// </summary>
        public IEnumerable<VtkPoint> OutVertices => outVertices;
    }
}
