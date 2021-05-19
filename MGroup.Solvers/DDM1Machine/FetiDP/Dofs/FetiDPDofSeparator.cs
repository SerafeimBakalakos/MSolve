using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Discretization.Interfaces;
using MGroup.Solvers_OLD.DDM.Environments;
using MGroup.Solvers_OLD.DDM.Mappings;
using MGroup.Solvers_OLD.DofOrdering.Reordering;

namespace MGroup.Solvers_OLD.DDM.FetiDP.Dofs
{
	public class FetiDPDofSeparator : IFetiDPDofSeparator
	{
		private readonly IDdmEnvironment environment;
		private readonly IStructuralModel model;
		private readonly IList<Cluster> clusters;

		private readonly Dictionary<int, DofTable> subdomainDofOrderingsCorner = new Dictionary<int, DofTable>();
		private readonly Dictionary<int, int[]> subdomainDofsBoundaryRemainderToRemainder = new Dictionary<int, int[]>();
		private readonly Dictionary<int, int[]> subdomainDofsCornerToFree = new Dictionary<int, int[]>();
		private readonly Dictionary<int, int[]> subdomainDofsInternalToRemainder = new Dictionary<int, int[]>();
		private readonly Dictionary<int, int[]> subdomainDofsRemainderToFree = new Dictionary<int, int[]>();
		private Dictionary<int, BooleanMatrixRowsToColumns> subdomainToGlobalCornerMappings =
			new Dictionary<int, BooleanMatrixRowsToColumns>();
		private readonly Dictionary<int, int> subdomainNumFreeDofs = new Dictionary<int, int>();

		public FetiDPDofSeparator(IDdmEnvironment environment, IStructuralModel model, IList<Cluster> clusters)
		{
			this.environment = environment;
			this.model = model;
			this.clusters = clusters;

			foreach (Cluster cluster in clusters)
			{
				foreach (ISubdomain subdomain in cluster.Subdomains)
				{
					subdomainDofOrderingsCorner[subdomain.ID] = null;
				}
			}
		}

		public DofTable GlobalCornerDofOrdering { get; private set; }

		public int NumGlobalCornerDofs { get; private set; }

		public DofTable GetDofOrderingCorner(int subdomainID) => subdomainDofOrderingsCorner[subdomainID];

		public int[] GetDofsBoundaryRemainderToRemainder(int subdomainID) 
			=> subdomainDofsBoundaryRemainderToRemainder[subdomainID];

		public int[] GetDofsCornerToFree(int subdomainID) => subdomainDofsCornerToFree[subdomainID];

		public int[] GetDofsInternalToRemainder(int subdomainID) => subdomainDofsInternalToRemainder[subdomainID];

		public int[] GetDofsRemainderToFree(int subdomainID) => subdomainDofsRemainderToFree[subdomainID];

		public int GetNumFreeDofs(int subdomainID) => subdomainNumFreeDofs[subdomainID];

		public BooleanMatrixRowsToColumns GetDofMappingCornerGlobalToSubdomain(int subdomainID)
			=> subdomainToGlobalCornerMappings[subdomainID];

		/// <summary>
		/// Lc mappings: subdomain to/from global
		/// </summary>
		public void MapCornerDofs()
		{
			subdomainToGlobalCornerMappings = new Dictionary<int, BooleanMatrixRowsToColumns>();
			//TODOMPI: not sure about this design (subdomainAction in a clusterAction). It works for subdomain-cluster operations
			//		but here we have subdomain-global operations.
			Action<Cluster> clusterAction = cluster => 
			{
				Action<ISubdomain> subdomanAction = subdomain =>
				{
					int s = subdomain.ID;
					int numSubdomainCornerDofs = subdomainDofsCornerToFree[s].Length;
					var Lc = new int[numSubdomainCornerDofs];
					foreach ((INode node, IDofType dof, int subdomainIdx) in subdomainDofOrderingsCorner[s])
					{
						int globalIdx = GlobalCornerDofOrdering[node, dof];
						Lc[subdomainIdx] = globalIdx;
					}
					var matrixLc = new BooleanMatrixRowsToColumns(numSubdomainCornerDofs, NumGlobalCornerDofs, Lc);
					lock (subdomainToGlobalCornerMappings) subdomainToGlobalCornerMappings[s] = matrixLc;
				};
				environment.ExecuteSubdomainAction(cluster.Subdomains, subdomanAction);
			};
			environment.ExecuteClusterAction(clusters, clusterAction);
		}

