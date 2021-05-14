using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MGroup.Solvers.Distributed.Topologies;

namespace MGroup.Solvers.Distributed.LinearAlgebra
{
    //TODOMPI: the counts array for AllToAll can be cached by the indexer. Also a mapping array can be cached as well to 
    //      simplify the nested for loops required to copy from localVector to sendValues and from recvValues to localVector. 
    internal class DistributedIndexerNodeLevel
    {
        private Dictionary<ComputeNode, int[]> commonEntriesWithNeighbors;

        internal DistributedIndexerNodeLevel(ComputeNode node)
        {
            this.Node = node;
        }

        internal int[] Multiplicities { get; private set; }

        internal ComputeNode Node { get; }

        internal int NumTotalEntries { get; private set; }

        //TODO: cache a buffer for sending and a buffer for receiving inside Indexer (lazily or not) and just return them. 
        //      Also provide an option to request newly initialized buffers. It may be better to have dedicated Buffer classes to
        //      handle all that logic (e.g. keeping allocated buffers in a LinkedList, giving them out & locking them, 
        //      freeing them in clients, etc.
        internal double[][] CreateBuffersForAllToAllWithNeighbors()
        {
            int numNeighbors = Node.Neighbors.Count;
            var buffers = new double[numNeighbors][];
            for (int n = 0; n < numNeighbors; ++n)
            {
                ComputeNode neighbor = Node.Neighbors[n];
                buffers[n] = new double[commonEntriesWithNeighbors[neighbor].Length];
            }
            return buffers;
        }

        //TODO: cache a buffer for sending and a buffer for receiving inside Indexer (lazily or not) and just return them. 
        //      Also provide an option to request newly initialized buffers. It may be better to have dedicated Buffer classes to
        //      handle all that logic (e.g. keeping allocated buffers in a LinkedList, giving them out & locking them, 
        //      freeing them in clients, etc.
        internal double[] CreateEntireBufferForAllToAllWithNeighbors()
        {
            int totalLength = 0;
            foreach (int[] entries in commonEntriesWithNeighbors.Values) totalLength += entries.Length;
            return new double[totalLength];
        }

        internal void Initialize(int numTotalEntries, Dictionary<ComputeNode, int[]> commonEntriesWithNeighbors)
        {
            this.NumTotalEntries = numTotalEntries;
            this.commonEntriesWithNeighbors = commonEntriesWithNeighbors;
            FindMultiplicities();
        }

        internal int[] GetCommonEntriesWithNeighbor(ComputeNode neighbor) => commonEntriesWithNeighbors[neighbor];

        private void FindMultiplicities()
        {
            Multiplicities = new int[NumTotalEntries];
            for (int i = 0; i < NumTotalEntries; ++i) Multiplicities[i] = 1;
            foreach (int[] commonEntries in commonEntriesWithNeighbors.Values)
            {
                foreach (int i in commonEntries) Multiplicities[i] += 1;
            }
        }
    }
}
