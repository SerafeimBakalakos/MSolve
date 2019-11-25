using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;

//TODO: Scatter, Gather operations that entail all subdomains should be provided as extension methods. Transferer classes need 
//      only provide versions where only some subdomains are used.
namespace ISAAR.MSolve.Discretization.Transfer
{
    public static class TransfererExtensions
    {
        public static Dictionary<ISubdomain, T> ChangeKey<T>(this Dictionary<int, T> subdomainIDsToData, IModel model)
        {
            var subdomainsToData = new Dictionary<ISubdomain, T>();
            foreach (var pair in subdomainIDsToData)
            {
                ISubdomain subdomain = model.GetSubdomain(pair.Key);
                subdomainsToData[subdomain] = pair.Value;
            }
            return subdomainsToData;
        }

        public static Dictionary<int, T> ChangeKey<T>(this Dictionary<ISubdomain, T> subdomainsToData)
        {
            var subdomainIDsToData = new Dictionary<int, T>();
            foreach (var pair in subdomainsToData) subdomainIDsToData[pair.Key.ID] = pair.Value;
            return subdomainIDsToData;
        }
    }
}
