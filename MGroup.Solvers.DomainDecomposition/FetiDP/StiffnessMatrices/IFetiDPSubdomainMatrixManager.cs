using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Vectors;

namespace MGroup.Solvers.DomainDecomposition.FetiDP.StiffnessMatrices
{
	public interface IFetiDPSubdomainMatrixManager
	{
		IMatrix SchurComplementOfRemainderDofs { get; }

		void CalcSchurComplementOfRemainderDofs();

		void ClearSubMatrices();

		void ExtractKrrKccKrc();

		void HandleDofsWereModified();

		void InvertKrr();

		Vector MultiplyInverseKrrTimes(Vector vector);

		Vector MultiplyKccTimes(Vector vector);

		Vector MultiplyKcrTimes(Vector vector);

		Vector MultiplyKrcTimes(Vector vector);

		void ReorderRemainderDofs();
	}
}
