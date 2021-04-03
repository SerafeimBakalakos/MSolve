using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using MGroup.Solvers.DDM;
using MGroup.Solvers.MPI.Environment;

//TODOMPI: have an indexer object that determines overlaps and perhaps whether it is a lhs or rhs vector (although we would want
//      to use the same indexer for all vectors and matrices of the same distributed linear system. Use the indexer to check if 
//      linear algebra operations are done between vertices & matrices of the same indexer. Could the linear system be this 
//      indexer?
//TODOMPI: perhaps I should have the vector rearranged so that entries belonging to the same boundary are consecutive. 
//      This is definitely faster for linear algebra operations between distributed vectors, but perhaps not between 
//      subdomain-cluster operations. Although the mapping matrices for subdomain-cluster operations could be rearranged as well!
//      Also for consecutive subvectors, I can employ BLAS, instead of writing linear algebra operations by hand.
//TODOMPI: this class will be mainly used for iterative methods. Taking that into account, make optimization. E.g. work arrays
//      used as buffers for MPI communication can be reused across vectors, instead of each vector allocating/freeing identical 
//      buffers. Such functionality can be included in the indexer, which is shared across vectors/matrices.
namespace MGroup.Solvers.MPI.LinearAlgebra
{
    public class DistributedOverlappingVector
    {
        private readonly IComputeEnvironment environment;
        private readonly Dictionary<ComputeNode, Indexer> indexers; //TODOMPI: a global Indexer object that stores data for each node
        private readonly Dictionary<ComputeNode, Vector> localVectors;

        private DistributedOverlappingVector(IComputeEnvironment environment, Dictionary<ComputeNode, Indexer> indexers, 
            Dictionary<ComputeNode, Vector> localVectors)
        {
            this.environment = environment;
            this.indexers = indexers;
            this.localVectors = localVectors;
        }

        public static DistributedOverlappingVector CreateLhsVector(IComputeEnvironment environment,
            Dictionary<ComputeNode, Indexer> indexers, Dictionary<ComputeNode, Vector> localVectors)
        {
            return new DistributedOverlappingVector(environment, indexers, localVectors);
        }

        public static DistributedOverlappingVector CreateRhsVector(IComputeEnvironment environment,
            Dictionary<ComputeNode, Indexer> indexers, Dictionary<ComputeNode, Vector> localVectors)
        {
            var vector = new DistributedOverlappingVector(environment, indexers, localVectors);
            vector.ConvertRhsToLhsVector();
            return vector;
        }

        public void AxpyIntoThis(DistributedOverlappingVector otherVector, double otherCoefficient)
        {
            environment.DoPerNode(
                node => this.localVectors[node].AxpyIntoThis(otherVector.localVectors[node], otherCoefficient)
            );
        }

        public void Clear()
        {
            environment.DoPerNode(node => localVectors[node].Clear());
        }

        public DistributedOverlappingVector Copy()
        {
            Dictionary<ComputeNode, Vector> localVectorsCloned = 
                environment.CreateDictionary(node => localVectors[node].Copy());
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

        public double DotProduct(DistributedOverlappingVector otherVector)
        {
            Func<ComputeNode, double> calcLocalDot = node =>
            {
                Indexer indexer = this.indexers[node];
                Vector thisLocalVector = this.localVectors[node];
                Vector otherLocalVector = otherVector.localVectors[node];

                // Find dot product for internal entries
                double dotLocal = 0.0;
                foreach (int i in indexer.InternalEntries)
                {
                    dotLocal += thisLocalVector[i] * otherLocalVector[i];
                }

                // Finds the dot product for entries of each boundary and divide it with the boundary's multiplicity
                for (int b = 0; b < indexer.BoundaryEntries.Count; ++b)
                {
                    int multiplicity = indexer.Node.Boundaries[b].Multiplicity;
                    int[] boundaryEntries = indexer.BoundaryEntries[b];
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
            environment.DoPerNode(
                node => this.localVectors[node].LinearCombinationIntoThis(
                    thisCoefficient, otherVector.localVectors[node], otherCoefficient)
            );
        }

        public void ScaleIntoThis(double scalar)
        {
            environment.DoPerNode(node => localVectors[node].ScaleIntoThis(scalar));
        }

        /// <summary>
        /// Each compute node sends its boundary values to all neighbors. 
        /// Each neighbor receives only the entries it has in common.
        /// </summary>
        private void ConvertRhsToLhsVector()
        {
            // Prepare the boundary entries of each node before communicating them to its neighbors.
            Func<ComputeNode, (double[] inValues, int[] counts, double[] outValues)> prepareLocalData = node =>
            {
                Vector localVector = localVectors[node];
                Indexer indexer = indexers[node];
                int numNeighbors = node.Neighbors.Count;

                // Find the common entries (to send) of this node with each of its neighbors, store them contiguously in an 
                // array and store their counts in another array.
                int[] counts = new int[numNeighbors];
                double[] sendValues = indexer.CreateBufferForAllToAllWithNeighbors();
                int sendValuesIdx = 0;
                for (int i = 0; i < numNeighbors; ++i)
                {
                    int[] commonEntries = indexer.NeighborCommonEntries[i];
                    counts[i] = commonEntries.Length;
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

            // Perform AllToAll to exchange the commonda boundary entries of each node with its neighbors.
            environment.NeighborhoodAllToAll(dataPerNode);

            // Add the common entries of neighbors back to the original local vector.
            Action<ComputeNode> sumLocalSubvectors = node =>
            {
                Vector localVector = localVectors[node];
                Indexer indexer = indexers[node];
                int numNeighbors = node.Neighbors.Count;
                (_, _, double[] recvValues) = dataPerNode[node];

                int recvValuesIdx = 0;
                for (int i = 0; i < numNeighbors; ++i)
                {
                    int[] commonEntries = indexer.NeighborCommonEntries[i];
                    for (int j = 0; j < commonEntries.Length; ++j)
                    {
                        localVector[commonEntries[j]] += recvValues[recvValuesIdx];
                    }
                }
            };
        }

        //TODOMPI: the counts array for AllToAll can be cached by the indexer. Also a mapping array can be cached as well to 
        //      simplify the nested for loops required to copy from localVector to sendValues and from recvValues to localVector. 
        public class Indexer
        {
            //TODOMPI: It would be easier if this could be inferred from NeighborCommonEntries
            public List<int[]> BoundaryEntries { get; set; }

            public int[] InternalEntries { get; set; }

            public ComputeNode Node { get; set; }

            public List<int[]> NeighborCommonEntries { get; set; }

            //TODO: cache a buffer for sending and a buffer for receiving inside Indexer (lazily or not) and just return them.
            public double[] CreateBufferForAllToAllWithNeighbors()
            {
                int totalLength = 0;
                foreach (int[] entries in NeighborCommonEntries) totalLength += entries.Length;
                return new double[totalLength];
            }
        }
    }
}
