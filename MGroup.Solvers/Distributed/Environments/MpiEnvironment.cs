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
    /// There is only one <see cref="ComputeNode"/> per MPI process. There may be many processes per machine, but each has its 
    /// own memory address space and they can communicate only through the MPI library. Aside from sharing hardware resources, 
    /// the processes and <see cref="ComputeNode"/>s are in essence run on different machines. 
    /// The data for a given <see cref="ComputeNode"/> and its <see cref="ComputeSubnode"/>s are assumed to exist in the same 
    /// shared memory address space. The execution of operations per <see cref="ComputeSubnode"/> of the local 
    /// <see cref="ComputeNode"/> depends on the <see cref="ISubnodeEnvironment"/> used.
    /// </summary>
    /// <remarks>
    /// Implements the Dispose pattern: 
    /// https://www.codeproject.com/Articles/15360/Implementing-IDisposable-and-the-Dispose-Pattern-P
    /// </remarks>
    public sealed class MpiEnvironment : IComputeEnvironment, IDisposable
    {
        private static readonly int allToAllTag = IntGuids.GetNewNonNegativeGuid();
        private static readonly int allToAllBufferLengthsTag = IntGuids.GetNewNonNegativeGuid();

        private readonly Intracommunicator commWorld;
        private readonly MPI.Environment mpiEnvironment;

        /// <summary>
        /// In MPI 2.0 this is very limited thus I implemented its functionality myself and don't need it
        /// </summary>
        private GraphCommunicator commNeighborhood; 

        private bool disposed = false;
        private ComputeNode localNode;
        private ComputeNodeTopology nodeTopology;
        private ISubnodeEnvironment subnodeEnvironment;

        public MpiEnvironment(int numProcesses, ISubnodeEnvironment subnodeEnvironment)
        {
            if (!subnodeEnvironment.IsMemoryAddressSpaceShared)
            {
                throw new ArgumentException("For subnodes only environments with shared memory address space can be used.");
            }
            this.subnodeEnvironment = subnodeEnvironment;

            //TODOMPI: See Threading param. In multithreaded programs, I must specify that to MPI.NET.
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
                if (value.Nodes.Count != commWorld.Size) //
                {
                    throw new ArgumentException(
                        $"There must be as many compute nodes as there are MPI processes ({commWorld.Size})");
                }

                nodeTopology = value;
                localNode = nodeTopology.Nodes[commWorld.Rank]; // Keep only 1 node for this process.
                subnodeEnvironment.Subnodes = localNode.Subnodes.Values;
                //commNeighborhood = CreateGraphCommunicator(nodeTopology); // Not needed yet
            } 
        }

        public int NumComputeNodes { get; }

        public ISubnodeEnvironment SubnodeEnvironment
        {
            get => subnodeEnvironment;
            set
            {
                //TODOMPI: Also check that the mode of MPI.NET environment (Threading param) does not change
                if (!subnodeEnvironment.IsMemoryAddressSpaceShared)
                {
                    throw new ArgumentException("For subnodes only environments with shared memory address space can be used.");
                }
                this.subnodeEnvironment = value;
            }
        }

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
            => subnodeEnvironment.CreateDictionaryPerSubnode(createDataPerSubnode);

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void DoPerNode(Action<ComputeNode> actionPerNode)
        {
            actionPerNode(localNode);
        }

        public void DoPerSubnode(Action<ComputeSubnode> actionPerSubnode)
        {
            foreach (ComputeSubnode subnode in localNode.Subnodes.Values) //TODOMPI: Parallelize this preferably with strategy method
            {
                actionPerSubnode(subnode);
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

        /// <summary>
        /// Initialize the MPI graph communicator. The edges between processes are the same as the 
        /// connectivity of compute nodes. 
        /// </summary>
        /// <param name="nodeTopology"></param>
        private GraphCommunicator CreateGraphCommunicator(ComputeNodeTopology nodeTopology)
        {
            //TODO: use the distributed MPI graphs, which are more scalable since they do not need to specify the full graph
            //TODO: allow MPI to reorder ranks

            if (nodeTopology.Nodes.Count != commWorld.Size)
            {
                throw new ArgumentException(
                    $"There must be as many compute nodes as there are MPI processes ({commWorld.Size})");
            }

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
            return new GraphCommunicator(commWorld, edges, reorderRanks);
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
