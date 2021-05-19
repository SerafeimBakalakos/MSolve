using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using MGroup.Solvers_OLD.DistributedTry1.Distributed.Environments;
using MGroup.Solvers_OLD.DistributedTry1.Distributed.LinearAlgebra;
using MGroup.Solvers_OLD.DistributedTry1.Distributed.Topologies;

namespace MGroup.Solvers_OLD.Tests.DistributedTry1.Distributed.LinearAlgebra
{
    public static class Utilities
    {
        //TODOMPI: Perhaps this should be an instance method of IComputeEnvironment, so that it can be used in model creation.
        //          Also it may be preferable to perform this in the constructor of DistributedVector, etc.
        public static void FilterNodeData<T>(IComputeEnvironment environment, Dictionary<ComputeNode, T> data)
        {
            if (environment is MpiEnvironment mpiEnvironment)
            {
                T val = data[mpiEnvironment.LocalNode];
                data.Clear();
                data[mpiEnvironment.LocalNode] = val;
            }
        }

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

        public static Dictionary<ComputeNode, Matrix> GlobalToLocalMatrices(double[,] globalMatrix,
            Dictionary<ComputeNode, int[]> localToGlobalMaps)
        {
            var localMatrices = new Dictionary<ComputeNode, Matrix>();
            foreach (ComputeNode node in localToGlobalMaps.Keys)
            {
                int[] map = localToGlobalMaps[node];
                int n = map.Length;
                var localMatrix = Matrix.CreateZero(n, n);
                for (int i = 0; i < n; ++i)
                {
                    for (int j = 0; j < n; ++j)
                    {
                        localMatrix[i, j] = globalMatrix[map[i], map[j]];
                    }
                }
                localMatrices[node] = localMatrix;
            }
            return localMatrices;
        }
    }
}
