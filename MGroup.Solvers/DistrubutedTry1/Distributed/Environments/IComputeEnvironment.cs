using System;
using System.Collections.Generic;
using System.Text;
using MGroup.Solvers_OLD.DistributedTry1.Distributed.Topologies;

//TODOMPI: consider renaming IProcessEnvironment to IThreadEnvironment or something similar. Also the comments. Actually these
//      local units of execution are neither processes (OS & MPI construct) nor threads (C#, CPU construct). 
//      I need a different name: e.g. ComputeNode, Cluster, etc.
namespace MGroup.Solvers_OLD.DistributedTry1.Distributed.Environments
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

        //TODOMPI: This is often used to determine if there are more than 1 clusters. However the semantics are ambiguous. Does
        //      it show the number of clusters in the whole model or only in this memory address space?
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

        //TODOMPI: the order of entries in values and counts arrays must match the order of neighbors. This should be enforced
        //      by the IComputeEnvironment implementation and communicated to the client.
        //TODOMPI: Overload that uses delegates for assembling the send data and processing the receive data per neighbor of 
        //      each compute node. This will result in better pipelining, which I think will greatly improve performance and 
        //      essentially hide the communication cost, considering what the clients do. 
        //TODOMPI: Alternatively expose non-blocking send and receive operations, to clients so that
        //      they can do them themselves. These may actualy help to avoid unnecessary buffers in communications between
        //      local nodes, for even greater benefit. However it forces clients to mess with async code.
        //      Perhaps IComputeEnvironment could facilitate the clients in their async code, by exposing ISend/IRecv that take
        //      delegates for creating the data (before send) and processing them (after recv) and by helping them to ensure 
        //      termination per node.
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
    }
}
