using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.LinearAlgebra.Vectors;

namespace ISAAR.MSolve.Discretization.FreedomDegrees
{
    public class GlobalFreeDofOrderingGeneral : GlobalFreeDofOrderingBase, IGlobalFreeDofOrdering
    {
        public GlobalFreeDofOrderingGeneral(int numGlobalFreeDofs, DofTable globalFreeDofs) : 
            base(numGlobalFreeDofs, globalFreeDofs)
        {
        }

        public DofTable GlobalFreeDofs => globalFreeDofs;

        public int NumGlobalFreeDofs => numGlobalFreeDofs;

        public void CreateSubdomainGlobalMaps(IModel model)
        {
            subdomainDofOrderings = new Dictionary<ISubdomain, ISubdomainFreeDofOrdering>();
            foreach (ISubdomain subdomain in model.Subdomains) subdomainDofOrderings[subdomain] = subdomain.FreeDofOrdering;
            CalcSubdomainGlobalMappings();
        }

        public ISubdomainFreeDofOrdering GetSubdomainDofOrdering(ISubdomain subdomain) => subdomainDofOrderings[subdomain];

        public int[] GetSubdomainToGlobalMap(ISubdomain subdomain) => subdomainToGlobalDofMaps[subdomain];
    }
}
