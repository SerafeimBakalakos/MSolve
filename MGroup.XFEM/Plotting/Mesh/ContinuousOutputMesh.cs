using System.Collections.Generic;
using System.Linq;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.Discretization.Mesh;
using ISAAR.MSolve.Logging.VTK;
using MGroup.XFEM.Entities;

namespace MGroup.XFEM.Plotting.Mesh
{
    public class ContinuousOutputMesh : IOutputMesh
    {
        private readonly List<VtkCell> outCells;
        private readonly List<VtkPoint> outVertices;

        public ContinuousOutputMesh(IEnumerable<XNode> originalVertices, IEnumerable<ICell<XNode>> originalCells)
        {
            this.OriginalVertices = originalVertices;
            this.OriginalCells = originalCells;

            var original2OutVertices = new Dictionary<XNode, VtkPoint>();

            this.outVertices = new List<VtkPoint>();
            foreach (XNode vertex in originalVertices)
            {
                var outVertex = new VtkPoint(vertex.ID, vertex.X, vertex.Y, vertex.Z);
                outVertices.Add(outVertex);
                original2OutVertices[vertex] = outVertex;
            }

            this.outCells = new List<VtkCell>();
            foreach (ICell<XNode> cell in originalCells)
            {
                List<VtkPoint> vertices = cell.Nodes.Select(v => original2OutVertices[v]).ToList();
                outCells.Add(new VtkCell(cell.CellType, vertices));
            }
        }

        public int NumOutCells => outCells.Count;

        public int NumOutVertices => outVertices.Count;

        public IEnumerable<ICell<XNode>> OriginalCells { get; }

        /// <summary>
        /// Same order as the corresponding one in <see cref="OutVertices"/>.
        /// </summary>
        public IEnumerable<XNode> OriginalVertices { get; }

        public IEnumerable<VtkCell> OutCells => outCells;

        /// <summary>
        /// Same order as the corresponding one in <see cref="OriginalVertices"/>.
        /// </summary>
        public IEnumerable<VtkPoint> OutVertices => outVertices;
    }
}
