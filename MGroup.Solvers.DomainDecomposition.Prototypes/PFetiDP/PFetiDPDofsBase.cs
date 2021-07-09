using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using MGroup.Solvers.DomainDecomposition.Prototypes.FetiDP;

namespace MGroup.Solvers.DomainDecomposition.Prototypes.PFetiDP
{
    public abstract class PFetiDPDofsBase
    {
        protected readonly IStructuralModel model;
        protected readonly PsmDofs psmDofs;
        protected readonly FetiDPDofs fetiDPDofs;

        public PFetiDPDofsBase(IStructuralModel model, PsmDofs psmDofs, FetiDPDofs fetiDPDofs)
        {
            this.model = model;
            this.psmDofs = psmDofs;
            this.fetiDPDofs = fetiDPDofs;
        }

        public Dictionary<int, Matrix> SubdomainMatricesNrb { get; } = new Dictionary<int, Matrix>();

        public abstract void MapPsmAndFetiDPDofs();
        
        protected void MapSubdomainRemainderBoundaryDofs(ISubdomain subdomain)
        {
            int s = subdomain.ID;

            // Free to boundary dofs
            int[] boundaryToFree = psmDofs.SubdomainDofsBoundaryToFree[s];
            var freeToBoundary = new Dictionary<int, int>();
            for (int i = 0; i < boundaryToFree.Length; i++)
            {
                freeToBoundary[boundaryToFree[i]] = i;
            }

            // Remainder to free dofs
            int[] remainderToFree = fetiDPDofs.SubdomainDofsRemainderToFree[s];
            var Nrb = Matrix.CreateZero(remainderToFree.Length, boundaryToFree.Length);
            for (int r = 0; r < remainderToFree.Length; r++)
            {
                int f = remainderToFree[r];
                bool exists = freeToBoundary.TryGetValue(f, out int b);
                if (exists) // some remainder dofs are internal, thus they cannot be boundary too.
                {
                    Nrb[r, b] = 1.0;
                }
            }

            this.SubdomainMatricesNrb[s] = Nrb;
        }
    }
}
