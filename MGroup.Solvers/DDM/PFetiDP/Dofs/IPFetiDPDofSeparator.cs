using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;
using MGroup.Solvers.DDM.FetiDP.Dofs;
using MGroup.Solvers.DDM.Mappings;
using MGroup.Solvers.DDM.Psm.StiffnessDistribution;

namespace MGroup.Solvers.DDM.PFetiDP.Dofs
{
	public interface IPFetiDPDofSeparator
	{
		IMappingMatrix GetDofMappingGlobalCornerToClusterBoundary(int clusterID);

		IMappingMatrix GetDofMappingBoundaryClusterToSubdomainRemainder(int subdomainID);

		void MapDofsPsmFetiDP(IStiffnessDistribution stiffnessDistribution);
	}
}
