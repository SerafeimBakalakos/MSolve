using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Vectors;

namespace MGroup.Solvers.DomainDecomposition.Prototypes.PSM
{
    public class PsmSubdomainVectors
    {
        private readonly PsmSubdomainDofs dofs;
        private readonly PsmSubdomainStiffnesses stiffnesses;

        public PsmSubdomainVectors(PsmSubdomainDofs dofs, PsmSubdomainStiffnesses stiffnesses)
        {
            this.dofs = dofs;
            this.stiffnesses = stiffnesses;
        }

        public Dictionary<int, Vector> Fb { get; } = new Dictionary<int, Vector>();

        public Dictionary<int, Vector> Fi { get; } = new Dictionary<int, Vector>();

        public Dictionary<int, Vector> FbCondensed { get; } = new Dictionary<int, Vector>();

        public Dictionary<int, Vector> Uf { get; } = new Dictionary<int, Vector>();

        public void CalcSubdomainForces(int s, Vector Ff)
        {
            int[] boundaryToFree = dofs.SubdomainDofsBoundaryToFree[s];
            int[] internalToFree = dofs.SubdomainDofsInternalToFree[s];
            Fb[s] = Ff.GetSubvector(boundaryToFree);
            Fi[s] = Ff.GetSubvector(internalToFree);
            FbCondensed[s] = Fb[s] - stiffnesses.Kbi[s] * stiffnesses.invKii[s] * Fi[s];
        }

        public void CalcFreeDisplacements(int s, Vector Ub)
        {
            // Extract internal and boundary parts of rhs vector 
            int numFreeDofs = dofs.NumSubdomainDofsFree[s];
            int[] boundaryToFree = dofs.SubdomainDofsBoundaryToFree[s];
            int[] internalToFree = dofs.SubdomainDofsInternalToFree[s];

            // ui[s] = inv(Kii[s]) * (fi[s] - Kib[s] * ub[s])
            Vector Ui = stiffnesses.invKii[s] * (Fi[s] - stiffnesses.Kib[s] * Ub); 

            // Gather ub[s], ui[s] into uf[s]
            Uf[s] = Vector.CreateZero(numFreeDofs);
            Uf[s].CopyNonContiguouslyFrom(boundaryToFree, Ub);
            Uf[s].CopyNonContiguouslyFrom(internalToFree, Ui);
        }
    }
}
