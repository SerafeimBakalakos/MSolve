using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using MGroup.Solvers_OLD.DistributedTry1.Distributed.Environments;
using MGroup.Solvers_OLD.DistributedTry1.Distributed.Topologies;

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
namespace MGroup.Solvers_OLD.DDM.Environments
{
	public interface IDdmEnvironment
	{
		void ExecuteClusterAction(IEnumerable<Cluster> clusters, Action<Cluster> action);

		void ExecuteSubdomainAction(IEnumerable<ISubdomain> subdomains, Action<ISubdomain> action);
		
		void ReduceAddVectors(IEnumerable<Vector> subdomainVectors, Vector result);

		void ReduceAddMatrices(IEnumerable<Matrix> subdomainMatrices, Matrix result);

		void ReduceAxpyVectors(IEnumerable<Vector> subdomainVectorsX, double alpha, Vector y);
	}
}