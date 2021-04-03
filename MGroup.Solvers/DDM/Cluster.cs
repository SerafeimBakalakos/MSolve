using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;
using MGroup.Solvers.DDM.Dofs;
using MGroup.Solvers.DDM.Mappings;

namespace MGroup.Solvers.DDM
{
	public class Cluster
	{
		public Cluster(int id)
		{
			this.ID = id;
		}

		public int ID { get; }

		public List<ISubdomain> Subdomains { get; } = new List<ISubdomain>();

		public Dictionary<INode, HashSet<Cluster>> InterClusterNodes { get; }

		public List<ClusterBoundary> ClusterBoundaries { get; } = new List<ClusterBoundary>();

	}
}
