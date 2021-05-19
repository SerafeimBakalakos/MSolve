using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Discretization.Interfaces;
using MGroup.Solvers_OLD.DDM.Environments;
using MGroup.Solvers_OLD.DDM.FetiDP.CoarseProblem;
using MGroup.Solvers_OLD.DDM.FetiDP.Dofs;
using MGroup.Solvers_OLD.DDM.FetiDP.StiffnessMatrices;
using MGroup.Solvers_OLD.DDM.Mappings;
using MGroup.Solvers_OLD.DDM.PFetiDP.Dofs;
using MGroup.Solvers_OLD.DDM.Psm.Dofs;
using MGroup.Solvers_OLD.DDM.Psm.InterfaceProblem;
using MGroup.Solvers_OLD.DDM.Psm.Preconditioner;

//WARNING: inv(Krr)* Lpr * x is performed twice
//WARNING: other important optimizations can be done: w2 and v5 both involve Lpr * some vector.First add these vectors and then apply Lpr mapping
namespace MGroup.Solvers_OLD.DDM.PFetiDP.Preconditioner
{
	/// <summary>
	/// Implements equation 6.1 of Papagiannakis bachelor thesis:
	/// inv(Aprec) = (Lpr^e)^T*inv(Krr^e)*Lpr^e + (bNbc - (Lpr^e)^T*inv(Krr^e)*Krc^e*Lc^e) * inv(Kcc^*) (-(Lc^e)^T * Kcr^e * inv(Krr^e) * Lpr^e + bcNb)
	/// inv(Aprec) will be multiplied with a vector: y = inv(Aprec) * x and the operations above will be performed as matrix-vector 
	/// multiplications from right to left. Let us name some vectors:
	/// x = left hand side vector coming from PCG/GMRES/etc: (length = global boundary dofs)
	/// v0 = cNb * x (length = global corner dofs)
	/// v1 = inv(Krr^e) * Lpr^e * x (per subomain: length = num remainder dofs)
	/// v2 = v0 -(Lc^e)^T * Kcr^e * v1 (length = global corner dofs)
	/// v3 = inv(Kcc^*) * v2: This is the coarse problem of FETI-DP (length = global corner dofs)
	/// v4 = bNc * v3 (length = global boundary dofs)
	/// v5 = inv(Krr^e)*Krc^e*Lc^e * v3 (per subomain: length = num remainder dofs)
	/// v6 = (Lpr^e)^T * (v1 - v5) (length = global boundary dofs)
	/// y = v4 + v6. y = right hand side vector returning to PCG/GMRES/etc: (length = global boundary dofs)
	/// </summary>
	public class PFetiDPPreconditioner : IPsmPreconditioner
	{
		private readonly IDdmEnvironment environment;
		private readonly IStructuralModel model;
		private readonly IList<Cluster> clusters;
		private readonly IPsmDofSeparator psmDofSeparator;
		private readonly IFetiDPDofSeparator fetiDPDofSeparator;
		private readonly IPFetiDPDofSeparator pFetiDPDofSeparator;
		private readonly IFetiDPMatrixManager fetiDPMatrixManager;
		private readonly IFetiDPCoarseProblem fetiDPCoarseProblem;

		public PFetiDPPreconditioner(IDdmEnvironment environment, IStructuralModel model, IList<Cluster> clusters,
			IPsmDofSeparator psmDofSeparator, IFetiDPDofSeparator fetiDPDofSeparator, IPFetiDPDofSeparator pFetiDPDofSeparator,
			IFetiDPMatrixManager fetiDPMatrixManager, IFetiDPCoarseProblem fetiDPCoarseProblem)
		{
			this.environment = environment;
			this.model = model;

			if (clusters.Count > 1)
			{
				throw new NotImplementedException();
			}
			this.clusters = clusters;
			this.psmDofSeparator = psmDofSeparator;
			this.fetiDPDofSeparator = fetiDPDofSeparator;
			this.pFetiDPDofSeparator = pFetiDPDofSeparator;
			this.fetiDPMatrixManager = fetiDPMatrixManager;
			this.fetiDPCoarseProblem = fetiDPCoarseProblem;
		}

		public void Calculate(IInterfaceProblemMatrix interfaceProblemMatrix)
		{
		}

