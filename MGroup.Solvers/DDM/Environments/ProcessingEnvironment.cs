using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Discretization.Interfaces;
using MGroup.Solvers.Distributed.Environments;
using MGroup.Solvers.Distributed.Topologies;

namespace MGroup.Solvers.DDM.Environments
{
	public class ProcessingEnvironment : IDdmEnvironment
	{
		private readonly ISubdomainProcessingEnvironment subdomainProcessingEnvironment;
		private readonly IClusterProcessingEnvironment clusterProcessingEnvironment;

		public ProcessingEnvironment(ISubdomainProcessingEnvironment subdomainProcessingEnvironment, 
			IClusterProcessingEnvironment clusterProcessingEnvironment)
		{
			this.subdomainProcessingEnvironment = subdomainProcessingEnvironment;
			this.clusterProcessingEnvironment = clusterProcessingEnvironment;
		}

		public ClusterTopology ClusterTopology { get; set; }

		public IComputeEnvironment ComputeEnvironment { get; set; }

		public T BroadcastClusterDataToSubdomains<T>(ISubdomain subdomain, Func<Cluster, T> getCusterData)
		{
			Cluster cluster = ClusterTopology.ClustersOfSubdomains[subdomain];
			return getCusterData(cluster);
		}

		public void ExecuteClusterAction(IEnumerable<Cluster> clusters, Action<Cluster> action)
			=> clusterProcessingEnvironment.ExecuteClusterAction(clusters, action);

		public void ExecuteSubdomainAction(IEnumerable<ISubdomain> subdomains, Action<ISubdomain> action)
			=> subdomainProcessingEnvironment.ExecuteSubdomainAction(subdomains, action);

		public Dictionary<ISubdomain, T> GatherSubdomainDataToCluster<T>(Cluster cluster, Func<ISubdomain, T> getSubdomainData)
		{
			var result = new Dictionary<ISubdomain, T>();
			foreach (ISubdomain sub in cluster.Subdomains)
			{
				result[sub] = getSubdomainData(sub);
			}
			return result;
		}

		public Cluster GetClusterOfComputeNode(ComputeNode node)
		{
			return ClusterTopology.Clusters[node.ID];
		}

		public void ReduceAddMatrices(IEnumerable<Matrix> subdomainMatrices, Matrix result)
			=> subdomainProcessingEnvironment.ReduceAddMatrices(subdomainMatrices, result);

		public void ReduceAddVectors(IEnumerable<Vector> subdomainVectors, Vector result)
			=> subdomainProcessingEnvironment.ReduceAddVectors(subdomainVectors, result);

		public void ReduceAxpyVectors(IEnumerable<Vector> subdomainVectorsX, double alpha, Vector y)
			=> subdomainProcessingEnvironment.ReduceAxpyVectors(subdomainVectorsX, alpha, y);
	}
}
