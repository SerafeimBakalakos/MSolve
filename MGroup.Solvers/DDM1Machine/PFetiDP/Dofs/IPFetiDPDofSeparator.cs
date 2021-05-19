using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;
using MGroup.Solvers_OLD.DDM.FetiDP.Dofs;
using MGroup.Solvers_OLD.DDM.Mappings;
using MGroup.Solvers_OLD.DDM.Psm.StiffnessDistribution;

namespace MGroup.Solvers_OLD.DDM.PFetiDP.Dofs
{
	public interface IPFetiDPDofSeparator
	{
		IMappingMatrix GetDofMappingGlobalCornerToClusterBoundary(int clusterID);

		IMappingMatrix GetDofMappingBoundaryClusterToSubdomainRemainder(int subdomainID);

		void MapDofsPsmFetiDP(IStiffnessDistribution stiffnessDistribution);
	}
}
