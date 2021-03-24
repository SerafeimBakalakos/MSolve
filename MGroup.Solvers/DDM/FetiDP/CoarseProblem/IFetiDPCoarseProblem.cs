using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using MGroup.Solvers.DDM.FetiDP.Dofs;
using MGroup.Solvers.DDM.Mappings;

namespace MGroup.Solvers.DDM.FetiDP.CoarseProblem
{
	public interface IFetiDPCoarseProblem
	{
		void ClearCoarseProblemMatrix();

		void CreateAndInvertCoarseProblemMatrix(Dictionary<int, BooleanMatrixRowsToColumns> subdomainLc, 
			Dictionary<int, IMatrix> subdomainKccStar);

		Vector MultiplyInverseCoarseProblemMatrixTimes(Vector vector);

		void ReorderGlobalCornerDofs(IFetiDPDofSeparator dofSeparator);
	}
}
