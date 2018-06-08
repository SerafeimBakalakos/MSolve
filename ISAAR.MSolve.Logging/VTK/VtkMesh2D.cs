using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.FEM.Entities;
using ISAAR.MSolve.FEM.Postprocessing;

namespace ISAAR.MSolve.Logging.VTK
{
    public class VtkMesh2D
    {
        private readonly Model model;
        private readonly VtkPoint2D[] points;
        private readonly VtkCell2D[] cells;
        private readonly Dictionary<Node, VtkPoint2D> nodes2Points;
        private readonly Dictionary<Element, VtkCell2D> elements2Cells;

        public VtkMesh2D(Model model)
        {
            this.model = model;

            IList<Node> nodes = model.Nodes;
            points = new VtkPoint2D[nodes.Count];
            nodes2Points = new Dictionary<Node, VtkPoint2D>();
            for (int i = 0; i < points.Length; ++i)
            {
                Node node = nodes[i];
                points[i] = new VtkPoint2D(i, node.X, node.Y);
                nodes2Points[node] = points[i];
            }

            IList<Element> elements = model.Elements;
            cells = new VtkCell2D[elements.Count];
            elements2Cells = new Dictionary<Element, VtkCell2D>();
            for (int i = 0; i < cells.Length; ++i)
            {
                Element element = elements[i];
                int code = VtkCell2D.cellTypeCodes[elements[i].ElementType.GetType()];
                var vertices = new VtkPoint2D[element.Nodes.Count];
                for (int j = 0; j < vertices.Length; ++j) vertices[j] = nodes2Points[element.Nodes[j]];
                cells[i] = new VtkCell2D(code, vertices);
                elements2Cells[element] = cells[i];
            }
        }

        public IReadOnlyList<VtkPoint2D> Points { get { return points; } }
        public IReadOnlyList<VtkCell2D> Cells { get { return cells; } }
        public IReadOnlyDictionary<Node, VtkPoint2D> Nodes2Points { get { return nodes2Points; } }
        public IReadOnlyDictionary<Element, VtkCell2D> Elements2Celss { get { return elements2Cells; } }
    }
}
