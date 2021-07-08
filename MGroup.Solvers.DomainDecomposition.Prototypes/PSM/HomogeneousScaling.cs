using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Commons;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.LinearAlgebra.Vectors;

namespace MGroup.Solvers.DomainDecomposition.Prototypes.PSM
{
    public class HomogeneousScaling : IPrimalScaling
    {
		private readonly IStructuralModel model;

		public HomogeneousScaling(IStructuralModel model)
		{
			this.model = model;
		}

		public Dictionary<int, SparseVector> DistributeNodalLoads(Table<INode, IDofType, double> nodalLoads)
        {
			Func<int, SparseVector> calcSubdomainForces = subdomainID =>
			{
				ISubdomain subdomain = model.GetSubdomain(subdomainID);
				DofTable freeDofs = subdomain.FreeDofOrdering.FreeDofs;

				//TODO: I go through every node and ignore the ones that are not loaded. 
				//		It would be better to directly access the loaded ones.
				var nonZeroLoads = new SortedDictionary<int, double>();
				foreach (INode node in subdomain.Nodes)
				{
					bool isLoaded = nodalLoads.TryGetDataOfRow(node, out IReadOnlyDictionary<IDofType, double> loadsOfNode);
					if (!isLoaded) continue;

					foreach (var dofLoadPair in loadsOfNode)
					{
						int freeDofIdx = freeDofs[node, dofLoadPair.Key];
						nonZeroLoads[freeDofIdx] = dofLoadPair.Value / node.SubdomainsDictionary.Count;
					}
				}

				return SparseVector.CreateFromDictionary(subdomain.FreeDofOrdering.NumFreeDofs, nonZeroLoads);
			};

			var results = new Dictionary<int, SparseVector>();
			foreach (ISubdomain subdomain in model.Subdomains)
			{
				results[subdomain.ID] = calcSubdomainForces(subdomain.ID);
			}
			return results;
		}
    }
}
