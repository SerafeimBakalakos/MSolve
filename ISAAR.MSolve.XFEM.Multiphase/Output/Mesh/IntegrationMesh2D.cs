using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ISAAR.MSolve.Discretization.Mesh;
using ISAAR.MSolve.Discretization.Mesh.Generation;
using ISAAR.MSolve.Logging.VTK;
using ISAAR.MSolve.XFEM.Multiphase.Elements;
using ISAAR.MSolve.XFEM.Multiphase.Entities;
using ISAAR.MSolve.XFEM.Multiphase.Integration;

namespace ISAAR.MSolve.XFEM.Multiphase.Output.Mesh
{
    public class IntegrationMesh2D : IOutputMesh<XNode>
    {
        private readonly List<VtkCell> outCells;
        private readonly List<VtkPoint> outVertices;

        public IntegrationMesh2D(XModel model)
        {
            this.OriginalVertices = null;
            this.OriginalCells = null;

            this.outVertices = new List<VtkPoint>();
            this.outCells = new List<VtkCell>();
            int vertexID = 0;
            foreach (IXFiniteElement element in model.Elements)
            {
                var integration = (SquareSubcellIntegration2D)(element.IntegrationStrategy);
                (IReadOnlyList<XNode> vertices, IReadOnlyList<CellConnectivity<XNode>> cells) = 
                    integration.GenerateIntegrationMesh(element);

                var originalToOutVertices = new Dictionary<XNode, VtkPoint>();
                for (int v = 0; v < vertices.Count; ++v)
                {
                    var outVertex = new VtkPoint(vertexID++, vertices[v].X, vertices[v].Y, vertices[v].Z);
                    originalToOutVertices[vertices[v]] = outVertex;
                    outVertices.Add(outVertex);
                }

                foreach (CellConnectivity<XNode> cell in cells)
                {
                    VtkPoint[] cellVertices = cell.Vertices.Select(v => originalToOutVertices[v]).ToArray();
                    outCells.Add(new VtkCell(cell.CellType, cellVertices));
                }
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
