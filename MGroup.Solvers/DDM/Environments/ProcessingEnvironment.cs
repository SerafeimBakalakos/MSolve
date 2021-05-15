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

		public void ExecuteClusterAction(IEnumerable<Cluster> clusters, Action<Cluster> action)
			=> clusterProcessingEnvironment.ExecuteClusterAction(clusters, action);

		public void ExecuteSubdomainAction(IEnumerable<ISubdomain> subdomains, Action<ISubdomain> action)
			=> subdomainProcessingEnvironment.ExecuteSubdomainAction(subdomains, action);

		public void ReduceAddMatrices(IEnumerable<Matrix> subdomainMatrices, Matrix result)
			=> subdomainProcessingEnvironment.ReduceAddMatrices(subdomainMatrices, result);

		public void ReduceAddVectors(IEnumerable<Vector> subdomainVectors, Vector result)
			=> subdomainProcessingEnvironment.ReduceAddVectors(subdomainVectors, result);

		public void ReduceAxpyVectors(IEnumerable<Vector> subdomainVectorsX, double alpha, Vector y)
			=> subdomainProcessingEnvironment.ReduceAxpyVectors(subdomainVectorsX, alpha, y);
	}
}
