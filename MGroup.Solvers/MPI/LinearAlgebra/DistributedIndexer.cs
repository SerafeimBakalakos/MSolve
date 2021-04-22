using System;
using System.Collections.Generic;
using System.Text;
using MGroup.Solvers.MPI.Environment;

namespace MGroup.Solvers.MPI.LinearAlgebra
{
    //TODOMPI: the counts array for AllToAll can be cached by the indexer. Also a mapping array can be cached as well to 
    //      simplify the nested for loops required to copy from localVector to sendValues and from recvValues to localVector. 
    //TODOMPI: Instead of lists, I should use dictionaries with ComputeNode as keys.
    public class DistributedIndexer
    {
        //TODOMPI: It would be easier if this could be inferred from NeighborCommonEntries
        public List<int[]> BoundaryEntries { get; set; }

        public int[] InternalEntries { get; set; }

        public ComputeNode ComputeNode { get; set; }

        public List<int[]> NeighborCommonEntries { get; set; }

        //TODO: cache a buffer for sending and a buffer for receiving inside Indexer (lazily or not) and just return them.
        public double[] CreateBufferForAllToAllWithNeighbors()
        {
            int totalLength = 0;
            foreach (int[] entries in NeighborCommonEntries) totalLength += entries.Length;
            return new double[totalLength];
        }
    }
}
