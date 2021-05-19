using System;
using System.Collections.Generic;
using System.Text;

namespace MGroup.Solvers_OLD.DDM.Environments
{
	public class ClusterEnvironmentManagedSequential : IClusterProcessingEnvironment
	{
		public void ExecuteClusterAction(IEnumerable<Cluster> clusters, Action<Cluster> action)
		{
			foreach (Cluster cluster in clusters)
			{
				action(cluster);
			}
		}
	}
}
