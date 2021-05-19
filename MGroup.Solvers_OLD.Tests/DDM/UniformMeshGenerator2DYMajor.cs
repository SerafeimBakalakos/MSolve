using System.Collections.Generic;
using ISAAR.MSolve.Discretization.Mesh.Generation;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.Discretization.Mesh;

//TODO: abstract this in order to be used with points in various coordinate systems
//TODO: perhaps the origin should be (0.0, 0.0) and the meshes could then be transformed. Abaqus does something similar with its
//      meshed parts during assembly
namespace MGroup.Tests.DDM
{
	/// <summary>
	/// Creates 2D meshes based on uniform rectilinear grids: the distance between two consecutive vertices for the same axis is 
	/// constant. This distance may be different for each axis though. For now the cells are quadrilateral with 4 vertices 
	/// (rectangles in particular).
	/// Authors: Serafeim Bakalakos
	/// </summary>
	public class UniformMeshGenerator2DYMajor<TNode> : IMeshGenerator<TNode> where TNode : INode
	{
		private readonly double minX, minY;
		private readonly double dx, dy;
		private readonly int cellsPerX, cellsPerY;
		private readonly int verticesPerX, verticesPerY;

		public UniformMeshGenerator2DYMajor(double minX, double minY, double maxX, double maxY, int cellsPerX, int cellsPerY)
		{
			this.minX = minX;
			this.minY = minY;
			this.dx = (maxX - minX) / cellsPerX;
			this.dy = (maxY - minY) / cellsPerY;
			this.cellsPerX = cellsPerX;
			this.cellsPerY = cellsPerY;
			this.verticesPerX = this.cellsPerX + 1;
			this.verticesPerY = this.cellsPerY + 1;
		}

		/// <summary>
		/// Generates a uniform mesh with the dimensions and density defined in the constructor.
		/// </summary>
		/// <returns></returns>
		public (IReadOnlyList<TNode> nodes, IReadOnlyList<CellConnectivity<TNode>> elements)
			CreateMesh(CreateNode<TNode> createNode)
		{
			TNode[] nodes = CreateNodes(createNode);
			CellConnectivity<TNode>[] elements = CreateElements(nodes);
			return (nodes, elements);
		}

		private TNode[] CreateNodes(CreateNode<TNode> createNode)
		{
			var vertices = new TNode[verticesPerY * verticesPerX];
			int id = 0;
			for (int i = 0; i < verticesPerX; ++i)
			{
				for (int j = 0; j < verticesPerY; ++j)
				{
					vertices[id] = createNode(id, minX + i * dx, minY + j * dy, 0.0);
					++id;
				}
			}
			return vertices;
		}

		private CellConnectivity<TNode>[] CreateElements(TNode[] allVertices)
		{
			var cells = new CellConnectivity<TNode>[cellsPerY * cellsPerX];
			for (int i = 0; i < cellsPerX; ++i)
			{
				for (int j = 0; j < cellsPerY; ++j)
				{
					int cell = i * cellsPerY + j;
					int firstVertex = i * verticesPerY + j;
					TNode[] verticesOfCell =
					{
						allVertices[firstVertex], allVertices[firstVertex + verticesPerY],
						allVertices[firstVertex + verticesPerY + 1], allVertices[firstVertex + 1]
					};
					cells[cell] = new CellConnectivity<TNode>(CellType.Quad4, verticesOfCell); // row major
				}
			}
			return cells;
		}
	}
}
