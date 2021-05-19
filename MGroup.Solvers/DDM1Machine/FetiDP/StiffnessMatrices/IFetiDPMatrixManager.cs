using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using MGroup.Solvers_OLD.DDM.FetiDP.Dofs;
using MGroup.Solvers_OLD.DDM.StiffnessMatrices;
using MGroup.Solvers_OLD.DofOrdering.Reordering;

namespace MGroup.Solvers_OLD.DDM.FetiDP.StiffnessMatrices
{
	public interface IFetiDPMatrixManager
	{
		IMatrix GetSchurComplementOfRemainderDofs(int subdomainID);

		void CalcSchurComplementOfRemainderDofs(int subdomainID);

		void ClearSubMatrices(int subdomainID);

		void ExtractKrrKccKrc(int subdomainID);

		void InvertKrr(int subdomainID);

		Vector MultiplyInverseKrrTimes(int subdomainID, Vector vector);

		Vector MultiplyKccTimes(int subdomainID, Vector vector);

		Vector MultiplyKcrTimes(int subdomainID, Vector vector);

		Vector MultiplyKrcTimes(int subdomainID, Vector vector);

		void ReorderRemainderDofs(int subdomainID);
	}
}
