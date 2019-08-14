using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;

namespace ISAAR.MSolve.Discretization.Transfer
{
    //TODO: Find a better way to serialize than using these objects. They also need to cast.
    public interface ISubdomainSerializer
    {
        ISubdomainDto Serialize(ISubdomain subdomain);
    }
}
