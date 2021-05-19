using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Discretization.Interfaces;

namespace MGroup.Solvers_OLD.DDM.Environments
{
	public class SubdomainEnvironmentManagedParallel : ISubdomainProcessingEnvironment
	{
		public void ExecuteSubdomainAction(IEnumerable<ISubdomain> subdomains, Action<ISubdomain> action)
			=> Parallel.ForEach(subdomains, action);

		public void ReduceAddMatrices(IEnumerable<Matrix> subdomainMatrices, Matrix result)
		{
			foreach (Matrix matrix in subdomainMatrices)
			{
				result.AddIntoThis(matrix);
			}
		}

		public void ReduceAddVectors(IEnumerable<Vector> subdomainVectors, Vector result)
		{
			//TODO: The following creates race conditions.
			//Parallel.ForEach(subdomainVectors, vector => result.AddIntoThis(vector));
			foreach (Vector vector in subdomainVectors)
			{
				result.AddIntoThis(vector); // TODO: Temporarily enable BLAS level parallelism
			}
		}

		public void ReduceAxpyVectors(IEnumerable<Vector> subdomainVectorsX, double alpha, Vector y)
		{
			foreach (Vector x in subdomainVectorsX)
			{
				y.AxpyIntoThis(x, alpha);
			}
		}
	}
}
