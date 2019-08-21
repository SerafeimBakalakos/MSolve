﻿using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.LinearAlgebra.Vectors;

namespace ISAAR.MSolve.Discretization.FreedomDegrees
{
    /// <summary>
    /// Defines the functionality provided and data structures needed for that. It does not specify how these data structures 
    /// are created.
    /// </summary>
    public abstract class GlobalFreeDofOrderingBase
    {
        protected readonly DofTable globalFreeDofs;
        protected readonly int numGlobalFreeDofs;
        protected Dictionary<ISubdomain, ISubdomainFreeDofOrdering> subdomainDofOrderings;
        protected Dictionary<ISubdomain, int[]> subdomainToGlobalDofMaps;

        protected GlobalFreeDofOrderingBase(int numGlobalFreeDofs, DofTable globalFreeDofs)
        {
            this.numGlobalFreeDofs = numGlobalFreeDofs;
            this.globalFreeDofs = globalFreeDofs;
        }

        public virtual void AddVectorSubdomainToGlobal(ISubdomain subdomain, IVectorView subdomainVector, IVector globalVector)
        {
            ISubdomainFreeDofOrdering subdomainOrdering = subdomainDofOrderings[subdomain];
            int[] subdomainToGlobalDofs = subdomainToGlobalDofMaps[subdomain];
            globalVector.AddIntoThisNonContiguouslyFrom(subdomainToGlobalDofs, subdomainVector);
        }

        public virtual void AddVectorSubdomainToGlobalMeanValue(ISubdomain subdomain, IVectorView subdomainVector,
            IVector globalVector) => throw new NotImplementedException();

        public virtual void ExtractVectorSubdomainFromGlobal(ISubdomain subdomain, IVectorView globalVector, IVector subdomainVector)
        {
            ISubdomainFreeDofOrdering subdomainOrdering = subdomainDofOrderings[subdomain];
            int[] subdomainToGlobalDofs = subdomainToGlobalDofMaps[subdomain];
            subdomainVector.CopyNonContiguouslyFrom(globalVector, subdomainToGlobalDofs);
        }

        protected virtual void CalcSubdomainGlobalMappings()
        {
            subdomainToGlobalDofMaps = new Dictionary<ISubdomain, int[]>(subdomainDofOrderings.Count);
            foreach (var subdomainOrderingPair in subdomainDofOrderings)
            {
                var subdomainToGlobalDofs = new int[subdomainOrderingPair.Value.NumFreeDofs];
                foreach ((INode node, IDofType dofType, int subdomainDofIdx) in subdomainOrderingPair.Value.FreeDofs)
                {
                    subdomainToGlobalDofs[subdomainDofIdx] = globalFreeDofs[node, dofType];
                }
                subdomainToGlobalDofMaps.Add(subdomainOrderingPair.Key, subdomainToGlobalDofs);
            }
        }
    }
}