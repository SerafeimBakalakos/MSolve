using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.FEM.Entities;

namespace MGroup.Solvers_OLD.Tests.DDM.ExampleModels
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
                subdomain.Elements.Add(element); //TODOMPI: This should be a dictionary.
            }
        }
    }
}
