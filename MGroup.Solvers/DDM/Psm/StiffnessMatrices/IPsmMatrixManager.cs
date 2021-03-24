using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using MGroup.Solvers.DDM.StiffnessMatrices;

namespace MGroup.Solvers.DDM.Psm.StiffnessMatrices
{
	public interface IPsmMatrixManager
	{
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
