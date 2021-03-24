using System.Collections.Generic;
using System.Linq;
using ISAAR.MSolve.Discretization.Interfaces;

//TODO: abstract this in order to be used with points in various coordinate systems
//TODO: perhaps the origin should be (0.0, 0.0) and the meshes could then be transformed. Abaqus does something similar with its
//      meshed parts during assembly
namespace ISAAR.MSolve.Discretization.Mesh.Generation.Custom
{
    /// <summary>
    /// Creates 3D meshes based on uniform rectilinear grids: the distance between two consecutive vertices for the same axis is 
    /// constant. This distance may be different for each axis though. For now the cells are hexahedral with 8 vertices. 
    /// (bricks in particular).
    /// Authors: Serafeim Bakalakos
    /// </summary>
    public class UniformMeshGenerator3D<TNode> : IMeshGenerator<TNode> where TNode: INode
    {
        private readonly double minX, minY, minZ;
        private readonly double dx, dy, dz;
        private readonly int cellsPerX, cellsPerY, cellsPerZ;
        private readonly int verticesPerX, verticesPerY, verticesPerZ;

        public UniformMeshGenerator3D(double minX, double minY, double minZ, double maxX, double maxY, double maxZ, 
            int cellsPerX, int cellsPerY, int cellsPerZ)
        {
            this.minX = minX;
            this.minY = minY;
            this.minZ = minZ;
            this.dx = (maxX - minX) / cellsPerX;
            this.dy = (maxY - minY) / cellsPerY;
            this.dz = (maxZ - minZ) / cellsPerZ;
            this.cellsPerX = cellsPerX;
            this.cellsPerY = cellsPerY;
            this.cellsPerZ = cellsPerZ;
            this.verticesPerX = cellsPerX + 1;
            this.verticesPerY = cellsPerY + 1;
            this.verticesPerZ = cellsPerZ + 1;
        }

        public bool StartIDsAt0 { get; set; } = true;

        public bool BatheOrdering { get; set; } = false;

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
            var vertices = new TNode[verticesPerX * verticesPerY * verticesPerZ];
            int id = 0;
            int start = StartIDsAt0 ? 0 : 1;
            for (int k = 0; k < verticesPerZ; ++k)
            {
                for (int j = 0; j < verticesPerY; ++j)
                {
                    for (int i = 0; i < verticesPerX; ++i)
                    {
                        vertices[id] = createNode(start + id, minX + i * dx, minY + j * dy, minZ + k * dz);
                        ++id;
                    }
                }
            }
            return vertices;
        }

        private CellConnectivity<TNode>[] CreateElements(TNode[] allVertices)
        {
            var cells = new CellConnectivity<TNode>[cellsPerX * cellsPerY * cellsPerZ];
            for (int k = 0; k < cellsPerZ; ++k)
            {
                for (int j = 0; j < cellsPerY; ++j)
                {
                    for (int i = 0; i < cellsPerX; ++i)
                    {
                        int cell = k * cellsPerX * cellsPerY + j * cellsPerX + i;
                        int firstVertex = k * verticesPerX * verticesPerY + j * verticesPerX + i;
                        var verticesOfCell = new int[]                                      // Node order for Hexa8
                        {
                            firstVertex,                                                    // (-1, -1, -1)
                            firstVertex + 1,                                                // ( 1, -1, -1)
                            firstVertex + verticesPerY + 1,                                 // ( 1,  1, -1)
                            firstVertex + verticesPerY,                                     // (-1,  1, -1)
                            firstVertex + verticesPerX * verticesPerY,                      // (-1, -1,  1)
                            firstVertex + verticesPerX * verticesPerY + 1,                  // ( 1, -1,  1)
                            firstVertex + verticesPerX * verticesPerY + verticesPerY + 1,   // ( 1,  1,  1)
                            firstVertex + verticesPerX * verticesPerY + verticesPerY        // (-1,  1,  1)
                        };
                        if (BatheOrdering)
                        {
                            verticesOfCell = ReorderHexa8NodesRegularToBathe(verticesOfCell);
                        }
                        cells[cell] = new CellConnectivity<TNode>(CellType.Hexa8, 
                            verticesOfCell.Select(idx => allVertices[idx]).ToArray()); // row major
                    }
                }
            }
            return cells;
        }

        private static int[] ReorderHexa8NodesRegularToBathe(int[] nodesRegular)
        {
            // idx Regular			    Bathe
            // 0   (-1, -1, -1)		    (+1, +1, +1)
            // 1   (+1, -1, -1)		    (-1, +1, +1)
            // 2   (+1, +1, -1)		    (-1, -1, +1)
            // 3   (-1, +1, -1)		    (+1, -1, +1)
            // 4   (-1, -1, +1)		    (+1, +1, -1)
            // 5   (+1, -1, +1)		    (-1, +1, -1)
            // 6   (+1, +1, +1)		    (-1, -1, -1)
            // 7   (-1, +1, +1)		    (+1, -1, -1)

            var nodesBathe = new int[8];
            nodesBathe[0] = nodesRegular[6];
            nodesBathe[1] = nodesRegular[7];
            nodesBathe[2] = nodesRegular[4];
            nodesBathe[3] = nodesRegular[5];
            nodesBathe[4] = nodesRegular[2];
            nodesBathe[5] = nodesRegular[3];
            nodesBathe[6] = nodesRegular[0];
            nodesBathe[7] = nodesRegular[1];

            return nodesBathe;
        }
    }
}
