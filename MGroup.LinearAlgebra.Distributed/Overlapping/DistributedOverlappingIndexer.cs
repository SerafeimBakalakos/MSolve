﻿using System;
using System.Collections.Generic;
using System.Text;
using MGroup.Environments;

namespace MGroup.LinearAlgebra.Distributed.Overlapping
{
    public class DistributedOverlappingIndexer
    {
        private readonly Dictionary<int, Local> localIndexers;

        public DistributedOverlappingIndexer(IComputeEnvironment environment)
        {
            localIndexers = environment.CreateDictionaryPerNode(
                n => new Local(environment.GetComputeNode(n)));
        }

        public DistributedOverlappingIndexer.Local GetLocalComponent(int nodeID) => localIndexers[nodeID];

        public class Local
        {
            private Dictionary<int, int[]> commonEntriesWithNeighbors;

            public Local(ComputeNode node)
            {
                this.Node = node;
            }

            public int[] Multiplicities { get; private set; }

            public ComputeNode Node { get; }

            public int NumEntries { get; private set; }

            //TODO: cache a buffer for sending and a buffer for receiving inside Indexer (lazily or not) and just return them. 
            //      Also provide an option to request newly initialized buffers. It may be better to have dedicated Buffer classes to
            //      handle all that logic (e.g. keeping allocated buffers in a LinkedList, giving them out & locking them, 
            //      freeing them in clients, etc.
            public double[][] CreateBuffersForAllToAllWithNeighbors()
            {
                int numNeighbors = Node.Neighbors.Count;
                var buffers = new double[numNeighbors][];
                for (int n = 0; n < numNeighbors; ++n)
                {
                    int neighbor = Node.Neighbors[n];
                    buffers[n] = new double[commonEntriesWithNeighbors[neighbor].Length];
                }
                return buffers;
            }

            public void Initialize(int numTotalEntries, Dictionary<int, int[]> commonEntriesWithNeighbors)
            {
                this.NumEntries = numTotalEntries;
                this.commonEntriesWithNeighbors = commonEntriesWithNeighbors;
                FindMultiplicities();
            }

            public int[] GetCommonEntriesWithNeighbor(int neighbor) => commonEntriesWithNeighbors[neighbor];

            public void FindMultiplicities()
            {
                Multiplicities = new int[NumEntries];
                for (int i = 0; i < NumEntries; ++i) Multiplicities[i] = 1;
                foreach (int[] commonEntries in commonEntriesWithNeighbors.Values)
                {
                    foreach (int i in commonEntries) Multiplicities[i] += 1;
                }
            }
        }
    }
}