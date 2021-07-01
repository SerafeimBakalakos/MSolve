using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;
using MGroup.Solvers.DomainDecomposition.PSM.Dofs;
using MGroup.Solvers.DomainDecomposition.StiffnessMatrices;

namespace MGroup.Solvers.DomainDecomposition.PSM.StiffnessMatrices
{
	public interface IPsmSubdomainMatrixManagerFactory
	{
		(ISubdomainMatrixManager, IPsmSubdomainMatrixManager) CreateMatrixManagers(
			ISubdomain subdomain, PsmSubdomainDofs subdomainDofs);
	}
}
