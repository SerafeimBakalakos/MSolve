using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Discretization.Interfaces;
using MGroup.Solvers.DDM.Environments;
using MGroup.Solvers.DDM.Mappings;
using MGroup.Solvers.DDM.PFetiDP.Dofs;
using MGroup.Solvers.DDM.Psm.Dofs;
using MGroup.Solvers.DDM.StiffnessMatrices;
using ISAAR.MSolve.Discretization.Commons;
using ISAAR.MSolve.Discretization.FreedomDegrees;

namespace MGroup.Solvers.DDM.Psm.StiffnessDistribution
{
	public class HeterogeneousStiffnessDistribution : IStiffnessDistribution
	{
		private readonly IDdmEnvironment environment;
		private readonly IList<Cluster> clusters;
		private readonly IPsmDofSeparator dofSeparator;
		private readonly IMatrixManager matrixManagerBasic;
		private readonly Dictionary<int, ScalingMatrixRowMajor> dofMappingBoundaryClusterToSubdomain = 
			new Dictionary<int, ScalingMatrixRowMajor>();
		private readonly Dictionary<int, double[]> relativeStiffnesses = new Dictionary<int, double[]>();

		public HeterogeneousStiffnessDistribution(IDdmEnvironment environment, IList<Cluster> clusters, 
			IPsmDofSeparator dofSeparator, IMatrixManager matrixManagerBasic)
		{
			this.environment = environment;
			this.clusters = clusters;
			this.dofSeparator = dofSeparator;
			this.matrixManagerBasic = matrixManagerBasic;
		}

		/// <summary>
		/// See eq (6.3) from Papagiannakis bachelor :
		/// Lpb^e = Db^e * Lb^e * inv( (Lb^e)^T * Db^e * Lb^e)
		/// </summary>
		public void CalcSubdomainScaling()
		{
			Action<Cluster> clusterAction = cluster =>
			{
				int c = cluster.ID;

				// Build Db^s from each subdomain's Kff
				var matricesDb = new Dictionary<int, double[]>();
				Action<ISubdomain> calcDb = sub =>
				{
					int s = sub.ID;
					int[] boundaryDofs = dofSeparator.GetSubdomainDofsBoundaryToFree(s);
					IMatrixView Kff = matrixManagerBasic.GetLinearSystem(s).Matrix;
					var Db = new double[boundaryDofs.Length];
					for (int boundaryDofIdx = 0; boundaryDofIdx < boundaryDofs.Length; boundaryDofIdx++)
					{
						int freeDofIdx = boundaryDofs[boundaryDofIdx];
						Db[boundaryDofIdx] = Kff[freeDofIdx, freeDofIdx];
					}
					lock (matricesDb) matricesDb[s] = Db;
				};
				environment.ExecuteSubdomainAction(cluster.Subdomains, calcDb);

				// Assemble subdomain Db^s matrices into cluster's (Lb^e)^T * Db^e * Lb^e)
				var clusterDb = new double[dofSeparator.GetNumBoundaryDofsCluster(c)];
				foreach (ISubdomain subdomain in cluster.Subdomains)
				{
					int s = subdomain.ID;
					double[] subdomainDb = matricesDb[s];
					int[] subdomainToCluster = dofSeparator.GetDofMappingBoundaryClusterToSubdomain(s).RowsToColumns;
					for (int i = 0; i < subdomainToCluster.Length; i++)
					{
						clusterDb[subdomainToCluster[i]] += subdomainDb[i];
					}
				}

				// Calculate Lpb^s = Db^s * Lb^s * inv( (Lb^e)^T * Db^e * Lb^e) )
				Action<ISubdomain> calcLpb = sub =>
				{
					int s = sub.ID;
					double[] subdomainDb = matricesDb[s];
					BooleanMatrixRowsToColumns Lb = dofSeparator.GetDofMappingBoundaryClusterToSubdomain(s);
					int[] subdomainToCluster = Lb.RowsToColumns;
					var subdomainRelativeStiffness = new double[subdomainDb.Length];
					for (int i = 0; i < subdomainDb.Length; i++)
					{
						subdomainRelativeStiffness[i] = subdomainDb[i] / clusterDb[subdomainToCluster[i]];
					}
					lock (relativeStiffnesses) relativeStiffnesses[s] = subdomainRelativeStiffness;

					var Lpb = new ScalingMatrixRowMajor(Lb.NumRows, Lb.NumColumns, Lb.RowsToColumns, relativeStiffnesses[s]);
					lock (dofMappingBoundaryClusterToSubdomain) dofMappingBoundaryClusterToSubdomain[s] = Lpb;
				};
				environment.ExecuteSubdomainAction(cluster.Subdomains, calcLpb);
			};
			environment.ExecuteClusterAction(clusters, clusterAction);
		}

		public IMappingMatrix GetDofMappingBoundaryClusterToSubdomain(int subdomainID) 
			=> dofMappingBoundaryClusterToSubdomain[subdomainID];

		public Dictionary<int, SparseVector> DistributeNodalLoads(Table<INode, IDofType, double> globalNodalLoads, 
			IEnumerable<ISubdomain> subdomains)
		{
			var subdomainLoads = new Dictionary<int, SortedDictionary<int, double>>();
			foreach (var subdomain in subdomains) subdomainLoads[subdomain.ID] = new SortedDictionary<int, double>();

			foreach ((INode node, IDofType dofType, double loadAmount) in globalNodalLoads)
			{
				if (node.SubdomainsDictionary.Count == 1) // optimization for internal dofs
				{
					ISubdomain subdomain = node.SubdomainsDictionary.First().Value;
					int subdomainDofIdx = subdomain.FreeDofOrdering.FreeDofs[node, dofType];
					subdomainLoads[subdomain.ID][subdomainDofIdx] = loadAmount;
				}
				else // boundary dof: regularize with respect to the diagonal entries of the stiffness matrix at this dof
				{
					foreach (var idSubdomain in node.SubdomainsDictionary)
					{
						int s = idSubdomain.Key;
						ISubdomain subdomain = idSubdomain.Value;
						int freeDofIdx = subdomain.FreeDofOrdering.FreeDofs[node, dofType];
						int boundaryDofIdx = dofSeparator.GetSubdomainDofOrderingBoundary(s)[node, dofType];
						subdomainLoads[s][freeDofIdx] = loadAmount * relativeStiffnesses[s][boundaryDofIdx];
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
			double[] relativeStiffnessOfSubdomain = relativeStiffnesses[subdomainID];
			for (int i = 0; i < boundaryDofs.Length; i++)
			{
				double coeff = relativeStiffnessOfSubdomain[i];
				subdomainForces[boundaryDofs[i]] *= coeff;
			}
		}
	}
}
