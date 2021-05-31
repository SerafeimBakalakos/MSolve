using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.FEM.Entities;
using ISAAR.MSolve.Solvers.Ordering;
using ISAAR.MSolve.Solvers.Ordering.Reordering;

namespace MGroup.Solvers.DomainDecomposition.Tests.ExampleModels
{
    public static class ModelUtilities
    {
        public static void Decompose(Model model, int numSubdomains, Func<int, int> elementToSubdomain)
        {
            model.SubdomainsDictionary.Clear();
            foreach (Node node in model.NodesDictionary.Values) node.SubdomainsDictionary.Clear();
            foreach (Element element in model.ElementsDictionary.Values) element.Subdomain = null;

            for (int s = 0; s < numSubdomains; ++s)
            {
                model.SubdomainsDictionary[s] = new Subdomain(s);
            }
            foreach (Element element in model.ElementsDictionary.Values)
            {
                Subdomain subdomain = model.SubdomainsDictionary[elementToSubdomain(element.ID)];
                subdomain.Elements.Add(element);
            }
        }

        public static void OrderDofs(IStructuralModel model)
        {
            var dofOrderer = new DofOrderer(new NodeMajorDofOrderingStrategy(), new NullReordering());
            var globalDofs = dofOrderer.OrderFreeDofs(model);
            model.GlobalDofOrdering = globalDofs;
            foreach (ISubdomain subdomain in model.Subdomains)
            {
                subdomain.FreeDofOrdering = globalDofs.SubdomainDofOrderings[subdomain];
            }
        }
    }
}
