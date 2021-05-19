using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Discretization.Interfaces;
using MGroup.Solvers_OLD.DDM.Environments;
using MGroup.Solvers_OLD.DDM.Mappings;
using MGroup.Solvers_OLD.DDM.PFetiDP.Dofs;
using MGroup.Solvers_OLD.DDM.Psm.Dofs;
using ISAAR.MSolve.Discretization.Commons;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using MGroup.Solvers_OLD.DistributedTry1.Distributed.Environments;
using MGroup.Solvers_OLD.DistributedTry1.Distributed.Topologies;
using System.Collections.Concurrent;

namespace MGroup.Solvers_OLD.DistributedTry1.DDM.Psm.StiffnessDistribution
{
	public class HomogeneousStiffnessDistribution_NEW : IStiffnessDistribution_NEW
	{
		private readonly IComputeEnvironment environment;
		private readonly IStructuralModel model;
		private readonly ClusterTopology clusterTopology;
		private readonly IPsmDofSeparator dofSeparator;
		private readonly ConcurrentDictionary<int, IMappingMatrix> dofMappingBoundaryClusterToSubdomain = 
			new ConcurrentDictionary<int, IMappingMatrix>();
		private readonly ConcurrentDictionary<int, double[]> inverseMultiplicities = new ConcurrentDictionary<int, double[]>();

		public HomogeneousStiffnessDistribution_NEW(IComputeEnvironment environment, IStructuralModel model, 
			ClusterTopology clusterTopology, IPsmDofSeparator dofSeparator)
		{
			this.environment = environment;
			this.model = model;
			this.clusterTopology = clusterTopology;
			this.dofSeparator = dofSeparator;
		}

		public void CalcSubdomainScaling()
		{
			Action<ComputeSubnode> calcSubdomainLpb = computeSubnode =>
			{
				ISubdomain subdomain = model.GetSubdomain(computeSubnode.ID);
				BooleanMatrixRowsToColumns Lb = dofSeparator.GetDofMappingBoundaryClusterToSubdomain(subdomain.ID);
				var boundaryDofs = dofSeparator.GetSubdomainDofOrderingBoundary(subdomain.ID);

				var subdomainW = new double[Lb.NumRows];
				foreach ((INode node, IDofType dof, int idx) in boundaryDofs)
				{
					subdomainW[idx] = 1.0 / node.GetMultiplicity();
				}

				var Lpb = new ScalingMatrixRowMajor(Lb.NumRows, Lb.NumColumns, Lb.RowsToColumns, subdomainW);
				dofMappingBoundaryClusterToSubdomain[subdomain.ID] = Lpb;
				inverseMultiplicities[subdomain.ID] = subdomainW;
			};
			environment.DoPerSubnode(calcSubdomainLpb);
		}

		public IMappingMatrix GetDofMappingBoundaryClusterToSubdomain(int subdomainID) 
			=> dofMappingBoundaryClusterToSubdomain[subdomainID];

		public Dictionary<int, SparseVector> DistributeNodalLoads(Table<INode, IDofType, double> nodalLoads)
		{
			Func<ComputeSubnode, SparseVector> calcSubdomainForces = computeSubnode =>
			{
				ISubdomain subdomain = model.GetSubdomain(computeSubnode.ID);
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
						nonZeroLoads[freeDofIdx] = dofLoadPair.Value / node.GetMultiplicity();
					}
				}

				return SparseVector.CreateFromDictionary(subdomain.FreeDofOrdering.NumFreeDofs, nonZeroLoads);
			};
			return environment.CreateDictionaryPerSubnode(calcSubdomainForces);
		}

		public void ScaleForceVector(int subdomainID, Vector subdomainForces)
		{
			int[] boundaryDofs = dofSeparator.GetSubdomainDofsBoundaryToFree(subdomainID);
			double[] relativeStiffnessOfSubdomain = inverseMultiplicities[subdomainID];
			for (int i = 0; i < boundaryDofs.Length; i++)
			{
				double coeff = relativeStiffnessOfSubdomain[i];
				subdomainForces[boundaryDofs[i]] *= coeff;
			}
		}
	}
}
