using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using MGroup.Solvers.Distributed.Exceptions;
using MGroup.Solvers.Distributed.Topologies;
using MPI;

//TODOMPI: Dedicated unit tests for each method of the environment classes. MPI tutorials and code reference may contain examples:
//      E.g.: https://www.rookiehpc.com/mpi/docs/mpi_alltoallv.php, 
//      http://www.math-cs.gordon.edu/courses/cps343/presentations/MPI_Collective.pdf
//TODOMPI: Map processes to actual hardware nodes in an efficient manner. Then the whole program (linear algebra, DDM, 
//      model creation) must depend on this mapping.
namespace MGroup.Solvers.Distributed.Environments
{
    /// <summary>
    /// There is only one compute node per process. There may be many processes per machine, but there is no way for them to
    /// communicate other than through the MPI library. Aside from sharing hardware resources, the processes and compute nodes
    /// are in essence run on different machines.
    /// </summary>
    /// <remarks>
    /// Implements the Dispose pattern: 
    /// https://www.codeproject.com/Articles/15360/Implementing-IDisposable-and-the-Dispose-Pattern-P
    /// </remarks>
    public sealed class MpiEnvironment : IComputeEnvironment, IDisposable
    {
        private static readonly int allToAllTag = IntGuids.GetNewNonNegativeGuid()/*Guid.NewGuid().GetHashCode()*/;
        private static readonly int allToAllBufferLengthsTag = IntGuids.GetNewNonNegativeGuid()/*Guid.NewGuid().GetHashCode()*/;

        private bool disposed = false;
        private readonly MPI.Environment mpiEnvironment;
        private readonly Intracommunicator commWorld;
        private GraphCommunicator commNeighborhood; //TODOMPI: this must be injected properly
        private ComputeNode localNode;
        private ComputeNodeTopology nodeTopology;


        public MpiEnvironment(int numProcesses)
        {
            string[] args = Array.Empty<string>();
            var mpiEnvironment = new MPI.Environment(ref args);

            // Check if the number of processes launched is correct
            if (numProcesses != Communicator.world.Size)
            {
                int commSize = Communicator.world.Size;
                mpiEnvironment.Dispose();
                throw new ArgumentException(
                    $"The expected number of processes ({numProcesses}) does not match " +
                    $"the actual number of processes launched ({commSize})");
            }

            this.mpiEnvironment = mpiEnvironment;
            this.commWorld = Communicator.world;
            this.NumComputeNodes = numProcesses;
        }

        ~MpiEnvironment()
        {
            Dispose(false);
        }

        public ComputeNode LocalNode => localNode;

        /// <summary>
        /// The full topology of compute nodes, not only the one that corresponds to this process. 
        /// The node that corresponds to this process will be the one with <see cref="ComputeNode.ID"/> equal to the process
        /// rank in <see cref="Communicator.world"/>.
        /// </summary>
        public ComputeNodeTopology NodeTopology 
        { 
            get => nodeTopology;
            set 
            {
                //TODO: use the distributed MPI graphs, which are more scalable since they do not need to specify the full graph
                //TODO: allow MPI to reorder ranks

                if (value.Nodes.Count != commWorld.Size)
                {
                    throw new ArgumentException(
                        $"There must be as many compute nodes as there are MPI processes ({commWorld.Size})");
                }

                nodeTopology = value;
                localNode = nodeTopology.Nodes[commWorld.Rank]; // Keep only 1 node for this process.

                // Initialize the MPI graph communicator. The edges between processes are the same as the 
                // connectivity of compute nodes.
                var edges = new int[commWorld.Size][];
                bool reorderRanks = false;
                for (int p = 0; p < commWorld.Size; ++p)
                {
                    ComputeNode node = nodeTopology.Nodes[p];
                    edges[p] = new int[node.Neighbors.Count];
                    for (int n = 0; n < node.Neighbors.Count; ++n)
                    {
                        edges[p][n] = node.Neighbors[n].ID;
                    }
                }
                commNeighborhood = new GraphCommunicator(commWorld, edges, reorderRanks);
            } 
        }

