using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Discretization.Interfaces;
using MGroup.Solvers.DDM.Environments;
using MGroup.Solvers.DDM.FetiDP.Dofs;
using MGroup.Solvers.DDM.Mappings;

namespace MGroup.Solvers.DDM.FetiDP.CoarseProblem
{
	public class FetiDPCoarseProblemDense : IFetiDPCoarseProblem
	{
		private readonly IDdmEnvironment environment;
		private readonly IStructuralModel model;
		private Matrix inverseGlobalKccStar;

		public FetiDPCoarseProblemDense(IDdmEnvironment environment, IStructuralModel model)
		{
			this.environment = environment;
			this.model = model;
		}

		public void ClearCoarseProblemMatrix()
		{
			inverseGlobalKccStar = null;
		}

		public void CreateAndInvertCoarseProblemMatrix(Dictionary<int, BooleanMatrixRowsToColumns> subdomainLc,
			Dictionary<int, IMatrix> subdomainKccStar)
		{
			int numGlobalCornerDofs = subdomainLc.First().Value.NumColumns;

			// Static condensation of remainder dofs (Schur complement).
			var subdomainMatrices = new List<Matrix>();
			Action<ISubdomain> subdomainAction = subdomain =>
			{
				// globalKccStar = sum_over_s(Lc[s]^T * KccStar[s] * Lc[s])
				int s = subdomain.ID;
				Matrix Lc = subdomainLc[s].CopyToFullMatrix();
				IMatrix KccStar = subdomainKccStar[s];
				Matrix temp = Lc.ThisTransposeTimesOtherTimesThis(KccStar);
				lock (subdomainMatrices) subdomainMatrices.Add(temp);
			};
			environment.ExecuteSubdomainAction(model.Subdomains, subdomainAction);

			var globalKccStar = Matrix.CreateZero(numGlobalCornerDofs, numGlobalCornerDofs);
			environment.ReduceAddMatrices(subdomainMatrices, globalKccStar);

			globalKccStar.InvertInPlace();
			this.inverseGlobalKccStar = globalKccStar;
		}

		public Vector MultiplyInverseCoarseProblemMatrixTimes(Vector vector) => inverseGlobalKccStar * vector;

		public void ReorderGlobalCornerDofs(IFetiDPDofSeparator dofSeparator)
		{
			// Do nothing, since the sparsity pattern is irrelevant for dense matrices.
		}

		public class Factory : IFetiDPCoarseProblemFactory
		{
			public IFetiDPCoarseProblem Create(IDdmEnvironment environment, IStructuralModel model)
				=> new FetiDPCoarseProblemDense(environment, model);
		}
	}
}
