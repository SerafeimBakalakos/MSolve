using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using MGroup.Solvers.Distributed.Exceptions;
using MGroup.Solvers.Distributed.Topologies;
using MPI;

//TODOMPI: Dispose everything from MPI.NET and make this disposable too.
namespace MGroup.Solvers.Distributed.Environments
{
    /// <summary>
    /// There is only one compute node per process. There may be many processes per machine, but there is no way for them to
    /// communicate other than through the MPI library. Aside from sharing hardware resources, the processes and compute nodes
    /// are in essence run on different machines.
    /// </summary>
    public class MpiEnvironment : IComputeEnvironment
    {
        private readonly Intracommunicator worldComm;
        private ComputeNode node;
        private ComputeNodeTopology nodeTopology;

        public MpiEnvironment()
        {
            this.worldComm = Communicator.world;
        }

        public ComputeNodeTopology NodeTopology 
        { 
            get => nodeTopology;
            set 
            {
                if (nodeTopology.Nodes.Count != 1)
                {
                    throw new ArgumentException("There must be only 1 compute node");
                }
                nodeTopology = value;
                node = nodeTopology.Nodes.Values.First();
            } 
        }

        public GraphCommunicator NeighborComm { get; set; } //TODOMPI: this must be injected properly

        public bool AllReduceAnd(Dictionary<ComputeNode, bool> valuePerNode)
        {
            bool localValue = valuePerNode[node];
            return worldComm.Allreduce(localValue, Operation<bool>.LogicalAnd);
        }

        public double AllReduceSum(Dictionary<ComputeNode, double> valuePerNode)
        {
            double localValue = valuePerNode[node];
            return worldComm.Allreduce(localValue, Operation<double>.Add);
        }

        public Dictionary<ComputeNode, T> CreateDictionary<T>(Func<ComputeNode, T> createDataPerNode)
        {
            var result = new Dictionary<ComputeNode, T>();
            result[node] = createDataPerNode(node);
            return result;
        }

        public void DoPerNode(Action<ComputeNode> actionPerNode)
        {
            actionPerNode(node);
        }

        public void NeighborhoodAllToAll(
            Dictionary<ComputeNode, (double[] inValues, int[] counts, double[] outValues)> dataPerNode)
        {
            (double[] inValues, int[] counts, double[] outValues) = dataPerNode[node];
            double[] outValuesTemp = outValues;
            NeighborComm.AlltoallFlattened(inValues, counts, counts, ref outValuesTemp);

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
