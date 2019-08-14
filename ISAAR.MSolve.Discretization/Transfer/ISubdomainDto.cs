using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;

namespace ISAAR.MSolve.Discretization.Transfer
{
    public interface ISubdomainDto
    {
        ISubdomain Deserialize();
    }
}
