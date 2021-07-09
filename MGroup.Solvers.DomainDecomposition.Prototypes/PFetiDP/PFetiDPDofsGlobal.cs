using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using MGroup.Solvers.DomainDecomposition.Prototypes.FetiDP;

namespace MGroup.Solvers.DomainDecomposition.Prototypes.PFetiDP
{
    public class PFetiDPDofsGlobal : PFetiDPDofsBase
    {
        public PFetiDPDofsGlobal(IStructuralModel model, PsmDofs psmDofs, FetiDPDofs fetiDPDofs) 
            : base (model, psmDofs, fetiDPDofs)
        {
        }

        public Matrix GlobalMatrixNcb { get; } 

        public override void MapPsmAndFetiDPDofs()
        {
            foreach (ISubdomain subdomain in model.Subdomains)
            {
                MapSubdomainRemainderBoundaryDofs(subdomain);
            }
            MapGlobalCornerBoundaryDofs();
        }

        private void MapGlobalCornerBoundaryDofs()
        {
            DofTable boundaryDofs = psmDofs.GlobalDofOrderingBoundary;
            DofTable cornerDofs = fetiDPDofs.GlobalDofOrderingCorner;
            var Ncb = Matrix.CreateZero(fetiDPDofs.NumGlobalDofsCorner, psmDofs.NumGlobalDofsBoundary);
            foreach ((INode node, IDofType dof, int c) in cornerDofs)
            {
                int b = boundaryDofs[node, dof];
                Ncb[c, b] = 1.0;
            }
        }
    }
}
