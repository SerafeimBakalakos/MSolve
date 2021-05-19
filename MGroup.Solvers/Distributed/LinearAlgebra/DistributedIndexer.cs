using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MGroup.Solvers.Distributed.Topologies;

//TODOMPI: the counts array for AllToAll can be cached by the indexer. Also a mapping array can be cached as well to 
//      simplify the nested for loops required to copy from localVector to sendValues and from recvValues to localVector. 
namespace MGroup.Solvers.Distributed.LinearAlgebra
{
    public class DistributedIndexer
    {
        private readonly Dictionary<ComputeNode, DistributedIndexerNodeLevel> localIndexers;

        public DistributedIndexer(IEnumerable<ComputeNode> computeNodes)
        {
            //TODOMPI: Use environment to create this Dictionary, which will also ensure that only local nodes are processed.
            localIndexers = new Dictionary<ComputeNode, DistributedIndexerNodeLevel>();
            foreach (ComputeNode node in computeNodes) localIndexers[node] = new DistributedIndexerNodeLevel(node); 
        }

        public void ConfigureForNode(ComputeNode node, int numTotalEntries,
            Dictionary<ComputeNode, int[]> commonEntriesWithNeighbors)
            => localIndexers[node].Initialize(numTotalEntries, commonEntriesWithNeighbors);

        //TODO: cache a buffer for sending and a buffer for receiving inside Indexer (lazily or not) and just return them. 
        //      Also provide an option to request newly initialized buffers. It may be better to have dedicated Buffer classes to
        //      handle all that logic (e.g. keeping allocated buffers in a LinkedList, giving them out & locking them, 
        //      freeing them in clients, etc.
        public double[][] CreateBuffersForAllToAllWithNeighbors(ComputeNode node) 
            => localIndexers[node].CreateBuffersForAllToAllWithNeighbors();

        public int[] GetCommonEntriesOfNodeWithNeighbor(ComputeNode node, ComputeNode neighbor) 
            => localIndexers[node].GetCommonEntriesWithNeighbor(neighbor);

        public int[] GetEntryMultiplicities(ComputeNode node) => localIndexers[node].Multiplicities;

        public int GetNumEntries(ComputeNode node) => localIndexers[node].NumEntries;
    }
}
