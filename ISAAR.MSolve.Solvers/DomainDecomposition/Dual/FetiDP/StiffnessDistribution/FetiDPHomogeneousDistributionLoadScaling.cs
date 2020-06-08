using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.Discretization.Transfer;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.DofSeparation;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.StiffnessDistribution;

namespace ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.StiffnessDistribution
{
    public class FetiDPHomogeneousDistributionLoadScaling : IHomogeneousDistributionLoadScaling
    {
        private readonly IFetiDPDofSeparator dofSeparator;

        public FetiDPHomogeneousDistributionLoadScaling(IFetiDPDofSeparator dofSeparator)
        {
            this.dofSeparator = dofSeparator;
        }

        public double ScaleNodalLoad(ISubdomain subdomain, INodalLoad load)
        {
            INode node = load.Node;
            IDofType dof = load.DOF;

            // Loads at corner dofs will be distributed equally. It shouldn't matter how I distribute these, since I 
            // will only sum them together again during the static condensation of remainder dofs phase.
            //TODO: is that correct?
            bool isCornerDof = dofSeparator.GetCornerDofOrdering(subdomain).Contains(node, dof);
            if (isCornerDof) return load.Amount / node.Multiplicity;
            else if (node.Multiplicity > 1) return load.Amount / node.Multiplicity;
            else return load.Amount;
        }

        public void ScaleForceVectorFree(ISubdomain subdomain, Vector forceVector)
        {
            foreach ((INode node, IDofType dof, int idx) in subdomain.FreeDofOrdering.FreeDofs)
            {
                forceVector.Set(idx, forceVector[idx] / node.Multiplicity);
            }

            //// Scale boundary dofs using the realtive stiffnesses
            //int[] boundary2RemainderDofs = dofSeparator.GetBoundaryDofIndices(subdomain);
            //int[] remainder2FreeDofs = dofSeparator.GetRemainderDofIndices(subdomain);
            //for (int i = 0; i < boundaryRelativeStiffnesses.Length; ++i)
            //{
            //    int freeDofIdx = remainder2FreeDofs[boundary2RemainderDofs[i]];
            //    forceVector[freeDofIdx] *= boundaryRelativeStiffnesses[i];
            //}

            //// Scale corner dofs using their multiplicity
            //IReadOnlyList<(INode node, IDofType dofType)> cornerDofs = dofSeparator.GetCornerDofs(subdomain);
            //int[] corner2FreeDofs = dofSeparator.GetCornerDofIndices(subdomain);
            //for (int i = 0; i < cornerDofs.Count; ++i)
            //{
            //    int freeDofIdx = corner2FreeDofs[i];
            //    int multiplicity = cornerDofs[i].node.Multiplicity;
            //    forceVector[freeDofIdx] /= multiplicity;
            //}
        }
    }
}
