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
        //TODO: For now all implemented environments use the same memory address space for nodes and subnodes, thus these Acecss
        //      methods are implemented by simply returning the correct reference. However in more complicated environments
        //      (e.g. ComputeNode being the CPU of a machine in an MPI network, and ComputeSubnodes being accelerators managed by
        //      that CPU) these methods will be necessary
        //TODO: In the aforementioned complex environments, it may also be worthwhile caching the transfered data 
        //      (it may be dangerous though, if these are updated).
        T AccessNodeDataFromSubnode<T>(ComputeSubnode subnode, Func<ComputeNode, T> getNodeData);

        T AccessSubnodeDataFromNode<T>(ComputeSubnode subnode, Func<ComputeSubnode, T> getSubnodeData);

        //TODOMPI: Perhaps this should be injected into the constructors, instead of the setter.
        ComputeNodeTopology NodeTopology { get; set; }

        int NumComputeNodes { get; }

        bool AllReduceAndForNodes(Dictionary<ComputeNode, bool> valuePerNode);

        double AllReduceSumForNodes(Dictionary<ComputeNode, double> valuePerNode);

        /// <summary>
        /// Keys are the <see cref="ComputeNode"/> objects managed by this environment.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="createPerNode"></param>
        Dictionary<ComputeNode, T> CreateDictionaryPerNode<T>(Func<ComputeNode, T> createDataPerNode);

        /// <summary>
        /// Keys are the ids of the <see cref="ComputeSubnode"/> objects (<see cref="ComputeSubnode.ID"/>) managed by this 
        /// environment.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="createPerNode"></param>
        Dictionary<int, T> CreateDictionaryPerSubnode<T>(Func<ComputeSubnode, T> createDataPerSubnode);

        void DoPerNode(Action<ComputeNode> actionPerNode);

        void DoPerSubnode(Action<ComputeSubnode> actionPerSubnode);

        /// <summary>
        /// Each <see cref="ComputeNode"/> sends and receives data to/from <see cref="ComputeNode"/>s in its neighborhood.
        /// Each <see cref="ComputeNode"/> posts the data it will send to each neighboring <see cref="ComputeNode"/> in
        /// <paramref name="dataPerNode"/>. If <paramref name="areRecvBuffersKnown"/> is true, then each 
        /// <see cref="ComputeNode"/> must also post a buffer for data that will be received from each neighboring 
        /// <see cref="ComputeNode"/> in <paramref name="dataPerNode"/>. Otherwise, this method will allocate the receive 
        /// buffers by using extra communication to query their lengths from the corresponding neighbors.
        /// </summary>
        /// <param name="dataPerNode">
        /// See <see cref="AllToAllNodeData{T}"/> for a description of the data that will be transfered.
        /// </param>
        /// <param name="areRecvBuffersKnown">
        /// Signifies if the buffers for receive values in <paramref name="dataPerNode"/> are allocated, in order to avoid 
        /// unnecessary communication. All callers of this method must provide the same value, so this fact must be known 
        /// a priori.
        /// </param>
        void NeighborhoodAllToAllForNodes<T>(Dictionary<ComputeNode, AllToAllNodeData<T>> dataPerNode, bool areRecvBuffersKnown);

        //TODOMPI: the order of entries in values and counts arrays must match the order of neighbors. This should be enforced
        //      by the IComputeEnvironment implementation and communicated to the client.
        //TODO: Extend it for nonsymmetric transfers: sendCounts != recvCounts
        //TODOMPI: Overload that uses delegates for assembling the send data and processing the receive data per neighbor of 
        //      each compute node. For better pipelining. Alternatively expose non-blocking send and receive operations, 
        //      to clients so that they can do them themselves. These may actualy help to avoid unnecessary buffers in 
        //      communications between local nodes.
        /// <summary>
        /// Each <see cref="ComputeNode"/> sends and receives data to/from <see cref="ComputeNode"/>s in its neighborhood. 
        /// </summary>
        /// <param name="dataPerNode">
        /// See <see cref="AllToAllNodeData"/> for a description of the data that will be transfered.
        /// </param>
        void NeighborhoodAllToAllForNodes(Dictionary<ComputeNode, AllToAllNodeData> dataPerNode); //TODOMPI: replace this with the generic version

        
        /// <summary>
        /// Similar to <see cref="NeighborhoodAllToAllForNodes(Dictionary{ComputeNode, AllToAllNodeData})"/>, but depends on neighborhood
        /// collectives. Unfortunately, neighborhood collectives are not supported by MPI.NET yet. Therefore, this method
        /// is less efficient than <see cref="NeighborhoodAllToAllForNodes(Dictionary{ComputeNode, AllToAllNodeData})"/> and working
        /// with its arguments is more complicated.
        /// </summary>
        /// <param name="dataPerNode"></param>
        void NeighborhoodAllToAllForNodes(Dictionary<ComputeNode, AllToAllNodeDataEntire> dataPerNode);
    }

    public class AllToAllNodeData<T>
    {
        /// <summary>
        /// Buffer of values that will be received by a <see cref="ComputeNode"/> i by each of its neighboring 
        /// <see cref="ComputeNode"/>s. Foreach j in <see cref="ComputeNode.Neighbors"/> of i, the values transfered from j to i 
        /// will be stored in <see cref="recvValues"/>[j].
        /// </summary>
        public T[][] recvValues;

        /// Buffer of values that will be sent from a <see cref="ComputeNode"/> i to each of its neighboring 
        /// <see cref="ComputeNode"/>s. Foreach j in <see cref="ComputeNode.Neighbors"/> of i, the values transfered from i to j 
        /// will be stored in <see cref="sendValues"/>[j]. 
        /// </summary>
        public T[][] sendValues;

        public AllToAllNodeData(int numNeighbors)
        {
            this.sendValues = new T[numNeighbors][];
            this.recvValues = new T[numNeighbors][];
        }
    }

    public class AllToAllNodeData //TODOMPI: replace this with the generic version
    {
        /// <summary>
        /// Buffer of values that will be received by a <see cref="ComputeNode"/> i by each of its neighboring 
        /// <see cref="ComputeNode"/>s. Foreach j in <see cref="ComputeNode.Neighbors"/> of i, the values transfered from j to i 
        /// will be stored in <see cref="recvValues"/>[j]. Warning: <see cref="sendValues"/>[j] of node i and 
        /// <see cref="recvValues"/>[i] of node i, must have the same <see cref="Array.Length"/>.
        /// </summary>
        public double[][] recvValues;

        /// Buffer of values that will be sent from a <see cref="ComputeNode"/> i to each of its neighboring 
        /// <see cref="ComputeNode"/>s. Foreach j in <see cref="ComputeNode.Neighbors"/> of i, the values transfered from i to j 
        /// will be stored in <see cref="sendValues"/>[j]. Warning: <see cref="sendValues"/>[j] of node i and 
        /// <see cref="recvValues"/>[i] of node i, must have the same <see cref="Array.Length"/>.
        /// </summary>
        public double[][] sendValues;
    }

    public struct AllToAllNodeDataEntire
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
