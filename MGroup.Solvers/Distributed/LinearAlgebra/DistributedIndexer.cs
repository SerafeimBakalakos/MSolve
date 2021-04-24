using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MGroup.Solvers.Distributed.Topologies;

namespace MGroup.Solvers.Distributed.LinearAlgebra
{
    //TODOMPI: the counts array for AllToAll can be cached by the indexer. Also a mapping array can be cached as well to 
    //      simplify the nested for loops required to copy from localVector to sendValues and from recvValues to localVector. 
    public class DistributedIndexer
    {
        private Dictionary<ComputeNode, int[]> commonEntriesWithNeighbors;

        private Dictionary<ComputeNodeBoundary, int[]> boundaryEntries;

        public DistributedIndexer(ComputeNode node)
        {
            this.Node = node;
        }

        public int[] InternalEntries { get; private set; }

        public ComputeNode Node { get; }

        public int NumTotalEntries { get; private set; }

        public void Initialize(int numTotalEntries, Dictionary<ComputeNodeBoundary, int[]> boundaryEntries)
        {
            this.NumTotalEntries = numTotalEntries;
            this.boundaryEntries = new Dictionary<ComputeNodeBoundary, int[]>(boundaryEntries); //TODO: Perhaps perform deep copy
            FindInternalEntries();
            FindCommonEntriesWithNeighbors();
        }

        //TODO: cache a buffer for sending and a buffer for receiving inside Indexer (lazily or not) and just return them. 
        //      Also provide an option to request newly initialized buffers. It may be better to have dedicated Buffer classes to
        //      handle all that logic (e.g. keeping allocated buffers in a LinkedList, giving them out & locking them, 
        //      freeing them in clients, etc.
        public double[] CreateBufferForAllToAllWithNeighbors()
        {
            int totalLength = 0;
            foreach (int[] entries in commonEntriesWithNeighbors.Values) totalLength += entries.Length;
            return new double[totalLength];
        }
        

        public int[] GetCommonEntriesWithNeighbor(ComputeNode neighbor) => commonEntriesWithNeighbors[neighbor];

        public int[] GetEntriesOfBoundary(ComputeNodeBoundary boundary) => boundaryEntries[boundary];

        private void FindInternalEntries()
        {
            var allBoundaryEntries = new HashSet<int>();
            foreach (ComputeNodeBoundary boundary in Node.Boundaries)
            {
                allBoundaryEntries.UnionWith(boundaryEntries[boundary]);
            }

            InternalEntries = new int[NumTotalEntries - allBoundaryEntries.Count];
            int idx = 0;
            foreach (int entry in Enumerable.Range(0, NumTotalEntries))
            {
                if (!allBoundaryEntries.Contains(entry))
                {
                    InternalEntries[idx++] = entry;
                }
            }
        }

        private void FindCommonEntriesWithNeighbors()
        {
            // Initialize sets for common entries
            var commonEntriesSets = new Dictionary<ComputeNode, SortedSet<int>>();
            foreach (ComputeNode neighbor in Node.Neighbors)
            {
                commonEntriesSets[neighbor] = new SortedSet<int>();
            }

            // Find the common entries with each neighbor
            foreach (ComputeNodeBoundary boundary in Node.Boundaries)
            {
                int[] entriesOfThisBoundary = boundaryEntries[boundary];
                foreach (ComputeNode neighbor in boundary.Nodes)
                {
                    if (neighbor == Node) continue;
                    commonEntriesSets[neighbor].UnionWith(entriesOfThisBoundary);
                }
            }

            // Convert the sets to arrays
            commonEntriesWithNeighbors = new Dictionary<ComputeNode, int[]>();
            foreach (ComputeNode neighbor in Node.Neighbors)
            {
                commonEntriesWithNeighbors[neighbor] = commonEntriesSets[neighbor].ToArray();
            }
        }
    }
}
