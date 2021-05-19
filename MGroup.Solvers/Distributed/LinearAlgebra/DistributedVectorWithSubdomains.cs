using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Exceptions;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using MGroup.Solvers.Distributed.Environments;
using MGroup.Solvers.Distributed.Topologies;

namespace MGroup.Solvers.Distributed.LinearAlgebra
{
    public class DistributedVectorWithSubdomains
    {
        private readonly IComputeEnvironment environment;
        private readonly DistributedIndexerWithSubdomains indexer;

        public DistributedVectorWithSubdomains(IComputeEnvironment environment, DistributedIndexerWithSubdomains indexer)
        {
            this.environment = environment;
            this.indexer = indexer;
            this.LocalVectors = environment.CreateDictionaryPerSubnode(
                subnode => Vector.CreateZero(indexer.GetNumEntries(subnode)));
        }

        public DistributedVectorWithSubdomains(IComputeEnvironment environment, DistributedIndexerWithSubdomains indexer,
            Dictionary<int, Vector> localVectors)
        {
            this.environment = environment;
            this.indexer = indexer;
            this.LocalVectors = localVectors;
        }

        public Dictionary<int, Vector> LocalVectors { get; }

        public void CopyFromClusterVector(DistributedOverlappingVector clusterVector)
        {
            Action<ComputeSubnode> subdomainAction = computeSubnode =>
            {
                Vector clusterLocalVector = environment.AccessNodeDataFromSubnode(computeSubnode, 
                    node => clusterVector.LocalVectors[node]);
                Vector subdomainVector = this.LocalVectors[computeSubnode.ID];
                int[] subdomainToClusterEntries = indexer.MapSubdomainToClusterEntries(computeSubnode);
                subdomainVector.CopyNonContiguouslyFrom(clusterLocalVector, subdomainToClusterEntries);
            };
            environment.DoPerSubnode(subdomainAction);
        }

        public void Clear()
        {
            environment.DoPerSubnode(subnode => LocalVectors[subnode.ID].Clear());
        }

        public DistributedVectorWithSubdomains Copy()
        {
            Dictionary<int, Vector> localVectorsCloned = 
                environment.CreateDictionaryPerSubnode(subnode => LocalVectors[subnode.ID].Copy());
            return new DistributedVectorWithSubdomains(environment, indexer, localVectorsCloned);
        }

        public void CopyFrom(DistributedVectorWithSubdomains other)
        {
            Debug.Assert((this.environment == other.environment) && (this.indexer == other.indexer));
            environment.DoPerSubnode(subnode => this.LocalVectors[subnode.ID].CopyFrom(other.LocalVectors[subnode.ID]));
        }

        public DistributedVectorWithSubdomains CreateZeroVectorWithSameFormat()
        {
            return new DistributedVectorWithSubdomains(environment, indexer);
        }

        public DistributedOverlappingVector SumToClusterVector()
        {
            var result = new DistributedOverlappingVector(environment, indexer.ClusterIndexer);
            Action<ComputeNode> clusterAction = computeNode =>
            {
                Vector clusterVector = result.LocalVectors[computeNode];
                foreach (ComputeSubnode computeSubnode in computeNode.Subnodes.Values)
                {
                    int[] subdomainToClusterEntries = environment.AccessSubnodeDataFromNode(computeSubnode,
                        subnode => indexer.MapSubdomainToClusterEntries(subnode));
                    Vector subdomainVector = environment.AccessSubnodeDataFromNode(computeSubnode,
                        subnode => this.LocalVectors[subnode.ID]);

                    clusterVector.AddIntoThisNonContiguouslyFrom(subdomainToClusterEntries, subdomainVector);
                }
            };
            environment.DoPerNode(clusterAction);

            //TODOMPI: Should the client do this? Subdomain -> cluster is a summing operation, so why not sum 
            //      cluster-cluster common entries as well?
            result.SumOverlappingEntries();

            return result;
        }
    }
}
