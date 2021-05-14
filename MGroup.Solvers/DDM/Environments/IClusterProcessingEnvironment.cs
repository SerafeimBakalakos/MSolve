using System;
using System.Collections.Generic;
using System.Text;

//TODOMPI: Clusters will be mapped to ComputeNodes, therefore the execution of actions per cluster and the communication between
//		clusters will be handled by IComputeEnvironment. This component must be removed.
namespace MGroup.Solvers.DDM.Environments
{
	public interface IClusterProcessingEnvironment
	{
		void ExecuteClusterAction(IEnumerable<Cluster> clusters, Action<Cluster> action);
	}
}
