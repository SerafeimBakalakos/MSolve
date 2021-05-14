using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using MGroup.Solvers.Distributed.Environments;
using MGroup.Solvers.Distributed.Topologies;

//TODO: The following boilerplate pattern is rereated in a lot of components and is pretty annoying. It should be handled by 
//		this class somehow. Perhaps model & clusters can be stored in the environment class:
//Action<Cluster> clusterAction = cluster =>
//{
//	Action<ISubdomain> subdomainAction = subdomain =>
//	{
//		Call method that actually computes something local for 1 subdomain
//		Lock Dictionaries that store data for all subdomains and store the computed data from the previous method.
//	};
//	environment.ExecuteSubdomainAction(cluster.Subdomains, subdomainAction);
//};
//environment.ExecuteClusterAction(clusters, clusterAction);
//TODOMPI: IComputeEnvironment stores the ComputeNodes (which correspond to clusters) internally, instead of injecting them in
//		each method. I should probably do the same for clusters and subdomains here.
namespace MGroup.Solvers.DDM.Environments
{
	public interface IDdmEnvironment
	{
		//TODOMPI: remove these. The mapping ComputeNodes <-> Clusters must be done based on the id or by Cluster inderiting from ComputeNode
		ClusterTopology ClusterTopology { get; set; } 
		Cluster GetClusterOfComputeNode(ComputeNode node); //TODOMPI: It will be hard to enforce the use of this all around. Also in many cases, I want the id of the cluster only, because it is not local.


		IComputeEnvironment ComputeEnvironment { get; }

		T BroadcastClusterDataToSubdomains<T>(ISubdomain subdomain, Func<Cluster, T> getCusterData);

		Dictionary<ISubdomain, T> GatherSubdomainDataToCluster<T>(Cluster cluster, Func<ISubdomain, T> getSubdomainData);

		void ExecuteClusterAction(IEnumerable<Cluster> clusters, Action<Cluster> action);

		void ExecuteSubdomainAction(IEnumerable<ISubdomain> subdomains, Action<ISubdomain> action);
		
		void ReduceAddVectors(IEnumerable<Vector> subdomainVectors, Vector result);

		void ReduceAddMatrices(IEnumerable<Matrix> subdomainMatrices, Matrix result);

		void ReduceAxpyVectors(IEnumerable<Vector> subdomainVectorsX, double alpha, Vector y);
	}
}
