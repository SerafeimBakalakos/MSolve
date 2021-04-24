using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using MGroup.Solvers.Distributed.Environments;
using MGroup.Solvers.Distributed.Topologies;

//TODOMPI: perhaps I should have the vector rearranged so that entries belonging to the same boundary are consecutive. 
//      This is definitely faster for linear algebra operations between distributed vectors, but perhaps not between 
//      subdomain-cluster operations. Although the mapping matrices for subdomain-cluster operations could be rearranged as well!
//      Also for consecutive subvectors, I can employ BLAS, instead of writing linear algebra operations by hand.
//TODOMPI: this class will be mainly used for iterative methods. Taking that into account, make optimizations. E.g. work arrays
//      used as buffers for MPI communication can be reused across vectors, instead of each vector allocating/freeing identical 
//      buffers. Such functionality can be included in the indexer, which is shared across vectors/matrices.
namespace MGroup.Solvers.Distributed.LinearAlgebra
{
    public class DistributedOverlappingVector
    {
        private readonly IComputeEnvironment environment;
        private readonly Dictionary<ComputeNode, DistributedIndexer> indexers; //TODOMPI: a global Indexer object that stores data for each node

        public DistributedOverlappingVector(IComputeEnvironment environment, 
            Dictionary<ComputeNode, DistributedIndexer> indexers)
        {
            this.environment = environment;
            this.indexers = indexers;
            this.LocalVectors = environment.CreateDictionary(node => Vector.CreateZero(indexers[node].NumTotalEntries));
        }

        public DistributedOverlappingVector(IComputeEnvironment environment,
            Dictionary<ComputeNode, DistributedIndexer> indexers, Dictionary<ComputeNode, Vector> localVectors)
        {
            this.environment = environment;
            this.indexers = indexers;
            this.LocalVectors = localVectors;
        }

        public Dictionary<ComputeNode, Vector> LocalVectors { get; }

        public void AxpyIntoThis(DistributedOverlappingVector otherVector, double otherCoefficient)
        {
            Debug.Assert((this.environment == otherVector.environment) && (this.indexers == otherVector.indexers));
            environment.DoPerNode(
                node => this.LocalVectors[node].AxpyIntoThis(otherVector.LocalVectors[node], otherCoefficient)
            );
        }

        public void Clear()
        {
            environment.DoPerNode(node => LocalVectors[node].Clear());
        }

        /// <summary>
        /// In an right-hand-side vector the value of boundary entries is equal to the sum of the corresponding values in local 
        /// vectors. In a left-hand-side vector the corresponding boundary entries of local vectors have the exact same value.
        /// This method performs the conversion. Warning: it should not be called if the vector is already left-hand-side.
        /// </summary>
        /// <remarks>
        /// Requires communication between compute nodes:
        /// Each compute node sends its boundary values to all neighbors. 
        /// Each neighbor receives only the entries it has in common.
        /// </remarks>
        public void ConvertRhsToLhsVector()
        {
            // Prepare the boundary entries of each node before communicating them to its neighbors.
            Func<ComputeNode, (double[] inValues, int[] counts, double[] outValues)> prepareLocalData = node =>
            {
                Vector localVector = LocalVectors[node];
                DistributedIndexer indexer = indexers[node];

                // Find the common entries (to send) of this node with each of its neighbors, store them contiguously in an 
                // array and store their counts in another array.
                int[] counts = new int[node.Neighbors.Count];
                double[] sendValues = indexer.CreateBufferForAllToAllWithNeighbors();
                int sendValuesIdx = 0;
                int countsIdx = 0;
                foreach (ComputeNode neighbor in node.Neighbors) // Neighbors of a node must be always accessed in this order
                {
                    int[] commonEntries = indexer.GetCommonEntriesWithNeighbor(neighbor);
                    counts[countsIdx++] = commonEntries.Length;
                    for (int j = 0; j < commonEntries.Length; ++j)
                    {
                        sendValues[sendValuesIdx++] = localVector[commonEntries[j]];
                    }
                }

                // Get a buffer for the common entries (to receive) of this node with each of its neighbors. 
                // Their counts are the same as the common entries that will be sent.
                double[] recvValues = indexer.CreateBufferForAllToAllWithNeighbors();

                return (sendValues, counts, recvValues);
            };
            var dataPerNode = environment.CreateDictionary(prepareLocalData);

            // Perform AllToAll to exchange the common boundary entries of each node with its neighbors.
            environment.NeighborhoodAllToAll(dataPerNode);

            // Add the common entries of neighbors back to the original local vector.
            Action<ComputeNode> sumLocalSubvectors = node =>
            {
                Vector localVector = LocalVectors[node];
                DistributedIndexer indexer = indexers[node];
                (_, _, double[] recvValues) = dataPerNode[node];

                int recvValuesIdx = 0;
                foreach (ComputeNode neighbor in node.Neighbors) // Neighbors of a node must be always accessed in this order
                {
                    int[] commonEntries = indexer.GetCommonEntriesWithNeighbor(neighbor);
                    for (int j = 0; j < commonEntries.Length; ++j)
                    {
                        localVector[commonEntries[j]] += recvValues[recvValuesIdx++];
                    }
                }
            };
            environment.DoPerNode(sumLocalSubvectors);
        }

