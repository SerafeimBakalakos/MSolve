using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.LinearAlgebra.Matrices;

namespace MGroup.Solvers.DomainDecomposition.Prototypes.PSM
{
    public class PsmStiffnesses
    {
        private readonly PsmDofs dofs;

        public PsmStiffnesses(PsmDofs dofs)
        {
            this.dofs = dofs;
        }

        public Dictionary<int, Matrix> Kbb { get; } = new Dictionary<int, Matrix>();

        public Dictionary<int, Matrix> Kbi { get; } = new Dictionary<int, Matrix>();

        public Dictionary<int, Matrix> Kib { get; } = new Dictionary<int, Matrix>();

        public Dictionary<int, Matrix> Kii { get; } = new Dictionary<int, Matrix>();

        public Dictionary<int, Matrix> invKii { get; } = new Dictionary<int, Matrix>();

        public Dictionary<int, Matrix> Sbb { get; } = new Dictionary<int, Matrix>();

        public void CalcSchurComplements(int s, IMatrix Kff)
        {
            int[] boundaryToFree = dofs.SubdomainDofsBoundaryToFree[s];
            int[] internalToFree = dofs.SubdomainDofsInternalToFree[s];
            Kbb[s] = (Matrix)Kff.GetSubmatrix(boundaryToFree, boundaryToFree);
            Kbi[s] = (Matrix)Kff.GetSubmatrix(boundaryToFree, internalToFree);
            Kib[s] = (Matrix)Kff.GetSubmatrix(internalToFree, boundaryToFree);
            Kii[s] = (Matrix)Kff.GetSubmatrix(internalToFree, internalToFree);
            invKii[s] = Kii[s].Invert();
            Sbb[s] = Kbb[s] - Kbi[s] * invKii[s] * Kib[s];
        }
    }
}
