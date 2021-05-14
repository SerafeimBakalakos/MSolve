using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Exceptions;
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
//TODOMPI: should this class have a Length property? It seems important for many linear algebra dimension matching checks, but 
//      it will probably require significant communication. Furthermore, these checks can probably depend on polymorphic methods
//      exposed by the vectors & matrix classes, which will check matching dimensions between matrix-vector or vector-vector.
//      E.g. Adding 2 vectors requires that they have the same length. Vector will check exactly that, and possibly expose a 
//      PatternMatchesForLinearCombo(other) method. DistributedVector however will check that they have the same indexers, 
//      without any need to communicate, only to find the total length. If I do provide such a property, it should be accessed 
//      from the indexer (which must be 1 object for all compute nodes). The indexer should lazily calculate it, store it
//      internally and update it whenever the connectivity changes. Or just prohibit changing the connectivity. Calculating it
//      will be similar to the dot product: sum the number of internal and boundary entries in each local node (divide the 
//      boundary entries over the multiplicities resulting in fractional number), reduce the double result from node and finally
//      round it to the nearest integer (and pray the precision errors are negligible).
namespace MGroup.Solvers.Distributed.LinearAlgebra
{
    public class DistributedOverlappingVector : IIterativeMethodVector
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

        public void AxpyIntoThis(IIterativeMethodVector otherVector, double otherCoefficient)
        {
            if (otherVector is DistributedOverlappingVector casted) AxpyIntoThis(casted, otherCoefficient);
            else
            {
                throw new SparsityPatternModifiedException(
                    "This operation is legal only if the 2 vectors have the same type and indexers.");
            }
        }

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

        IIterativeMethodVector IIterativeMethodVector.Copy() => Copy();

        public DistributedOverlappingVector Copy()
        {
            Dictionary<ComputeNode, Vector> localVectorsCloned = 
                environment.CreateDictionary(node => LocalVectors[node].Copy());
            return new DistributedOverlappingVector(environment, indexers, localVectorsCloned);
        }

        public void CopyFrom(IIterativeMethodVector otherVector)
        {
            if (otherVector is DistributedOverlappingVector casted) CopyFrom(casted);
            else
            {
                throw new SparsityPatternModifiedException(
                    "This operation is legal only if the 2 vectors have the same type and indexers.");
            }
        }

