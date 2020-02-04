using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.Discretization.Mesh;
using ISAAR.MSolve.Discretization.Mesh.Generation;
using ISAAR.MSolve.Discretization.Mesh.Generation.Custom;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.Logging.VTK;
using ISAAR.MSolve.XFEM.Multiphase.Elements;
using ISAAR.MSolve.XFEM.Multiphase.Entities;
using ISAAR.MSolve.XFEM.Multiphase.Geometry;
using ISAAR.MSolve.XFEM.Multiphase.Integration;

namespace ISAAR.MSolve.XFEM.Multiphase.Plotting.Mesh
{
    public class VolumeIntegrationMesh2D : IOutputMesh<XNode>
    {
        private readonly List<VtkCell> outCells;
        private readonly List<VtkPoint> outVertices;

        public VolumeIntegrationMesh2D(XModel physicalModel, GeometricModel geometricModel)
        {
            this.outVertices = new List<VtkPoint>();
            this.outCells = new List<VtkCell>();
            int outVertexID = 0;
            foreach (IXFiniteElement element in physicalModel.Elements)
            {
                if (element.Phases.Count == 1) ProcessStandardElement(element, ref outVertexID);
                else if (element.VolumeIntegration is IntegrationWithNonConformingSubsquares2D)
                {
                    ProcessSubsquareElement(element, ref outVertexID);
                }
                else if (element.VolumeIntegration is IntegrationWithConformingSubtriangles2D triangleIntegration)
                {
                    ProcessSubtriangleElement(geometricModel, element, ref outVertexID);
                }
                else throw new NotImplementedException();
            }
        }

        public int NumOutCells => outCells.Count;
        public int NumOutVertices => outVertices.Count;
        public IEnumerable<ICell<XNode>> OriginalCells => throw new NotImplementedException();
        public IEnumerable<XNode> OriginalVertices => throw new NotImplementedException();
        public IEnumerable<VtkCell> OutCells => outCells;
        public IEnumerable<VtkPoint> OutVertices => outVertices;

        private (IReadOnlyList<XNode> vertices, IReadOnlyList<CellConnectivity<XNode>> cells) GenerateSquareIntegrationMesh(
            IXFiniteElement element, int subcellsPerAxis)
        {
            var meshGen = new UniformMeshGenerator2D<XNode>(-1, -1, 1, 1, subcellsPerAxis, subcellsPerAxis);
            return meshGen.CreateMesh((id, x, y, z) =>
            {
                var natural = new NaturalPoint(x, y);
                CartesianPoint cartesian = element.StandardInterpolation.TransformNaturalToCartesian(element.Nodes, natural);
                return new XNode(int.MaxValue, cartesian.X, cartesian.Y);
            });
        }


        private void ProcessStandardElement(IXFiniteElement element, ref int outVertexID)
        {
            var verticesOfCell = new VtkPoint[element.Nodes.Count];
            for (int i = 0; i < element.Nodes.Count; ++i)
            {
                XNode originalVertex = element.Nodes[i];
                var outVertex = new VtkPoint(outVertexID++, originalVertex.X, originalVertex.Y, originalVertex.Z);
                this.outVertices.Add(outVertex);
                verticesOfCell[i] = outVertex;
            }
            var outCell = new VtkCell(((IElementType)element).CellType, verticesOfCell);
            this.outCells.Add(outCell);
        }

        private void ProcessSubsquareElement(IXFiniteElement element, ref int outVertexID)
        {
            var squareIntegration = (IntegrationWithNonConformingSubsquares2D)(element.VolumeIntegration);
            (IReadOnlyList<XNode> vertices, IReadOnlyList<CellConnectivity<XNode>> cells) =
                        GenerateSquareIntegrationMesh(element, squareIntegration.SubcellsPerAxis);

            var originalToOutVertices = new Dictionary<XNode, VtkPoint>();
            for (int v = 0; v < vertices.Count; ++v)
            {
                var outVertex = new VtkPoint(outVertexID++, vertices[v].X, vertices[v].Y, vertices[v].Z);
                originalToOutVertices[vertices[v]] = outVertex;
                this.outVertices.Add(outVertex);
            }

            foreach (CellConnectivity<XNode> cell in cells)
            {
                VtkPoint[] cellVertices = cell.Vertices.Select(v => originalToOutVertices[v]).ToArray();
                this.outCells.Add(new VtkCell(cell.CellType, cellVertices));
            }
        }

        private void ProcessSubtriangleElement(GeometricModel geometricModel, IXFiniteElement element, ref int outVertexID)
        {
            //TODO: The resulting triangle is Tri3 only for 1st order elements. Extend this.
            Debug.Assert(element.StandardInterpolation == FEM.Interpolation.InterpolationQuad4.UniqueInstance
                    || element.StandardInterpolation == FEM.Interpolation.InterpolationTri3.UniqueInstance);

            IReadOnlyList<ElementSubtriangle> subtriangles = geometricModel.GetConformingTriangulationOf(element);
            foreach (ElementSubtriangle triangle in subtriangles)
            {
                CartesianPoint[] points = triangle.GetVerticesCartesian(element);
                var subtriangleVertices = new VtkPoint[points.Length];
                for (int i = 0; i < points.Length; ++i)
                {
                    subtriangleVertices[i] = new VtkPoint(outVertexID++, points[i].X, points[i].Y, points[i].Z);
                }
                outVertices.AddRange(subtriangleVertices);
                var outCell = new VtkCell(CellType.Tri3, subtriangleVertices);
                outCells.Add(outCell);
            }
        }
    }
}
