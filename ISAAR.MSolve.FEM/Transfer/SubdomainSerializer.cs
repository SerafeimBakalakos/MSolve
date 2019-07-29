using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.Discretization.Transfer;
using ISAAR.MSolve.FEM.Entities;

namespace ISAAR.MSolve.FEM.Transfer
{
    public class SubdomainSerializer : ISubdomainSerializer
    {
        public ISubdomainDto Serialize(ISubdomain subdomain) => new SubdomainDto((Subdomain)subdomain);
    }
}
