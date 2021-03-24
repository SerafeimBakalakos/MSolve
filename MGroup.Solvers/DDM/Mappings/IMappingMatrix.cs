using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Vectors;

namespace MGroup.Solvers.DDM.Mappings
{
	public interface IMappingMatrix
	{
		int NumColumns { get; }

		int NumRows { get; }

		Matrix CopyToFullMatrix();

		Vector Multiply(Vector vector, bool transpose);
	}
}
