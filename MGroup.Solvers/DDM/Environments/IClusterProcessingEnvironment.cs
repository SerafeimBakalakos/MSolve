using System;
using System.Collections.Generic;
using System.Text;

namespace MGroup.Solvers.DDM.Environments
{
	public interface IClusterProcessingEnvironment
	{
		void ExecuteClusterAction(IEnumerable<Cluster> clusters, Action<Cluster> action);
	}
}
