﻿using System;
using System.Collections.Generic;
using System.Text;

//TODOMPI: Use Func<int, T> instead of Dictionary<int, T> as parameter. It will also allow lazy calculation fo subdomain data.
//      If I need the semantics of "already calculated data are passed into the environment" then I could have extension methods
//      that use Dictionaries and call IComputeEnvironment methods, 
//      e.g. Reduce(this IComputeEnvironment environment, Dictionary<int, double> values) => environment.Reduce(n => values[n])
namespace MGroup.Environments
{
    /// <summary>
    /// Manages a collection of compute nodes (e.g. MPI processes, C# threads, etc). Each compute node has its own distributed 
    /// memory, even if all nodes are run on the same CPU thread. As such, classes that implement this interface, describe 
    /// execution of operations across nodes (parallel, sequential, etc), data transfer between each node's memory and 
    /// synchronization of the nodes.
    /// </summary>
    public interface IComputeEnvironment
    {
        bool AllReduceAnd(Dictionary<int, bool> valuePerNode);

        double AllReduceSum(Dictionary<int, double> valuePerNode);

        /// <summary>
        /// Keys are the ids of the <see cref="ComputeNode"/> objects managed by this environment.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="createPerNode"></param>
        Dictionary<int, T> CreateDictionaryPerNode<T>(Func<int, T> createDataPerNode);

        void DoPerNode(Action<int> actionPerNode);

        //TODOMPI: Its most common use is weird: An Action<int> is called by the environment. The environment passes the id of 
        //      each ComputeNode it manages. Then the Action<int> requests from the environment to provide the ComputeNode for
        //      the same id that the environment provided, e.g. to inspect the neighboring ComputeNode ids.
        ComputeNode GetComputeNode(int nodeID);

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
        void NeighborhoodAllToAll<T>(Dictionary<int, AllToAllNodeData<T>> dataPerNode, bool areRecvBuffersKnown);
    }

    //TODOMPI: Use Dictionaries<int, T[]> with ComputeNode.ID as keys, instead of T[][].
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