        public DistributedOverlappingVector Copy()
        {
            Dictionary<ComputeNode, Vector> localVectorsCloned = 
                environment.CreateDictionary(node => LocalVectors[node].Copy());
            return new DistributedOverlappingVector(environment, indexers, localVectorsCloned);
        }

        /// <summary>
        /// Creates the global array representation from any subvectors employed internally. Mainly for testing purposes. 
        /// </summary>
        /// <returns></returns>
        public double[] CopyToArray()
        {
            //TODOMPI: Use AllGatherV and then remove duplicate entries or copy to a single array.
            throw new NotImplementedException();
        }

        public bool Equals(DistributedOverlappingVector other, double tolerance = 1E-7)
        {
            if ((this.environment != other.environment) || (this.indexers != other.indexers)) return false;

            Dictionary<ComputeNode, bool> flags = environment.CreateDictionary(
                node => this.LocalVectors[node].Equals(other.LocalVectors[node], tolerance));
            return environment.AllReduceAnd(flags);
        }

        public double DotProduct(DistributedOverlappingVector otherVector)
        {
            Debug.Assert((this.environment == otherVector.environment) && (this.indexers == otherVector.indexers));
            Func<ComputeNode, double> calcLocalDot = node =>
            {
                DistributedIndexer indexer = this.indexers[node];
                Vector thisLocalVector = this.LocalVectors[node];
                Vector otherLocalVector = otherVector.LocalVectors[node];

                // Find dot product for internal entries
                double dotLocal = 0.0;
                foreach (int i in indexer.InternalEntries)
                {
                    dotLocal += thisLocalVector[i] * otherLocalVector[i];
                }

                // Finds the dot product for entries of each boundary and divide it with the boundary's multiplicity
                foreach (ComputeNodeBoundary boundary in node.Boundaries)
                {
                    int multiplicity = boundary.Multiplicity;
                    int[] boundaryEntries = indexer.GetEntriesOfBoundary(boundary);
                    double dotBoundary = 0.0;
                    foreach (int i in boundaryEntries)
                    {
                        dotBoundary += thisLocalVector[i] * otherLocalVector[i];
                    }
                    dotLocal += dotBoundary / multiplicity;
                }

                return dotLocal;
            };

            Dictionary<ComputeNode, double> dotPerNode = environment.CreateDictionary(calcLocalDot);
            return environment.AllReduceSum(dotPerNode);
        }

        public void LinearCombinationIntoThis(
            double thisCoefficient, DistributedOverlappingVector otherVector, double otherCoefficient)
        {
            Debug.Assert((this.environment == otherVector.environment) && (this.indexers == otherVector.indexers));
            environment.DoPerNode(
                node => this.LocalVectors[node].LinearCombinationIntoThis(
                    thisCoefficient, otherVector.LocalVectors[node], otherCoefficient)
            );
        }

        public void ScaleIntoThis(double scalar)
        {
            environment.DoPerNode(node => LocalVectors[node].ScaleIntoThis(scalar));
        }
    }
}
