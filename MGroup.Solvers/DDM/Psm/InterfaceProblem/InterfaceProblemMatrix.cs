using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Discretization.Interfaces;
using MGroup.Solvers.DDM.Environments;
using MGroup.Solvers.DDM.Mappings;
using MGroup.Solvers.DDM.Psm.Dofs;
using MGroup.Solvers.DDM.Psm.StiffnessMatrices;

namespace MGroup.Solvers.DDM.Psm.InterfaceProblem
{
	public class InterfaceProblemMatrix : IInterfaceProblemMatrix
	{
		private readonly IPsmDofSeparator dofSeparator;
		private readonly IDdmEnvironment environment;
		private readonly IPsmMatrixManager matrixManager;
		private readonly IStructuralModel model;

		public InterfaceProblemMatrix(
			IDdmEnvironment environment, IStructuralModel model, IPsmDofSeparator dofSeparator, IPsmMatrixManager matrixManager)
		{
			this.environment = environment;
			this.model = model;
			this.dofSeparator = dofSeparator;
			this.matrixManager = matrixManager;
			NumRows = dofSeparator.GetNumBoundaryDofsCluster(0);
			NumColumns = NumRows;
		}

		public int NumColumns { get; }

		public int NumRows { get; }

		public void Multiply(IVectorView lhsVector, IVector rhsVector)
		{
			// y = S * x = sum{ Lb[s] ^T * Sb[s] * Lb[s] * x}
			var x = (Vector)lhsVector;
			var y = (Vector)rhsVector;

			var subdomainVectors = new List<Vector>();
			Action<ISubdomain> calcSubdomainF = sub =>
			{
				IMappingMatrix Lb = dofSeparator.GetDofMappingBoundaryClusterToSubdomain(sub.ID);
				Vector subU = Lb.Multiply(x, false);
				Vector subF = MultiplySubdomainSchurComplement(sub.ID, subU);
				Vector partialF = Lb.Multiply(subF, true);
				lock (subdomainVectors) subdomainVectors.Add(partialF);
			};
			environment.ExecuteSubdomainAction(model.Subdomains, calcSubdomainF);

			y.Clear();
			environment.ReduceAddVectors(subdomainVectors, y);
		}

		/// <summary>
		/// S[s] * x = (Kbb[s] - Kbi[s] * inv(Kii[s]) * Kib[s]) * x
		/// </summary>
		/// <param name="subdomainID">The ID of a subdomain</param>
		/// <param name="displacements">The displacements that correspond to boundary dofs of this subdomain.</param>
		/// <return>The forces that correspond to boundary dofs of this subdomain.</return>
		public Vector MultiplySubdomainSchurComplement(int subdomainID, Vector displacements)
		{
			Vector forces = matrixManager.MultiplyKbb(subdomainID, displacements);
			Vector temp = matrixManager.MultiplyKib(subdomainID, displacements);
			temp = matrixManager.MultiplyInverseKii(subdomainID, temp);
			temp = matrixManager.MultiplyKbi(subdomainID, temp);
			forces.SubtractIntoThis(temp);
			return forces;
		}
	}
}
