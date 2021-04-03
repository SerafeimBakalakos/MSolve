﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using MGroup.Solvers.MPI.Exceptions;
using MPI;

//TODOMPI: Dispose everything from MPI.NET and make this disposable too.
namespace MGroup.Solvers.MPI.Environment
{
    /// <summary>
    /// There is only one compute node per process. There may be many processes per machine, but there is no way for them to
    /// communicate other than through the MPI library. Aside from sharing hardware resources, the processes and compute nodes
    /// are in essence run on different machines.
    /// </summary>
    public class MpiEnvironment : IComputeEnvironment
    {
        private readonly Intracommunicator worldComm;

        public MpiEnvironment()
        {
            this.worldComm = Communicator.world;
        }

        public List<ComputeNode> ComputeNodes { get; } = new List<ComputeNode>();

        public GraphCommunicator neighborComm { get; set; }

        public double AllReduceSum(Dictionary<ComputeNode, double> valuePerNode)
        {
            double localValue = valuePerNode[ComputeNodes[0]];
            return worldComm.Allreduce(localValue, Operation<double>.Add);
        }

        public Dictionary<ComputeNode, T> CreateDictionary<T>(Func<ComputeNode, T> createDataPerNode)
        {
            var result = new Dictionary<ComputeNode, T>();
            ComputeNode node = ComputeNodes[0];
            result[node] = createDataPerNode(node);
            return result;
        }

        public void DoPerNode(Action<ComputeNode> actionPerNode)
        {
            actionPerNode(ComputeNodes[0]);
        }

        public void NeighborhoodAllToAll(
            Dictionary<ComputeNode, (double[] inValues, int[] counts, double[] outValues)> dataPerNode)
        {
            (double[] inValues, int[] counts, double[] outValues) = dataPerNode[ComputeNodes[0]];
            double[] outValuesTemp = outValues;
            neighborComm.AlltoallFlattened(inValues, counts, counts, ref outValuesTemp);

            //TODOMPI: Perhaps I could replace the previous array inside the dictionary, but that would change the semantics.
            //      E.g. if a class retains the buffer for receiving values, that class must update the buffer instance.
            //      Even worse, this behavior is only present for MpiEnvironment, although I could implement it in other 
            //      environments as well.
            if (outValuesTemp != outValues)
            {
                throw new MpiException("The original buffer supplied for writing the received values was not sufficient. "
                    + "Please supply a buffer with length >= the sum of entries in recvCounts");
            }
        }
    }
}
