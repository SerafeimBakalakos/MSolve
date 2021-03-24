using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Vectors;

namespace MGroup.Solvers.DDM.Environments
{
	public interface IProcessingEnvironment
	{
		void ExecuteClusterAction(IEnumerable<Cluster> clusters, Action<Cluster> action);

		void ExecuteSubdomainAction(IEnumerable<ISubdomain> subdomains, Action<ISubdomain> action);

		void ReduceAddVectors(IEnumerable<Vector> subdomainVectors, Vector result);

		void ReduceAddMatrices(IEnumerable<Matrix> subdomainMatrices, Matrix result);

		void ReduceAxpyVectors(IEnumerable<Vector> subdomainVectorsX, double alpha, Vector y);

	}
}
