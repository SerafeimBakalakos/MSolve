using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.FEM.Entities;
using ISAAR.MSolve.FEM.Postprocessing;
using ISAAR.MSolve.Numerical.LinearAlgebra.Interfaces;

namespace ISAAR.MSolve.Logging.VTK
{
    public class StructuralOutput
    {
        private readonly string directory;
        private readonly Model model;
        private readonly VtkPoint2D[] points;
        private readonly VtkCell2D[] cells;
        private readonly Dictionary<Node, VtkPoint2D> nodes2Points;
        private readonly Dictionary<Element, VtkCell2D> elements2Cells;

        private double[][] displacements;
        private double[][] strains;
        private double[][] stresses;

        public StructuralOutput(string directory, Model model)
        {
            this.directory = directory;
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
                int code = VtkCell2D.cellTypeCodes[elements[i].GetType()];
                var vertices = new VtkPoint2D[element.Nodes.Count];
                for (int j = 0; j < vertices.Length; ++j) vertices[j] = nodes2Points[element.Nodes[j]];
                cells[i] = new VtkCell2D(code, vertices);
                elements2Cells[element] = cells[i];
            }
        }

        public void UpdateDisplacements(IVector solution)
        {
            displacements = new double[points.Length][];
            Dictionary<Node, double[]> nodalDisplacements = (new DisplacementField2D()).FindNodalDisplacements(model, solution);
            foreach (var nodeDisplacementsPair in nodalDisplacements)
            {
                Node node = nodeDisplacementsPair.Key;
                int pointID = nodes2Points[node].ID;
                displacements[pointID] = nodeDisplacementsPair.Value;
            }
        }

        public void UpdateStrains()
        {
            throw new NotImplementedException();
        }

        public void UpdateStresses()
        {
            throw new NotImplementedException();
        }

        public void WriteMesh()
        {
            string path = directory + "\\undeformed_mesh.vtk";
            using (var writer = new VtkFileWriter(path))
            {
                writer.WriteMesh(points, cells);
            }
        }

        public void WriteDisplacementField(int iteration = -1)
        {
            var builder = new StringBuilder();
            builder.Append(directory);
            builder.Append("\\dispacements");
            if (iteration >= 0) builder.Append($"_{iteration}");
            builder.Append(".vtk");

            using (var writer = new VtkFileWriter(builder.ToString()))
            {
                writer.WriteVector2DField("displacements", displacements);
            }
        }
        
    }
}
