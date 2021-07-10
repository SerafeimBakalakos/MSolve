﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;
using MGroup.Solvers.DomainDecomposition.FetiDP.Dofs;
using MGroup.Solvers.DomainDecomposition.Mappings;
using MGroup.Solvers.DomainDecomposition.PSM.Dofs;

namespace MGroup.Solvers.DomainDecomposition.PFetiDP.Dofs
{
    public class PFetiDPSubdomainDofs
    {
        private readonly PsmSubdomainDofs psmDofs;
        private readonly FetiDPSubdomainDofs fetiDPDofs;

        public PFetiDPSubdomainDofs(PsmSubdomainDofs psmDofs, FetiDPSubdomainDofs fetiDPDofs)
        {
            this.psmDofs = psmDofs;
            this.fetiDPDofs = fetiDPDofs;
        }

        /// <summary>
        /// Boolean mapping matrix where rows = corner dofs of subdomain, columns = boundary dofs of subdomain.
        /// </summary>
        public IMappingMatrix MatrixNcb { get; private set; }

        /// <summary>
        /// Boolean mapping matrix where rows = remainder dofs of subdomain, columns = boundary (not boundary remainder) dofs of 
        /// subdomain.
        /// </summary>
        public IMappingMatrix MatrixNrb { get; private set; }

        public void MapPsmFetiDPDofs()
        {
            // Free to boundary dofs
            int[] boundaryToFree = psmDofs.DofsBoundaryToFree;
            var freeToBoundary = new Dictionary<int, int>();
            for (int i = 0; i < boundaryToFree.Length; i++)
            {
                freeToBoundary[boundaryToFree[i]] = i;
            }

            // Corner to boundary dofs
            int[] cornerToFree = fetiDPDofs.DofsRemainderToFree;
            var cornerToBoundary = new int[cornerToFree.Length];
            for (int c = 0; c < cornerToFree.Length; c++)
            {
                int f = cornerToFree[c];
                bool exists = freeToBoundary.TryGetValue(f, out int b); // all corner dofs are also boundary.
                Debug.Assert(exists, "Found corner dof that is not boundary. This should not have happened");
                cornerToBoundary[c] = b;
            }
            this.MatrixNcb = new BooleanMatrixRowsToColumns(cornerToFree.Length, boundaryToFree.Length, cornerToBoundary);

            // Remainder to free dofs
            int[] remainderToFree = fetiDPDofs.DofsRemainderToFree;
            var remainderToBoundary = new Dictionary<int, int>();
            for (int r = 0; r < remainderToFree.Length; r++)
            {
                int f = remainderToFree[r];
                bool exists = freeToBoundary.TryGetValue(f, out int b);
                if (exists) // some remainder dofs are internal, thus they cannot be boundary too.
                {
                    remainderToBoundary[r] = b;
                }
            }
            this.MatrixNrb = new MappingMatrixN(remainderToFree.Length, boundaryToFree.Length, remainderToBoundary);
        }
    }
}