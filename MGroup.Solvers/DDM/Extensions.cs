using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.Solvers;
using ISAAR.MSolve.Discretization.FreedomDegrees;

namespace MGroup.Solvers.DDM
{
	public static class Extensions
	{
		public static int GetMultiplicity(this INode node) => node.SubdomainsDictionary.Count;

		/// <summary>
		/// WARNING: Only takes into account dof multiplicity for now.
		/// TODO: Is scaling necessary in PSM? Don't boundary dofs have the same displacements in all subdomains.
		/// TODO: I should probably use Lpb matrices to match theory
		/// TODO: This should probably correct the values of boundary dofs so that they are the same across all subdomains 
		/// </summary>
		public static Vector GatherGlobalDisplacementsOLD(this ISolver solver, IStructuralModel model)
		{
			var globalDisplacements = Vector.CreateZero(model.GlobalDofOrdering.NumGlobalFreeDofs);

			foreach (ISubdomain subdomain in model.Subdomains)
			{
				int id = subdomain.ID;
				int[] subdomainToGlobalDofs = model.GlobalDofOrdering.MapFreeDofsSubdomainToGlobal(subdomain);

				var partialDisplacements = Vector.CreateZero(globalDisplacements.Length);
				IVectorView subdomainDisplacements = solver.LinearSystems[id].Solution;
				foreach ((INode node, IDofType dofType, int subdomainIdx) in subdomain.FreeDofOrdering.FreeDofs)
				{
					int globalIdx = subdomainToGlobalDofs[subdomainIdx];
					partialDisplacements[globalIdx] = subdomainDisplacements[subdomainIdx] / node.SubdomainsDictionary.Count;
				}

				globalDisplacements.AddIntoThis(partialDisplacements);
			}

			return globalDisplacements;
		}
	}
}
