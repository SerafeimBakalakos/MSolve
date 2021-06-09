using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.FEM.Entities;
using MGroup.FEM.Entities;
using MGroup.Solvers.Ordering;
using MGroup.Solvers.Ordering.Reordering;

namespace MGroup.Solvers.DomainDecomposition.Tests.ExampleModels
{
    public static class ModelUtilities
    {
        public static void OrderDofs(IStructuralModel model)
        {
            var dofOrderer = new DofOrderer(new NodeMajorDofOrderingStrategy(), new NullReordering());
            foreach (ISubdomain subdomain in model.Subdomains)
            {
                subdomain.FreeDofOrdering = dofOrderer.OrderFreeDofs(subdomain);
            }
        }
    }
}