		public void SolveLinearSystem(IVectorView rhsVector, IVector lhsVector)
		{
			Vector x = (Vector)rhsVector;
			Vector y = (Vector)lhsVector;

			// v0 = cNb * x (length = global corner dofs)
			int c = clusters.First().ID;
			IMappingMatrix bNc = pFetiDPDofSeparator.GetDofMappingGlobalCornerToClusterBoundary(c);
			Vector v0 = bNc.Multiply(x, true);

			// v1 = inv(Krr^e) * Lpr^e * x (per subomain: length = num remainder dofs)
			Dictionary<int, Vector> v1 = CalcV1(x);

			// v2 = v0 -(Lc^e)^T * Kcr^e * v1(length = global corner dofs)
			Vector v2 = v0;
			CalcV2(v1, v2);

			// v3 = inv(Kcc^*) * v2: This is the coarse problem of FETI-DP (length = global corner dofs)
			Vector v3 = fetiDPCoarseProblem.MultiplyInverseCoarseProblemMatrixTimes(v2);

			// v4 = bNc * v3 (length = global boundary dofs)
			Vector v4 = bNc.Multiply(v3, false);

			// v5 = inv(Krr^e)*Krc^e*Lc^e * v3 (per subomain: length = num remainder dofs)
			Dictionary<int, Vector> v5 = CalcV5(v3);

			// v6 = (Lpr^e)^T * (v1 - v5) (length = global boundary dofs)
			Vector v6 = CalcV6(v1, v5);

			// y = v4 + v6. y = right hand side vector returning to PCG/GMRES/etc: (length = global boundary dofs)
			y.CopyFrom(v4);
			y.AddIntoThis(v6);
		}

		private Dictionary<int, Vector> CalcV1(Vector x)
		{
			// v1 = inv(Krr^e) * Lpr^e * x (per subomain: length = num remainder dofs)
			var v1 = new Dictionary<int, Vector>();
			Action<ISubdomain> subdomainAction = subdomain =>
			{
				int s = subdomain.ID;
				IMappingMatrix Lpr = pFetiDPDofSeparator.GetDofMappingBoundaryClusterToSubdomainRemainder(s);
				Vector temp = Lpr.Multiply(x, false);
				temp = fetiDPMatrixManager.MultiplyInverseKrrTimes(s, temp);
				lock (v1) v1[s] = temp;
			};
			environment.ExecuteSubdomainAction(model.Subdomains, subdomainAction);
			return v1;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="v1"></param>
		/// <param name="v2">When the method is called, v0 should be passed in</param>
		private void CalcV2(Dictionary<int, Vector> v1, Vector v2)
		{
			// v2 = v0 -(Lc^e)^T * Kcr^e * v1(length = global corner dofs)
			var subdomainVectors = new List<Vector>();
			Action<ISubdomain> subdomainAction = subdomain =>
			{
				int s = subdomain.ID;
				IMappingMatrix Lc = fetiDPDofSeparator.GetDofMappingCornerGlobalToSubdomain(s);
				Vector temp = fetiDPMatrixManager.MultiplyKcrTimes(s, v1[s]);
				temp = Lc.Multiply(temp, true);
				lock (subdomainVectors) subdomainVectors.Add(temp);
			};
			environment.ExecuteSubdomainAction(model.Subdomains, subdomainAction);
			environment.ReduceAxpyVectors(subdomainVectors, -1, v2);
		}

		private Dictionary<int, Vector> CalcV5(Vector v2)
		{
			// v5 = inv(Krr^e)*Krc^e*Lc^e * v3 (per subomain: length = num remainder dofs)
			var v5 = new Dictionary<int, Vector>();
			Action<ISubdomain> subdomainAction = subdomain =>
			{
				int s = subdomain.ID;
				IMappingMatrix Lc = fetiDPDofSeparator.GetDofMappingCornerGlobalToSubdomain(s);
				Vector temp = Lc.Multiply(v2, false);
				temp = fetiDPMatrixManager.MultiplyKrcTimes(s, temp);
				temp = fetiDPMatrixManager.MultiplyInverseKrrTimes(s, temp);
				lock (v5) v5[s] = temp;
			};
			environment.ExecuteSubdomainAction(model.Subdomains, subdomainAction);
			return v5;
		}

		/// <summary>
		/// </summary>
		/// <param name="v1">Will be overwritten and thus made unavailable</param>
		/// <param name="v5">Will be made unavailable</param>
		/// <returns></returns>
		private Vector CalcV6(Dictionary<int, Vector> v1, Dictionary<int, Vector> v5)
		{
			// v6 = (Lpr^e)^T * (v1 - v5) (length = global boundary dofs)
			var subdomainVectors = new List<Vector>();
			Action<ISubdomain> subdomainAction = subdomain =>
			{
				int s = subdomain.ID;
				IMappingMatrix Lpr = pFetiDPDofSeparator.GetDofMappingBoundaryClusterToSubdomainRemainder(s);
				v1[s].SubtractIntoThis(v5[s]);
				Vector temp = Lpr.Multiply(v1[s], true);
				lock (subdomainVectors) subdomainVectors.Add(temp);
				//v6.AddIntoThis(temp);
			};
			environment.ExecuteSubdomainAction(model.Subdomains, subdomainAction);

			int c = clusters.First().ID;
			var v6 = Vector.CreateZero(psmDofSeparator.GetNumBoundaryDofsCluster(c));
			environment.ReduceAddVectors(subdomainVectors, v6);

			v1.Clear();
			v5.Clear();
			return v6;
		}
	}
}
