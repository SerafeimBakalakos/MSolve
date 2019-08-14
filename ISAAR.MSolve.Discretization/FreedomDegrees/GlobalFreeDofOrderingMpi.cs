using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using MPI;

namespace ISAAR.MSolve.Discretization.FreedomDegrees
{
    public class GlobalFreeDofOrderingMpi //: IGlobalFreeDofOrdering //TODO: This must be implemented ASAP
    {
        private readonly Intracommunicator comm;
        private readonly int masterProcess;
        private readonly int rank;

        //private readonly Dictionary<ISubdomain, int[]> subdomainToGlobalDofMaps;

        public GlobalFreeDofOrderingMpi(int numGlobalFreeDofs, DofTable globalFreeDofs, Intracommunicator comm,
            int masterProcess)
        {
            this.NumGlobalFreeDofs = numGlobalFreeDofs;
            this.GlobalFreeDofs = globalFreeDofs;
            //this.SubdomainDofOrderings = subdomainDofOrderings;

            this.comm = comm;
            this.rank = comm.Rank;
            this.masterProcess = masterProcess;

            //TODO: This should be evaluated lazily.
            //subdomainToGlobalDofMaps = new Dictionary<ISubdomain, int[]>(subdomainDofOrderings.Count);
            //foreach (var subdomainOrderingPair in subdomainDofOrderings)
            //{
            //    var subdomainToGlobalDofs = new int[subdomainOrderingPair.Value.NumFreeDofs];
            //    foreach ((INode node, IDofType dofType, int subdomainDofIdx) in subdomainOrderingPair.Value.FreeDofs)
            //    {
            //        subdomainToGlobalDofs[subdomainDofIdx] = globalFreeDofs[node, dofType];
            //    }
            //    subdomainToGlobalDofMaps.Add(subdomainOrderingPair.Key, subdomainToGlobalDofs);
            //}
        }

        public DofTable GlobalFreeDofs { get; }
        public int NumGlobalFreeDofs { get; }

        //public IReadOnlyDictionary<ISubdomain, ISubdomainFreeDofOrdering> SubdomainDofOrderings { get; }

        //public void AddVectorSubdomainToGlobal(ISubdomain subdomain, IVectorView subdomainVector, IVector globalVector)
        //{
        //    ISubdomainFreeDofOrdering subdomainOrdering = SubdomainDofOrderings[subdomain];
        //    int[] subdomainToGlobalDofs = subdomainToGlobalDofMaps[subdomain];
        //    globalVector.AddIntoThisNonContiguouslyFrom(subdomainToGlobalDofs, subdomainVector);
        //}

        //public void AddVectorSubdomainToGlobalMeanValue(ISubdomain subdomain, IVectorView subdomainVector,
        //    IVector globalVector) => throw new NotImplementedException();

        //public void ExtractVectorSubdomainFromGlobal(ISubdomain subdomain, IVectorView globalVector, IVector subdomainVector)
        //{
        //    ISubdomainFreeDofOrdering subdomainOrdering = SubdomainDofOrderings[subdomain];
        //    int[] subdomainToGlobalDofs = subdomainToGlobalDofMaps[subdomain];
        //    subdomainVector.CopyNonContiguouslyFrom(globalVector, subdomainToGlobalDofs);
        //}

        //public int[] MapFreeDofsSubdomainToGlobal(ISubdomain subdomain) => subdomainToGlobalDofMaps[subdomain];
    }
}
