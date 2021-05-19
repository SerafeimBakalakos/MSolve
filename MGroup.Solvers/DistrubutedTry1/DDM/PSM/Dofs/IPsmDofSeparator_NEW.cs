using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using MGroup.Solvers_OLD.DDM.Mappings;
using MGroup.Solvers_OLD.DofOrdering.Reordering;

//TODOMPI: DofTable should be replaced with an IntTable that stores ids, instead of actual references to nodes and dofs. 
//		This will make transfering it via MPI much faster.
//TODO: Naming convention for dofs (free/constrained, boundary/internal/corner/intercluster, subdomain/cluster/global) that will
//		be followed across all components
//TODOMPI: I should decouple the code, such that there are classes that define operations on data, classes that define transfer of data,
//		and classes that define the order of such operations and transfers (sequential, shared parallel or distributed parallel)
//		and any synchronization (e.g. locks in IDofSeparator classes). This should probably be done for each component 
//		(e.g. IDofSeparator, IMatrixManager, etc.). However if I can find common code (boilerplate), I could probably avoid some 
//		duplication. 
namespace MGroup.Solvers_OLD.DistributedTry1.DDM.Psm.Dofs
{
	public interface IPsmDofSeparator_NEW : Solvers_OLD.DDM.Psm.Dofs.IPsmDofSeparator
	{
		void OrderBoundaryDofsOfClusters();
	}
}
