using System;
using System.Collections.Generic;
using System.Text;

//TODOMPI: consider renaming IProcessEnvironment to IThreadEnvironment or something similar. Also the comments. Actually these
//      local units of execution are neither processes (OS & MPI construct) nor threads (C#, CPU construct). 
//      I need a different name: e.g. ComputeNode, Cluster, etc.
namespace MGroup.Solvers.MPI.Environment
{
    /// <summary>
    /// Manages a collection of compute nodes (e.g. MPI processes, C# threads, etc). Each compute node has its own distributed 
    /// memory, even if all nodes are run on the same CPU thread. As such classes that implement this interface, describe 
    /// execution of operations across nodes (parallel, sequential, etc), data transfer between each node's memory and 
    /// synchronization of the nodes.
    /// </summary>
    public interface IComputeEnvironment
    {
        List<ComputeNode> ComputeNodes { get; }

        double AllReduceSum(Dictionary<ComputeNode, double> valuePerNode);

        /// <summary>
        /// Keys are the compute nodes managed by this environment.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="createPerNode"></param>
        Dictionary<ComputeNode, T> CreateDictionary<T>(Func<ComputeNode, T> createDataPerNode); //TODOMPI: perhaps I should use Dictionaries for all methods to collect data from each compute node.

        void DoPerNode(Action<ComputeNode> actionPerNode);

        //TODOMPI: the order of entries in values and counts arrays must match the order of neighbors. This should be enforced
        //      by the IComputeEnvironment implementation and communicated to the client.
        //TODO: Extend it for nonsymmetric transfers: sendCounts != recvCounts
        void NeighborhoodAllToAll(
            Dictionary<ComputeNode, (double[] inValues, int[] counts, double[] outValues)> dataPerNode);
    }
}
