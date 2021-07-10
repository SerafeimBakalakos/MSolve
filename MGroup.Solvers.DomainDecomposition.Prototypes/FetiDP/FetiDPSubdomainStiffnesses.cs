using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.LinearAlgebra.Matrices;

namespace MGroup.Solvers.DomainDecomposition.Prototypes.FetiDP
{
    public class FetiDPSubdomainStiffnesses
    {
        private readonly FetiDPSubdomainDofs dofs;

        public FetiDPSubdomainStiffnesses(FetiDPSubdomainDofs dofs)
        {
            this.dofs = dofs;
        }

        public Dictionary<int, Matrix> Kcc { get; } = new Dictionary<int, Matrix>();

        public Dictionary<int, Matrix> Kcr { get; } = new Dictionary<int, Matrix>();

        public Dictionary<int, Matrix> Krc { get; } = new Dictionary<int, Matrix>();

        public Dictionary<int, Matrix> Krr { get; } = new Dictionary<int, Matrix>();

        public Dictionary<int, Matrix> invKrr { get; } = new Dictionary<int, Matrix>();

        public Dictionary<int, Matrix> Scc { get; } = new Dictionary<int, Matrix>();

        public void CalcSchurComplements(int s, IMatrix Kff)
        {
            int[] boundaryToFree = dofs.SubdomainDofsCornerToFree[s];
            int[] internalToFree = dofs.SubdomainDofsRemainderToFree[s];
            Kcc[s] = (Matrix)Kff.GetSubmatrix(boundaryToFree, boundaryToFree);
            Kcr[s] = (Matrix)Kff.GetSubmatrix(boundaryToFree, internalToFree);
            Krc[s] = (Matrix)Kff.GetSubmatrix(internalToFree, boundaryToFree);
            Krr[s] = (Matrix)Kff.GetSubmatrix(internalToFree, internalToFree);
            invKrr[s] = Krr[s].Invert();
            Scc[s] = Kcc[s] - Kcr[s] * invKrr[s] * Krc[s];
        }
    }
}
