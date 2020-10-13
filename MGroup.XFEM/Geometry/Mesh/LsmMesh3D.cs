using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace MGroup.XFEM.Geometry.Mesh
{
    public class LsmMesh3D : LsmMeshBase
    {
        public LsmMesh3D(double[] minCoordinates, double[] maxCoordinates, int[] numElementsLsm, int[] numElementsFem) 
            : base(3, new UniformMesh3D(minCoordinates, maxCoordinates, numElementsFem),
                  new UniformMesh3D(minCoordinates, maxCoordinates, numElementsLsm))
        {
            ElementNeighbors = FindElementNeighbors(base.multiple);
        }

        protected override List<int[]> ElementNeighbors { get; }

        private List<int[]> FindElementNeighbors(int[] multiple)
        {
            var elementNeighbors = new List<int[]>();
            for (int k = 0; k < multiple[2]; ++k)
            {
                for (int j = 0; j < multiple[1]; ++j)
                {
                    for (int i = 0; i < multiple[0]; ++i)
                    {
                        // Offset from the lsm element that has the same first node as the FEM element
                        int[] offset = { i, j, k };
                        elementNeighbors.Add(offset);
                    }
                }
            }
            return elementNeighbors;
        }
    }
}
