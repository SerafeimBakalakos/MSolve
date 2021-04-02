using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Discretization.Interfaces;
using MGroup.Solvers.DDM.Environments;
using MGroup.Solvers.DDM.FetiDP.Dofs;
using MGroup.Solvers.DDM.Mappings;
using MGroup.Solvers.DDM.Psm.Dofs;
using MGroup.Solvers.DDM.Psm.StiffnessDistribution;

namespace MGroup.Solvers.DDM.PFetiDP.Dofs
{
	public class PFetiDPDofSeparator : IPFetiDPDofSeparator
	{
		private readonly IDdmEnvironment environment;
		private readonly IStructuralModel model;
		private readonly IList<Cluster> clusters;
		private readonly IPsmDofSeparator psmDofs;
		private readonly IFetiDPDofSeparator fetiDPDofs;
		private Dictionary<int, IMappingMatrix> matricesbNc =
			new Dictionary<int, IMappingMatrix>();
		private Dictionary<int, IMappingMatrix> matricesLpr =
			new Dictionary<int, IMappingMatrix>();

		public PFetiDPDofSeparator(IDdmEnvironment environment, IStructuralModel model, IList<Cluster> clusters, 
			IPsmDofSeparator psmDofs, IFetiDPDofSeparator fetiDPDofs)
		{
			this.environment = environment;
			this.model = model;
			this.clusters = clusters;
			this.psmDofs = psmDofs;
			this.fetiDPDofs = fetiDPDofs;
		}

		public IMappingMatrix GetDofMappingBoundaryClusterToSubdomainRemainder(int subdomainID)
			=> matricesLpr[subdomainID];

		public IMappingMatrix GetDofMappingGlobalCornerToClusterBoundary(int clusterID)
			=> matricesbNc[clusterID];

		public void MapDofsPsmFetiDP(IStiffnessDistribution stiffnessDistribution)
		{
			CalcMatricesLpr(stiffnessDistribution);
			CalcMatricesbNc();
		}

		public void CalcMatricesLpr(IStiffnessDistribution stiffnessDistribution)
		{
			Action<ISubdomain> subdomainAction = subdomain =>
			{
				int s = subdomain.ID;
				IMappingMatrix Lpb = stiffnessDistribution.GetDofMappingBoundaryClusterToSubdomain(s);
				var matrices = new IMappingMatrix[2];

				// rNb mapping
				matrices[0] = MapBoundaryToRemainder(psmDofs.GetSubdomainDofsBoundaryToFree(s), fetiDPDofs.GetDofsRemainderToFree(s));

				matrices[1] = Lpb;
				var Lpr = new ProductMappingMatrix(matrices);
				lock (matricesLpr) matricesLpr[s] = Lpr;
			};
			environment.ExecuteSubdomainAction(model.Subdomains, subdomainAction);
		}

		public void CalcMatricesbNc()
		{
			// bNc mappings: global corner <-> cluster boundary
			if (clusters.Count > 1)
			{
				throw new NotImplementedException();
			}

			Cluster cluster = clusters.First();
			DofTable clusterBoundaryDofs = psmDofs.GetClusterDofOrderingBoundary(cluster.ID);
			DofTable globalCornerDofs = fetiDPDofs.GlobalCornerDofOrdering;
			var cornerToBoundary = new int[fetiDPDofs.NumGlobalCornerDofs];
			foreach ((INode node, IDofType dof, int cornerIdx) in globalCornerDofs)
			{
				int boundaryIdx = clusterBoundaryDofs[node, dof];
				cornerToBoundary[cornerIdx] = boundaryIdx;
			}

			int numRows = psmDofs.GetNumBoundaryDofsCluster(cluster.ID);
			int numColumns = fetiDPDofs.NumGlobalCornerDofs;
			matricesbNc[cluster.ID] =
				new BooleanMatrixColumnsToRows(numRows, numColumns, cornerToBoundary);
		}

		private static MappingMatrixN MapBoundaryToRemainder(int[] boundaryToFree, int[] remainderToFree)
		{
			var freeToBoundary = new Dictionary<int, int>();
			for (int i = 0; i < boundaryToFree.Length; i++)
			{
				freeToBoundary[boundaryToFree[i]] = i;
			}

			var remainderToBoundary = new Dictionary<int, int>();
			for (int r = 0; r < remainderToFree.Length; r++)
			{
				int f = remainderToFree[r];
				bool exists = freeToBoundary.TryGetValue(f, out int b);
				if (exists)
				{
					remainderToBoundary[r] = b;
				}
			}

			return new MappingMatrixN(remainderToFree.Length, boundaryToFree.Length, remainderToBoundary);
		}
	}
}
