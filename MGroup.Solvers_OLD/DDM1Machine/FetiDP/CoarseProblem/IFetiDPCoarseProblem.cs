using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using MGroup.Solvers_OLD.DDM.FetiDP.Dofs;
using MGroup.Solvers_OLD.DDM.Mappings;

namespace MGroup.Solvers_OLD.DDM.FetiDP.CoarseProblem
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
