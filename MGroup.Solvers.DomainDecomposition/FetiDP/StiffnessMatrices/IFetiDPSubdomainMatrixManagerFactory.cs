using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;
using MGroup.Solvers.DomainDecomposition.FetiDP.Dofs;
using MGroup.Solvers.DomainDecomposition.StiffnessMatrices;

namespace MGroup.Solvers.DomainDecomposition.FetiDP.StiffnessMatrices
{
	public interface IFetiDPSubdomainMatrixManagerFactory
	{
		(ISubdomainMatrixManager, IFetiDPSubdomainMatrixManager) CreateMatrixManagers(ISubdomain subdomain, FetiDPSubdomainDofs dofSeparator);
	}
}
