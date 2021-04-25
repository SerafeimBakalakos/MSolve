using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using MGroup.Solvers.Distributed.Exceptions;
using MGroup.Solvers.Distributed.Topologies;
using MPI;

//TODOMPI: Dedicated unit tests for each method of the environment classes. MPI totorials and code reference may contain examples:
//      E.g.: https://www.rookiehpc.com/mpi/docs/mpi_alltoallv.php, 
//      http://www.math-cs.gordon.edu/courses/cps343/presentations/MPI_Collective.pdf
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
        private bool disposed = false;
        private readonly MPI.Environment mpiEnvironment;
        private readonly Intracommunicator commWorld;
        private GraphCommunicator commNeighborhood; //TODOMPI: this must be injected properly
        private ComputeNode node;
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
        }

        ~MpiEnvironment()
        {
            Dispose(false);
        }

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
                node = nodeTopology.Nodes[commWorld.Rank]; // Keep only 1 node for this process.

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

        public ComputeNode SingleNode => node; //TODOMPI: Perhaps a List<ComputeNode> ActiveNodes in IComputeEnvironment 

        public bool AllReduceAnd(Dictionary<ComputeNode, bool> valuePerNode)
        {
            bool localValue = valuePerNode[node];
            return commWorld.Allreduce(localValue, Operation<bool>.LogicalAnd);
        }

        public double AllReduceSum(Dictionary<ComputeNode, double> valuePerNode)
        {
            double localValue = valuePerNode[node];
            return commWorld.Allreduce(localValue, Operation<double>.Add);
        }

        public Dictionary<ComputeNode, T> CreateDictionary<T>(Func<ComputeNode, T> createDataPerNode)
        {
            var result = new Dictionary<ComputeNode, T>();
            result[node] = createDataPerNode(node);
            return result;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
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
            commNeighborhood.AlltoallFlattened(inValues, counts, counts, ref outValuesTemp);

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

        //public void NeighborhoodAllToAll(Dictionary<ComputeNode, AllToAllNodeData> dataPerNode)
        //{
            
        //}

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
