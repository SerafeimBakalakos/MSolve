using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;

namespace ISAAR.MSolve.Discretization.Transfer
{
    public class EmptySubdomainDto : ISubdomainDto
    {
        public ISubdomain Deserialize()
        {
            throw new NotSupportedException($"You cannot deserialize objects of type {this.GetType()}");
        }
    }
}
