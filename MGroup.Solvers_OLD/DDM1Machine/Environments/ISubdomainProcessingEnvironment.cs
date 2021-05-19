using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Discretization.Interfaces;

namespace MGroup.Solvers_OLD.DDM.Environments
{
	public interface ISubdomainProcessingEnvironment
	{
		void ExecuteSubdomainAction(IEnumerable<ISubdomain> subdomains, Action<ISubdomain> action);

		void ReduceAddVectors(IEnumerable<Vector> subdomainVectors, Vector result);

		void ReduceAddMatrices(IEnumerable<Matrix> subdomainMatrices, Matrix result);

		void ReduceAxpyVectors(IEnumerable<Vector> subdomainVectorsX, double alpha, Vector y);

	}
}
