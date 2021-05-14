using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;

namespace MGroup.Solvers.DDM
{
	public class Cluster
	{
		public Cluster(int id)
		{
			this.ID = id;
		}

		public int ID { get; }

		/// <summary>
		/// If cluster c1 has nodes n1, n5, n12 in common with cluster c2, then 
		/// c1.<see cref="InterClusterNodes"/>[c2.ID].IsSupersetOf(new int[] { n1.ID, n5.ID, n12.ID }) == true.
		/// </summary>
		public Dictionary<int, SortedSet<int>> InterClusterNodes = new Dictionary<int, SortedSet<int>>();

		public List<ISubdomain> Subdomains { get; } = new List<ISubdomain>();
	}
}
