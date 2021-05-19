using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Discretization.Interfaces;

namespace MGroup.Solvers_OLD.DDM.Environments
{
	public class SubdomainEnvironmentManagedSequential : ISubdomainProcessingEnvironment
	{
		public void ExecuteSubdomainAction(IEnumerable<ISubdomain> subdomains, Action<ISubdomain> action)
		{
			foreach (ISubdomain subdomain in subdomains)
			{
				action(subdomain);
			}
		}

		public void ReduceAddMatrices(IEnumerable<Matrix> subdomainMatrices, Matrix result)
		{
			foreach (Matrix matrix in subdomainMatrices)
			{
				result.AddIntoThis(matrix);
			}
		}

		public void ReduceAddVectors(IEnumerable<Vector> subdomainVectors, Vector result)
		{
			foreach (Vector vector in subdomainVectors)
			{
				result.AddIntoThis(vector);
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
