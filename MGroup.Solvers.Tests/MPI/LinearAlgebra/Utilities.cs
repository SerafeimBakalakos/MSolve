using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using MGroup.Solvers.MPI.Environments;
using MGroup.Solvers.MPI.LinearAlgebra;
using MGroup.Solvers.MPI.Topologies;

namespace MGroup.Solvers.Tests.MPI.LinearAlgebra
{
    public static class Utilities
    {
        public static Dictionary<ComputeNode, Vector> GlobalToLocalVectors(double[] globalVector, 
            Dictionary<ComputeNode, int[]> localToGlobalMaps)
        {
            var localVectors = new Dictionary<ComputeNode, Vector>();
            foreach (ComputeNode node in localToGlobalMaps.Keys)
            {
                int[] map = localToGlobalMaps[node];
                var localVector = Vector.CreateZero(map.Length);
                for (int i = 0; i < localVector.Length; ++i) localVector[i] = globalVector[map[i]];
                localVectors[node] = localVector;
            }
            return localVectors;
        }
    }
}
