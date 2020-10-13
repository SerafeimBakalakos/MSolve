using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace MGroup.XFEM.Geometry.Mesh
{
    public class LsmMesh2D : LsmMeshBase
    {
        public LsmMesh2D(double[] minCoordinates, double[] maxCoordinates, int[] numElementsLsm, int[] numElementsFem) 
            : base(2, new UniformMesh2D(minCoordinates, maxCoordinates, numElementsFem),
                  new UniformMesh2D(minCoordinates, maxCoordinates, numElementsLsm))
        {
            ElementNeighbors = FindElementNeighbors(base.multiple);
        }

        protected override List<int[]> ElementNeighbors { get; }

        private List<int[]> FindElementNeighbors(int[] multiple)
        {
            var elementNeighbors = new List<int[]>();
            for (int j = 0; j < multiple[1]; ++j)
            {
                for (int i = 0; i < multiple[0]; ++i)
                {
                    // Offset from the LSM element that has the same first node as the FEM element
                    int[] offset = { i, j };
                    elementNeighbors.Add(offset);
                }
            }
            return elementNeighbors;
        }
    }
}
