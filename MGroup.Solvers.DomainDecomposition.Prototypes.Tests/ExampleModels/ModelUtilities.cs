using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.FEM.Entities;
using MGroup.Solvers.Ordering;
using MGroup.Solvers.Ordering.Reordering;

namespace MGroup.Solvers.DomainDecomposition.Prototypes.Tests.ExampleModels
{
    public static class ModelUtilities
    {
        public static void DecomposeIntoSubdomains(this Model model, int numSubdomains, Func<int, int> getSubdomainOfElement)
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
                Subdomain subdomain = model.SubdomainsDictionary[getSubdomainOfElement(element.ID)];
                subdomain.Elements.Add(element);
            }

            model.ConnectDataStructures();
        }


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
