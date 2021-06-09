using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Discretization.Interfaces;

namespace MGroup.Solvers.DomainDecomposition.Partitioning
{
    public interface IPartitioner
    {
        int NumSubdomainsTotal { get; }

        int GetClusterOfSubdomain(int subdomainID);

        IEnumerable<int> GetNeighboringSubdomains(int subdomainID);

        int GetSubdomainOfElement(int elementID);

        void Partition(IStructuralModel model);
    }
}
