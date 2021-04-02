using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Discretization.Interfaces;
using MGroup.Solvers.DDM.Environments;
using MGroup.Solvers.DDM.Mappings;
using MGroup.Solvers.DDM.PFetiDP.Dofs;
using MGroup.Solvers.DDM.Psm.Dofs;
using ISAAR.MSolve.Discretization.Commons;
using ISAAR.MSolve.Discretization.FreedomDegrees;

namespace MGroup.Solvers.DDM.Psm.StiffnessDistribution
{
	public class HomogeneousStiffnessDistribution : IStiffnessDistribution
	{
		private readonly IDdmEnvironment environment;
		private readonly IList<Cluster> clusters;
		private readonly IPsmDofSeparator dofSeparator;
		private readonly Dictionary<int, IMappingMatrix> dofMappingBoundaryClusterToSubdomain = 
			new Dictionary<int, IMappingMatrix>();
		private readonly Dictionary<int, double[]> inverseMultiplicities = new Dictionary<int, double[]>();

		public HomogeneousStiffnessDistribution(IDdmEnvironment environment, IList<Cluster> clusters, 
			IPsmDofSeparator dofSeparator)
		{
			this.environment = environment;
			this.clusters = clusters;
			this.dofSeparator = dofSeparator;
		}

		public void CalcSubdomainScaling()
		{
			Action<Cluster> clusterAction = cluster =>
			{
				int c = cluster.ID;
				var clusterW = new double[dofSeparator.GetNumBoundaryDofsCluster(c)];
				foreach (var (node, dof, idx) in dofSeparator.GetClusterDofOrderingBoundary(c))
				{
					clusterW[idx] = 1.0 / node.GetMultiplicity();
				}

				Action<ISubdomain> subdomainAction = sub =>
				{
					BooleanMatrixRowsToColumns Lb = dofSeparator.GetDofMappingBoundaryClusterToSubdomain(sub.ID);
					var subdomainW = new double[Lb.NumRows];
					for (int i = 0; i < Lb.NumRows; ++i)
					{
						subdomainW[i] = clusterW[Lb.RowsToColumns[i]];
					}
					var Lpb = new ScalingMatrixRowMajor(Lb.NumRows, Lb.NumColumns, Lb.RowsToColumns, subdomainW);
					lock (dofMappingBoundaryClusterToSubdomain) dofMappingBoundaryClusterToSubdomain[sub.ID] = Lpb;
					lock (inverseMultiplicities) inverseMultiplicities[sub.ID] = subdomainW;
				};
				environment.ExecuteSubdomainAction(cluster.Subdomains, subdomainAction);
			};
			environment.ExecuteClusterAction(clusters, clusterAction);
		}

		public IMappingMatrix GetDofMappingBoundaryClusterToSubdomain(int subdomainID) 
			=> dofMappingBoundaryClusterToSubdomain[subdomainID];

		public Dictionary<int, SparseVector> DistributeNodalLoads(Table<INode, IDofType, double> globalNodalLoads, 
			IEnumerable<ISubdomain> subdomains)
		{
			var subdomainLoads = new Dictionary<int, SortedDictionary<int, double>>();
			foreach (ISubdomain subdomain in subdomains)
			{
				subdomainLoads[subdomain.ID] = new SortedDictionary<int, double>();
			}

			foreach ((INode node, IDofType dofType, double loadAmount) in globalNodalLoads)
			{
				if (node.SubdomainsDictionary.Count == 1) // optimization for internal dofs
				{
					ISubdomain subdomain = node.SubdomainsDictionary.First().Value;
					int subdomainDofIdx = subdomain.FreeDofOrdering.FreeDofs[node, dofType];
					subdomainLoads[subdomain.ID][subdomainDofIdx] = loadAmount;
				}
				else // boundary dofs
				{
					foreach (var idSubdomain in node.SubdomainsDictionary)
					{
						int id = idSubdomain.Key;
						ISubdomain subdomain = idSubdomain.Value;
						int subdomainDofIdx = subdomain.FreeDofOrdering.FreeDofs[node, dofType];
						subdomainLoads[id][subdomainDofIdx] = loadAmount / node.SubdomainsDictionary.Count;
					}
				}
			}

			var subdomainVectors = new Dictionary<int, SparseVector>();
			Action<ISubdomain> subdomainAction = sub =>
			{
				int s = sub.ID;
				int numSubdomainDofs = sub.FreeDofOrdering.NumFreeDofs;
				var vector = SparseVector.CreateFromDictionary(numSubdomainDofs, subdomainLoads[s]);
				lock (subdomainVectors) subdomainVectors[s] = vector;
			};
			environment.ExecuteSubdomainAction(subdomains, subdomainAction);

			return subdomainVectors;
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
