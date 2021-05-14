using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;
using MGroup.Solvers.DDM.Environments;
using MGroup.Solvers.Distributed.Topologies;

namespace MGroup.Solvers.DDM
{
    public class ClusterTopology
    {
        private readonly IDdmEnvironment environment;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="environment"></param>
        public ClusterTopology(IDdmEnvironment environment)
        {
            this.environment = environment;
        }

        public Dictionary<int, Cluster> Clusters { get; } = new Dictionary<int, Cluster>();

        public Dictionary<ISubdomain, Cluster> ClustersOfSubdomains { get; } = new Dictionary<ISubdomain, Cluster>();

        public void FindClustersOfSubdomains()
        {
            Action<ComputeNode> associateClustersToSubdomains = computeNode =>
            {
                Cluster cluster = Clusters[computeNode.ID];
                foreach (ISubdomain subdomain in cluster.Subdomains)
                {
                    ClustersOfSubdomains.Add(subdomain, cluster); // No overlapping allowed
                }
            };
            environment.ComputeEnvironment.DoPerNode(associateClustersToSubdomains);
        }

        public void FindNeighboringClusters()
        {
            if (environment.ComputeEnvironment.NumComputeNodes == 1) return;

            Action<ComputeNode> findNeighbors = computeNode =>
            {
                Cluster cluster = Clusters[computeNode.ID];
                foreach (ISubdomain subdomain in cluster.Subdomains) //TODOMPI: Parallelize this (will need locking)
                {
                    foreach (INode node in subdomain.Nodes)
                    {
                        if (node.GetMultiplicity() == 1) continue; // internal node

                        HashSet<int> clustersOfNode = FindClustersOfNode(node);
                        clustersOfNode.Remove(cluster.ID);
                        if (clustersOfNode.Count == 0) continue; // Boundary node between subdomains of this cluster

                        // Boundary node between subdomains of different clusters. Find them.
                        foreach (int otherCluster in clustersOfNode)
                        {
                            //TODOMPI: Let Cluster take care of this (e.g. cluster.AddCommonNode(int otherCluster, int commonNode)
                            bool alreadyNeghbor = cluster.InterClusterNodes.TryGetValue(otherCluster,
                                out SortedSet<int> commonNodes);
                            if (!alreadyNeghbor)
                            {
                                commonNodes = new SortedSet<int>();
                                cluster.InterClusterNodes[otherCluster] = commonNodes;
                            }
                            commonNodes.Add(node.ID);
                        }
                    }
                }
            };
            environment.ComputeEnvironment.DoPerNode(findNeighbors);
        }
        
        private HashSet<int> FindClustersOfNode(INode node)
        {
            var clustersOfNode = new HashSet<int>();
            foreach (ISubdomain subdomain in node.SubdomainsDictionary.Values)
            {
                clustersOfNode.Add(ClustersOfSubdomains[subdomain].ID);
            }
            return clustersOfNode;
        }
    }
}
