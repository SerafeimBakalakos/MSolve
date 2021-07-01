using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using MGroup.Solvers.DomainDecomposition.StiffnessMatrices;

namespace MGroup.Solvers.DomainDecomposition.PSM.StiffnessMatrices
{
	public interface IPsmSubdomainMatrixManager
	{
		IMatrixView CalcSchurComplement();

		void ClearSubMatrices();

		void ExtractKiiKbbKib();

		void HandleDofsWereModified();

		void InvertKii();

		Vector MultiplyInverseKii(Vector vector);

		Vector MultiplyKbb(Vector vector);

		Vector MultiplyKbi(Vector vector);

		Vector MultiplyKib(Vector vector);

		void ReorderInternalDofs();
	}
}
