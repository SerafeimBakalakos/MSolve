using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using MGroup.Solvers.DomainDecomposition.StiffnessMatrices;

namespace MGroup.Solvers.DomainDecomposition.PSM.StiffnessMatrices
{
	public interface IPsmMatrixManager
	{
		IMatrixView CalcSchurComplement(int subdomainID);

		void ClearSubMatrices(int subdomainID);

		void ExtractKiiKbbKib(int subdomainID);

		void InvertKii(int subdomainID);

		Vector MultiplyInverseKii(int subdomainID, Vector vector);

		Vector MultiplyKbb(int subdomainID, Vector vector);

		Vector MultiplyKbi(int subdomainID, Vector vector);

		Vector MultiplyKib(int subdomainID, Vector vector);

		void ReorderInternalDofs(int subdomainID);
	}
}
