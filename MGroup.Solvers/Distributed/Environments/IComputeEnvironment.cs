using System;
using System.Collections.Generic;
using System.Text;
using MGroup.Solvers.Distributed.Topologies;

//TODOMPI: consider renaming IProcessEnvironment to IThreadEnvironment or something similar. Also the comments. Actually these
//      local units of execution are neither processes (OS & MPI construct) nor threads (C#, CPU construct). 
//      I need a different name: e.g. ComputeNode, Cluster, etc.
namespace MGroup.Solvers.Distributed.Environments
{
    /// <summary>
    /// Manages a collection of compute nodes (e.g. MPI processes, C# threads, etc). Each compute node has its own distributed 
    /// memory, even if all nodes are run on the same CPU thread. As such classes that implement this interface, describe 
    /// execution of operations across nodes (parallel, sequential, etc), data transfer between each node's memory and 
    /// synchronization of the nodes.
    /// </summary>
    public interface IComputeEnvironment
    {
        //TODOMPI: Perhaps this should be injected into the constructors, instead of the setter.
        ComputeNodeTopology NodeTopology { get; set; }

        bool AllReduceAnd(Dictionary<ComputeNode, bool> valuePerNode);

        double AllReduceSum(Dictionary<ComputeNode, double> valuePerNode);

        /// <summary>
        /// Keys are the compute nodes managed by this environment.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="createPerNode"></param>
        Dictionary<ComputeNode, T> CreateDictionary<T>(Func<ComputeNode, T> createDataPerNode);

        void DoPerNode(Action<ComputeNode> actionPerNode);

        //TODOMPI: the order of entries in values and counts arrays must match the order of neighbors. This should be enforced
        //      by the IComputeEnvironment implementation and communicated to the client.
        //TODO: Extend it for nonsymmetric transfers: sendCounts != recvCounts
        /// <summary>
        /// Each <see cref="ComputeNode"/> sends and receives data to/from <see cref="ComputeNode"/>s in its neighborhood. 
        /// </summary>
        /// <param name="dataPerNode">
        /// See <see cref="AllToAllNodeData"/> for a description of the data that will be transfered.
        /// </param>
        void NeighborhoodAllToAll(Dictionary<ComputeNode, AllToAllNodeData> dataPerNode);
    }

    public struct AllToAllNodeData
    {
        /// <summary>
        /// Buffer of values that will be received by a <see cref="ComputeNode"/> i by the other <see cref="ComputeNode"/>s j in 
        /// the neighborhood of i. Values received by a specific <see cref="ComputeNode"/> j will be contiguous. For 
        /// <see cref="ComputeNode"/> i, the order in which data from each <see cref="ComputeNode"/> j will be received is the 
        /// same as the order of <see cref="ComputeNode"/>s in the neighborhood of i. It is possible that a 
        /// <see cref="ComputeNode"/> will not receive any data from another <see cref="ComputeNode"/>, e.g. itself.
        /// </summary>
        public double[] recvValues;

        /// <summary>
        /// Buffer of values that will be sent by a <see cref="ComputeNode"/> i to the other <see cref="ComputeNode"/>s j in 
        /// the neighborhood of i. Values sent to a specific <see cref="ComputeNode"/> j will be contiguous. For 
        /// <see cref="ComputeNode"/> i, the order in which data to each <see cref="ComputeNode"/> j will be sent is the 
        /// same as the order of <see cref="ComputeNode"/>s in the neighborhood of i. It is possible that a 
        /// <see cref="ComputeNode"/> will not send any data to another <see cref="ComputeNode"/>, e.g. itself.
        /// </summary>
        public double[] sendValues;

        /// <summary>
        /// For a <see cref="ComputeNode"/> i
        /// The number of values that will be sent to / received from each other <see cref="ComputeNode"/>. Specifically
        /// for a <see cref="ComputeNode"/> i: <see cref="sendRecvCounts"/>[0] will be sent to / received from the first 
        /// <see cref="ComputeNode"/> in the neighborhood of i, <see cref="sendRecvCounts"/>[1] to/from the second, etc. If a 
        /// <see cref="ComputeNode"/> i will not send/receive any data to/from another <see cref="ComputeNode"/> j, then 
        /// <see cref="sendRecvCounts"/>[j] = 0.
        /// </summary>
        public int[] sendRecvCounts;
    }

}