		//TODOMPI: This method should be called in a process that has all subdomains. This is restrictive, since it enforces the
		//		centralized design for FETI-DP coarse problem. Same for the method that maps subdomain-global corner dofs 
		//		(MapCornerDofs()). Find a way around this. Also investigate if coarse problem dofs
		//		should be handled by a different component. Perhaps the whole design of components that handle subdomain, cluster
		//		and global operations for the same intention (e.g. 1 component for dofs, 1 for matrices, etc) is problematic.
		public void OrderGlobalCornerDofs() 
		{
			GlobalCornerDofOrdering = new DofTable();
			NumGlobalCornerDofs = 0;
			foreach (ISubdomain subdomain in model.Subdomains)
			{
				DofTable cornerDofs = subdomainDofOrderingsCorner[subdomain.ID]; //TODO: This should be a method of DofTable itself.
				foreach ((INode node, IDofType dof, int subdomainIdx) in cornerDofs)
				{
					bool didNotExist = GlobalCornerDofOrdering.TryAdd(node, dof, NumGlobalCornerDofs);
					if (didNotExist)
					{
						NumGlobalCornerDofs++;
					}
				}
			}
		}

		public void ReorderGlobalCornerDofs(DofPermutation permutation)
		{
			if (permutation.IsBetter)
			{
				GlobalCornerDofOrdering.Reorder(permutation.PermutationArray, permutation.PermutationIsOldToNew);
			}
		}

		public void ReorderRemainderDofs(int subdomainID, DofPermutation permutation)
		{
			if (permutation.IsBetter)
			{
				int[] remainderDofs = permutation.ReorderKeysOfDofIndicesMap(subdomainDofsRemainderToFree[subdomainID]);
				lock (subdomainDofsRemainderToFree) subdomainDofsRemainderToFree[subdomainID] = remainderDofs;
			}
		}

		public void SeparateBoundaryRemainderAndInternalDofs()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Corner, remainder dofs
		/// </summary>
		/// <param name="cornerDofSelection"></param>
		public void SeparateCornerRemainderDofs(ICornerDofSelection cornerDofSelection)
		{
			Action<Cluster> clusterAction = cluster =>
			{
				Action<ISubdomain> subdomainAction = subdomain =>
				{
					int s = subdomain.ID;
					(DofTable cornerDofOrdering, int[] cornerToFree, int[] remainderToFree) =
						SeparateSubdomainDofs(subdomain, cornerDofSelection);
					lock (subdomainNumFreeDofs) subdomainNumFreeDofs[s] = subdomain.FreeDofOrdering.NumFreeDofs;
					lock (subdomainDofOrderingsCorner) subdomainDofOrderingsCorner[s] = cornerDofOrdering;
					lock (subdomainDofsCornerToFree) subdomainDofsCornerToFree[s] = cornerToFree;
					lock (subdomainDofsRemainderToFree) subdomainDofsRemainderToFree[s] = remainderToFree;
				};
				environment.ExecuteSubdomainAction(cluster.Subdomains, subdomainAction);
			};
			environment.ExecuteClusterAction(clusters, clusterAction);
		}

		private static (DofTable cornerDofOrdering, int[] cornerToFree, int[] remainderToFree) SeparateSubdomainDofs(
			ISubdomain subdomain, ICornerDofSelection cornerDofSelection)
		{
			var cornerDofOrdering = new DofTable();
			var cornerToFree = new List<int>();
			var remainderToFree = new HashSet<int>();
			int numCornerDofs = 0;
			DofTable freeDofs = subdomain.FreeDofOrdering.FreeDofs;
			IEnumerable<INode> nodes = freeDofs.GetRows(); //TODO: Optimize access: Directly get INode, Dictionary<IDof, int>
			foreach (INode node in nodes)
			{
				IReadOnlyDictionary<IDofType, int> dofsOfNode = freeDofs.GetDataOfRow(node);
				foreach (var dofIdxPair in dofsOfNode)
				{
					IDofType dof = dofIdxPair.Key;
					if (cornerDofSelection.IsCornerDof(node, dof))
					{
						cornerDofOrdering[node, dof] = numCornerDofs++;
						cornerToFree.Add(dofIdxPair.Value);
					}
					else
					{
						remainderToFree.Add(dofIdxPair.Value);
					}
				}
			}
			return (cornerDofOrdering, cornerToFree.ToArray(), remainderToFree.ToArray());
		}
	}
}
