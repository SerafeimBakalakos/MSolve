using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;

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
            subdomainDofOrderings = new Dictionary<int, ISubdomainFreeDofOrdering>();
            foreach (ISubdomain subdomain in model.EnumerateSubdomains())
            {
                subdomainDofOrderings[subdomain.ID] = subdomain.FreeDofOrdering;
            }
            CalcSubdomainGlobalMappings();
        }

        public ISubdomainFreeDofOrdering GetSubdomainDofOrdering(ISubdomain subdomain) => subdomainDofOrderings[subdomain.ID];

        public int[] MapSubdomainToGlobalDofs(ISubdomain subdomain) => subdomainToGlobalDofMaps[subdomain.ID];
    }
}