        public int NumComputeNodes { get; }

        public T AccessNodeDataFromSubnode<T>(ComputeSubnode subnode, Func<ComputeNode, T> getNodeData)
            => getNodeData(subnode.ParentNode);

        public T AccessSubnodeDataFromNode<T>(ComputeSubnode subnode, Func<ComputeSubnode, T> getSubnodeData)
            => getSubnodeData(subnode);

        public bool AllReduceAndForNodes(Dictionary<ComputeNode, bool> valuePerNode)
        {
            bool localValue = valuePerNode[localNode];
            return commWorld.Allreduce(localValue, Operation<bool>.LogicalAnd);
        }

        public double AllReduceSumForNodes(Dictionary<ComputeNode, double> valuePerNode)
        {
            double localValue = valuePerNode[localNode];
            return commWorld.Allreduce(localValue, Operation<double>.Add);
        }

        public Dictionary<ComputeNode, T> CreateDictionaryPerNode<T>(Func<ComputeNode, T> createDataPerNode)
        {
            var result = new Dictionary<ComputeNode, T>();
            result[localNode] = createDataPerNode(localNode);
            return result;
        }

        public Dictionary<int, T> CreateDictionaryPerSubnode<T>(Func<ComputeSubnode, T> createDataPerSubnode)
        {
            var result = new Dictionary<int, T>();
            foreach (ComputeSubnode subnode in localNode.Subnodes.Values) //TODOMPI: Parallelize this preferably with strategy method
            {
                result[subnode.ID] = createDataPerSubnode(subnode);
            }
            return result;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void DoPerNode(Action<ComputeNode> actionPerNode)
        {
            actionPerNode(localNode);
        }

        public void DoPerSubnode(Action<ComputeSubnode> action)
        {
            foreach (ComputeSubnode subnode in localNode.Subnodes.Values) //TODOMPI: Parallelize this preferably with strategy method
            {
                action(subnode);
            }
        }

        public void NeighborhoodAllToAllForNodes<T>(Dictionary<ComputeNode, AllToAllNodeData<T>> dataPerNode, bool areRecvBuffersKnown)
        {
            AllToAllNodeData<T> data = dataPerNode[localNode];
            int numNeighbors = localNode.Neighbors.Count;

            //TODO: This can be improved greatly. As soon as a process receives the length of its recv buffer, the actual data 
            //      can be transfered between these 2 processes. There is no need to wait for the other p2p length communications
            if (!areRecvBuffersKnown) 
            {
                // Transfer the lengths of receive buffers via non-blocking send/receive operations
                var recvLengthRequests = new RequestList(); //TODOMPI: Use my own RequestList implementation for more efficient WaitAll() and pipeline opportunities
                for (int n = 0; n < numNeighbors; ++n)
                {
                    ReceiveRequest req = commWorld.ImmediateReceive<int>(
                        localNode.Neighbors[n].ID, allToAllBufferLengthsTag, length => data.recvValues[n] = new T[length]);
                    recvLengthRequests.Add(req);
                }

                var sendLengthRequests = new RequestList(); //TODOMPI: Can't I avoid waiting for send requests? Especially for the lengths
                for (int n = 0; n < numNeighbors; ++n)
                {
                    Request req = commWorld.ImmediateSend(
                        data.sendValues[n].Length, localNode.Neighbors[n].ID, allToAllBufferLengthsTag);
                    sendLengthRequests.Add(req);
                }

                // Wait for requests to end 
                recvLengthRequests.WaitAll();
                sendLengthRequests.WaitAll();
            }

            // Transfer data via non-blocking send/receive operations
            var recvRequests = new RequestList(); //TODOMPI: Use my own RequestList implementation for more efficient WaitAll() and pipeline opportunities
            for (int n = 0; n < numNeighbors; ++n)
            {
                ReceiveRequest req = commWorld.ImmediateReceive(localNode.Neighbors[n].ID, allToAllTag, data.recvValues[n]);
                recvRequests.Add(req);
            }

            var sendRequests = new RequestList(); //TODOMPI: Can't I avoid waiting for send requests? 
            for (int n = 0; n < numNeighbors; ++n)
            {
                Request req = commWorld.ImmediateSend(data.sendValues[n], localNode.Neighbors[n].ID, allToAllTag);
                sendRequests.Add(req);
            }

            // Wait for requests to end 
            recvRequests.WaitAll();
            sendRequests.WaitAll();
        }

        public void NeighborhoodAllToAllForNodes(Dictionary<ComputeNode, AllToAllNodeData> dataPerNode)
        {
            AllToAllNodeData data = dataPerNode[localNode];
            int numNeighbors = localNode.Neighbors.Count;

            // Communication via non-blocking send/receive operations
            var recvRequests = new RequestList(); //TODOMPI: Use my own RequestList implementation for more efficient WaitAll() and pipeline opportunities
            for (int n = 0; n < numNeighbors; ++n)
            {
                ReceiveRequest req = commWorld.ImmediateReceive(localNode.Neighbors[n].ID, allToAllTag, data.recvValues[n]);
                recvRequests.Add(req);
            }

            var sendRequests = new RequestList(); //TODOMPI: Can't I avoid waiting for send requests? 
            for (int n = 0; n < numNeighbors; ++n)
            {
                Request req = commWorld.ImmediateSend(data.sendValues[n], localNode.Neighbors[n].ID, allToAllTag);
                sendRequests.Add(req);
            }

            // Wait for requests to end 
            recvRequests.WaitAll();
            sendRequests.WaitAll();
        }

        public void NeighborhoodAllToAllForNodes(Dictionary<ComputeNode, AllToAllNodeDataEntire> dataPerNode)
        {
            AllToAllNodeDataEntire data = dataPerNode[localNode];

            // 1D buffers to jagged arrays 
            int numNeighbors = localNode.Neighbors.Count;
            double[][] sendValues = new double[numNeighbors][];
            double[][] recvValues = new double[numNeighbors][];
            int[] counts = data.sendRecvCounts;
            int offset = 0;
            for (int n = 0; n < numNeighbors; ++n)
            {
                sendValues[n] = new double[counts[n]];
                Array.Copy(data.sendValues, offset, sendValues[n], 0, counts[n]);
                recvValues[n] = new double[counts[n]];
                offset += counts[n];
            }

            // Communication via non-blocking send/receive operations
            var recvRequests = new RequestList(); //TODOMPI: Use my own RequestList implementation for more efficient WaitAll() and pipeline opportunities
            for (int n = 0; n < numNeighbors; ++n)
            {
                ReceiveRequest req = commWorld.ImmediateReceive(localNode.Neighbors[n].ID, allToAllTag, recvValues[n]);
                recvRequests.Add(req);
            }

            var sendRequests = new RequestList(); //TODOMPI: Can't I avoid waiting for send requests?
            for (int n = 0; n < numNeighbors; ++n)
            {
                Request req = commWorld.ImmediateSend(sendValues[n], localNode.Neighbors[n].ID, allToAllTag);
                sendRequests.Add(req); 
            }

            // Wait for requests to end 
            recvRequests.WaitAll();
            sendRequests.WaitAll();

            // Jagged arrays to 1D buffers
            offset = 0;
            for (int n = 0; n < numNeighbors; ++n)
            {
                Array.Copy(recvValues[n], 0, data.recvValues, offset, counts[n]);
                offset += counts[n];
            }
        }

        private void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    if (commNeighborhood != null)
                    {
                        commNeighborhood.Dispose();
                    }

                    // DO NOT DISPOSE Communicator.world here, since it is not owned by this class

                    if ((mpiEnvironment != null) && (MPI.Environment.Finalized == false))
                    {
                        mpiEnvironment.Dispose();
                    }
                }

                // If there were unmanaged resources, they should be disposed here
            }
            disposed = true;
        }
    }
}
