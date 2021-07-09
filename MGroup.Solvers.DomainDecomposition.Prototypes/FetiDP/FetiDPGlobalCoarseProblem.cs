using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Vectors;

namespace MGroup.Solvers.DomainDecomposition.Prototypes.FetiDP
{
    public class FetiDPGlobalCoarseProblem
    {
        private readonly IStructuralModel model;
        private readonly FetiDPDofs dofs;
        private readonly FetiDPStiffnesses stiffnesses;

        private Matrix invScc;

        public FetiDPGlobalCoarseProblem(IStructuralModel model, FetiDPDofs dofs, FetiDPStiffnesses stiffnesses)
        {
            this.model = model;
            this.dofs = dofs;
            this.stiffnesses = stiffnesses;
        }

        public void Initialize()
        {
            var globalScc = Matrix.CreateZero(dofs.NumGlobalDofsCorner, dofs.NumGlobalDofsCorner);
            foreach (ISubdomain subdomain in model.Subdomains)
            {
                int s = subdomain.ID;
                Matrix Lc = dofs.SubdomainMatricesLc[s];
                Matrix localScc = stiffnesses.Scc[s];
                globalScc.AddIntoThis(Lc.Transpose() * localScc * Lc);
            }
            this.invScc = globalScc.Invert();
        }

        public Vector Solve(Vector rhsVector) => invScc * rhsVector;
    }
}
