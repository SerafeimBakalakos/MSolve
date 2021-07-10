using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using MGroup.Solvers.DomainDecomposition.Prototypes.FetiDP;

namespace MGroup.Solvers.DomainDecomposition.Prototypes.PFetiDP
{
    public abstract class PFetiDPSubdomainDofs
    {
        protected readonly IStructuralModel model;
        protected readonly PsmSubdomainDofs psmDofs;
        protected readonly FetiDPSubdomainDofs fetiDPDofs;

        public PFetiDPSubdomainDofs(IStructuralModel model, PsmSubdomainDofs psmDofs, FetiDPSubdomainDofs fetiDPDofs)
        {
            this.model = model;
            this.psmDofs = psmDofs;
            this.fetiDPDofs = fetiDPDofs;
        }

        public Dictionary<int, Matrix> SubdomainMatricesNcb { get; } = new Dictionary<int, Matrix>();

        public Dictionary<int, Matrix> SubdomainMatricesNrb { get; } = new Dictionary<int, Matrix>();

        public void MapPsmFetiDPDofs()
        {
            foreach (ISubdomain subdomain in model.Subdomains)
            {
                int s = subdomain.ID;

                // Free to boundary dofs
                int[] boundaryToFree = psmDofs.SubdomainDofsBoundaryToFree[s];
                var freeToBoundary = new Dictionary<int, int>();
                for (int i = 0; i < boundaryToFree.Length; i++)
                {
                    freeToBoundary[boundaryToFree[i]] = i;
                }

                // Corner to boundary dofs
                int[] cornerToFree = fetiDPDofs.SubdomainDofsRemainderToFree[s];
                var Ncb = Matrix.CreateZero(cornerToFree.Length, boundaryToFree.Length);
                for (int c = 0; c < cornerToFree.Length; c++)
                {
                    int f = cornerToFree[c];
                    bool exists = freeToBoundary.TryGetValue(f, out int b); // all corner dofs are also boundary.
                    Debug.Assert(exists, "Found corner dof that is not boundary. This should not have happened");
                    Ncb[c, b] = 1.0;
                }
                this.SubdomainMatricesNcb[s] = Ncb;

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
}