        public void CopyFrom(DistributedOverlappingVector other)
        {
            environment.DoPerNode(node => this.LocalVectors[node].CopyFrom(other.LocalVectors[node]));
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

        IIterativeMethodVector IIterativeMethodVector.CreateZeroVectorWithSameFormat() => CreateZeroVectorWithSameFormat();

        public DistributedOverlappingVector CreateZeroVectorWithSameFormat()
        {
            Dictionary<ComputeNode, Vector> zeroLocalVectors = environment.CreateDictionary(
                node => Vector.CreateZero(LocalVectors[node].Length));
            return new DistributedOverlappingVector(environment, indexers, zeroLocalVectors);
        }

        public double DotProduct(IIterativeMethodVector otherVector)
        {
            if (otherVector is DistributedOverlappingVector casted) return DotProduct(casted);
            else
            {
                throw new SparsityPatternModifiedException(
                    "This operation is legal only if the 2 vectors have the same type and indexers.");
            }
        }

        public double DotProduct(DistributedOverlappingVector otherVector)
        {
            Debug.Assert((this.environment == otherVector.environment) && (this.indexers == otherVector.indexers));
            Func<ComputeNode, double> calcLocalDot = node =>
            {
                DistributedIndexer indexer = this.indexers[node];
                Vector thisLocalVector = this.LocalVectors[node];
                Vector otherLocalVector = otherVector.LocalVectors[node];

                double dotLocal = 0.0;
                for (int i = 0; i < indexer.NumTotalEntries; ++i)
                {
                    dotLocal += thisLocalVector[i] * otherLocalVector[i] / indexer.Multiplicities[i];
                }

                return dotLocal;
            };

            Dictionary<ComputeNode, double> dotPerNode = environment.CreateDictionary(calcLocalDot);
            return environment.AllReduceSum(dotPerNode);
        }

        public bool Equals(DistributedOverlappingVector other, double tolerance = 1E-7)
        {
            if ((this.environment != other.environment) || (this.indexers != other.indexers)) return false;

            Dictionary<ComputeNode, bool> flags = environment.CreateDictionary(
                node => this.LocalVectors[node].Equals(other.LocalVectors[node], tolerance));
            return environment.AllReduceAnd(flags);
        }

        public void LinearCombinationIntoThis(
            double thisCoefficient, IIterativeMethodVector otherVector, double otherCoefficient)
        {
            if (otherVector is DistributedOverlappingVector casted)
            {
                LinearCombinationIntoThis(thisCoefficient, casted, otherCoefficient);
            }
            else
            {
                throw new SparsityPatternModifiedException(
                    "This operation is legal only if the 2 vectors have the same type and indexers.");
            }
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

        /// <summary>
        /// Gathers the entries of remote vectors that correspond to the boundary entries of the local vectors and sums them.
        /// As a result, the boundary entries of each local vector will have the same total values. These values are the same
        /// as the ones we would have if a global vector was created by assembling the local vectors.
        /// </summary>
        /// /// <remarks>
        /// Requires communication between compute nodes:
        /// Each compute node sends its boundary entries to the neighbors that are assiciated with these entries. 
        /// Each neighbor receives only the entries it has in common.
        /// </remarks>
        public void SumOverlappingEntries()
        {
            // Prepare the boundary entries of each node before communicating them to its neighbors.
            Func<ComputeNode, AllToAllNodeData> prepareLocalData = node =>
            {
                Vector localVector = LocalVectors[node];
                DistributedIndexer indexer = indexers[node];

                // Find the common entries (to send) of this node with each of its neighbors
                double[][] sendValues = indexer.CreateBuffersForAllToAllWithNeighbors();
                for (int n = 0; n < node.Neighbors.Count; ++n) // Neighbors of a node must be always accessed in this order
                {
                    int[] commonEntries = indexer.GetCommonEntriesWithNeighbor(node.Neighbors[n]);
                    var sv = Vector.CreateFromArray(sendValues[n]);
                    sv.CopyNonContiguouslyFrom(localVector, commonEntries);
                }

                // Get a buffer for the common entries (to receive) of this node with each of its neighbors. 
                double[][] recvValues = indexer.CreateBuffersForAllToAllWithNeighbors();
                return new AllToAllNodeData() { sendValues = sendValues, recvValues = recvValues };
            };
            var dataPerNode = environment.CreateDictionary(prepareLocalData);

            // Perform AllToAll to exchange the common boundary entries of each node with its neighbors.
            environment.NeighborhoodAllToAll(dataPerNode);

            // Add the common entries of neighbors back to the original local vector.
            Action<ComputeNode> sumLocalSubvectors = node =>
            {
                Vector localVector = LocalVectors[node];
                DistributedIndexer indexer = indexers[node];
                double[][] recvValues = dataPerNode[node].recvValues;

                for (int n = 0; n < node.Neighbors.Count; ++n) // Neighbors of a node must be always accessed in this order
                {
                    int[] commonEntries = indexer.GetCommonEntriesWithNeighbor(node.Neighbors[n]);
                    var rv = Vector.CreateFromArray(recvValues[n]);
                    localVector.AddIntoThisNonContiguouslyFrom(commonEntries, rv);
                }
            };
            environment.DoPerNode(sumLocalSubvectors);
        }

        /// <summary>
        /// Identical to <see cref="SumOverlappingEntries"/>, but uses neighborhood collectives. Unfortunately these are not
        /// fully implemented yet.
        /// </summary>
        private void SumOverlappingEntriesFuture()
        {
            // Prepare the boundary entries of each node before communicating them to its neighbors.
            Func<ComputeNode, AllToAllNodeDataEntire> prepareLocalData = node =>
            {
                Vector localVector = LocalVectors[node];
                DistributedIndexer indexer = indexers[node];

                // Find the common entries (to send) of this node with each of its neighbors, store them contiguously in an 
                // array and store their counts in another array.
                int[] counts = new int[node.Neighbors.Count];
                double[] sendValues = indexer.CreateEntireBufferForAllToAllWithNeighbors();
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
                double[] recvValues = indexer.CreateEntireBufferForAllToAllWithNeighbors();

                return new AllToAllNodeDataEntire() { sendValues = sendValues, sendRecvCounts = counts, recvValues = recvValues };
            };
            var dataPerNode = environment.CreateDictionary(prepareLocalData);

            // Perform AllToAll to exchange the common boundary entries of each node with its neighbors.
            environment.NeighborhoodAllToAll(dataPerNode);

            // Add the common entries of neighbors back to the original local vector.
            Action<ComputeNode> sumLocalSubvectors = node =>
            {
                Vector localVector = LocalVectors[node];
                DistributedIndexer indexer = indexers[node];
                double[] recvValues = dataPerNode[node].recvValues;

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
    }
}
