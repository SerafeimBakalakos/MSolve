﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.LinearAlgebra.Vectors;

namespace ISAAR.MSolve.Discretization.FreedomDegrees
{
    public class GlobalFreeDofOrderingSingleSubdomain : IGlobalFreeDofOrdering
    {
        private readonly ISubdomain subdomain;
        private readonly ISubdomainFreeDofOrdering subdomainDofOrdering;
        private int[] subdomainToGlobalDofMap;

        public GlobalFreeDofOrderingSingleSubdomain(ISubdomain subdomain, ISubdomainFreeDofOrdering subdomainDofOrdering)
        {
            this.subdomain = subdomain;

            this.NumGlobalFreeDofs = subdomainDofOrdering.NumFreeDofs;
            this.GlobalFreeDofs = subdomainDofOrdering.FreeDofs;
            this.subdomainDofOrdering = subdomainDofOrdering;
        }

        public DofTable GlobalFreeDofs { get; }
        public int NumGlobalFreeDofs { get; }

        //TODO: Usually the only vectors added are the ones from subdomains, therefore in this case we can just copy
        //      subdomainVector, instead of adding it.
        public void AddVectorSubdomainToGlobal(ISubdomain subdomain, IVectorView subdomainVector, IVector globalVector)
        {
            Debug.Assert(subdomain == this.subdomain);
            globalVector.AddSubvectorIntoThis(0, subdomainVector, 0, subdomainVector.Length);
        }

        public void AddVectorSubdomainToGlobalMeanValue(ISubdomain subdomain, IVectorView subdomainVector, 
            IVector globalVector)
        {
            Debug.Assert(subdomain == this.subdomain);
            globalVector.AddSubvectorIntoThis(0, subdomainVector, 0, subdomainVector.Length);
        }

        public void CreateSubdomainGlobalMaps(IModel model)
        {
            // A for-loop is faster than LINQ
            this.subdomainToGlobalDofMap = new int[NumGlobalFreeDofs];
            for (int i = 0; i < NumGlobalFreeDofs; ++i) this.subdomainToGlobalDofMap[i] = i;
        }

        public void ExtractVectorSubdomainFromGlobal(ISubdomain subdomain, IVectorView globalVector, IVector subdomainVector)
        {
            Debug.Assert(subdomain == this.subdomain);
            subdomainVector.CopyFrom(globalVector);
        }

        public ISubdomainFreeDofOrdering GetSubdomainDofOrdering(ISubdomain subdomain)
        {
            Debug.Assert(subdomain == this.subdomain);
            return subdomainDofOrdering;
        }

        //TODO: Using this map is a waste of computational power: map[i] = i
        public int[] MapSubdomainToGlobalDofs(ISubdomain subdomain)
        {
            Debug.Assert(subdomain == this.subdomain);
            return subdomainToGlobalDofMap;
        }
    }
}
