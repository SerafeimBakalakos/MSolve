using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.Discretization.Commons;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using System.Collections.Concurrent;
using MGroup.Environments;
using MGroup.Solvers.DomainDecomposition.PSM.Dofs;
using MGroup.Solvers.DomainDecomposition.Mappings;
using MGroup.LinearAlgebra.Distributed.Overlapping;

namespace MGroup.Solvers.DomainDecomposition.PSM.StiffnessDistribution
{
	public class HomogeneousStiffnessDistribution : IStiffnessDistribution
	{
		private readonly IComputeEnvironment environment;
		private readonly IStructuralModel model;
		private readonly IPsmDofSeparator dofSeparator;
		private readonly ConcurrentDictionary<int, double[]> inverseMultiplicities = new ConcurrentDictionary<int, double[]>();
		//private readonly ConcurrentDictionary<int, IMappingMatrix> dofMappingBoundaryClusterToSubdomain = 
		//	new ConcurrentDictionary<int, IMappingMatrix>();

		public HomogeneousStiffnessDistribution(IComputeEnvironment environment, IStructuralModel model, 
			IPsmDofSeparator dofSeparator)
		{
			this.environment = environment;
			this.model = model;
			this.dofSeparator = dofSeparator;
		}

		public void CalcSubdomainScaling(DistributedOverlappingIndexer indexer)
		{
			Action<int> calcSubdomainScaling = subdomainID =>
			{
				ISubdomain subdomain = model.GetSubdomain(subdomainID);
				int numBoundaryDofs = dofSeparator.GetSubdomainDofsBoundaryToFree(subdomainID).Length;

				var subdomainW = new double[numBoundaryDofs];
				int[] multiplicites = indexer.GetLocalComponent(subdomainID).Multiplicities;
				for (int i = 0; i < numBoundaryDofs; ++i)
				{
					subdomainW[i] = 1.0 / multiplicites[i];
				}

				//DofTable boundaryDofs = dofSeparator.GetSubdomainDofOrderingBoundary(subdomainID);
				//foreach ((INode node, IDofType dof, int idx) in boundaryDofs)
				//{
				//	subdomainW[idx] = 1.0 / node.SubdomainsDictionary.Count;
				//}

				inverseMultiplicities[subdomain.ID] = subdomainW;

				//BooleanMatrixRowsToColumns Lb = dofSeparator.GetDofMappingBoundaryClusterToSubdomain(subdomain.ID);
				//var Lpb = new ScalingMatrixRowMajor(Lb.NumRows, Lb.NumColumns, Lb.RowsToColumns, subdomainW);
				//dofMappingBoundaryClusterToSubdomain[subdomain.ID] = Lpb;
			};
			environment.DoPerNode(calcSubdomainScaling);
		}

		//public IMappingMatrix GetDofMappingBoundaryClusterToSubdomain(int subdomainID) 
		//	=> dofMappingBoundaryClusterToSubdomain[subdomainID];

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
			return environment.CreateDictionaryPerNode(calcSubdomainForces);
		}

		public void ScaleForceVector(int subdomainID, Vector subdomainForces)
		{
			int[] boundaryDofs = dofSeparator.GetSubdomainDofsBoundaryToFree(subdomainID);
			double[] coefficients = inverseMultiplicities[subdomainID];
			for (int i = 0; i < boundaryDofs.Length; i++)
			{
				double coeff = coefficients[i];
				subdomainForces[boundaryDofs[i]] *= coeff;
			}
		}
	}
}