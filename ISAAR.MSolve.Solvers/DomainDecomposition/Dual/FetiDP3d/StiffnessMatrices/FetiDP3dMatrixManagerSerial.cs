using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.CornerNodes;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.DofSeparation;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP3d.Augmentation;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.LagrangeMultipliers;
using ISAAR.MSolve.Solvers.Ordering.Reordering;

namespace ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP3d.StiffnessMatrices
{
    public class FetiDP3dMatrixManagerSerial : IFetiDP3dMatrixManager
    {
        private readonly IFetiDP3dGlobalMatrixManager matrixManagerGlobal;
        private readonly Dictionary<ISubdomain, IFetiDP3dSubdomainMatrixManager> matrixManagersSubdomain;
        private readonly IModel model;
        private readonly string msgHeader;

        public FetiDP3dMatrixManagerSerial(IModel model, IFetiDPDofSeparator dofSeparator, 
            ILagrangeMultipliersEnumerator lagrangesEnumerator, IAugmentationConstraints augmentationConstraints, 
            IFetiDP3dMatrixManagerFactory matrixManagerFactory)
        {
            this.model = model;
            this.matrixManagersSubdomain = new Dictionary<ISubdomain, IFetiDP3dSubdomainMatrixManager>();
            matrixManagerGlobal = matrixManagerFactory.CreateGlobalMatrixManager(model, dofSeparator, augmentationConstraints);
            foreach (ISubdomain sub in model.EnumerateSubdomains())
            {
                this.matrixManagersSubdomain[sub] = 
                    matrixManagerFactory.CreateSubdomainMatrixManager(sub, dofSeparator, lagrangesEnumerator,
                    augmentationConstraints);
            }
            this.msgHeader = $"{this.GetType().Name}: ";
        }

        public Vector CoarseProblemRhs => matrixManagerGlobal.CoarseProblemRhs;

        public void CalcCoarseProblemRhs()
        {
            // Calculate FcStar of each subdomain
            // fcStar[s] = fbc[s] - Krc[s]^T * inv(Krr[s]) * fr[s] -> delegated to the SubdomainMatrixManager
            // globalFcStar = sum_over_s(Lc[s]^T * fcStar[s]) -> delegated to the GlobalMatrixManager
            var allFcStar = new Dictionary<ISubdomain, Vector>();
            foreach (ISubdomain sub in model.EnumerateSubdomains())
            {
                matrixManagersSubdomain[sub].CalcSubdomainFcStartVector();
                allFcStar[sub] = matrixManagersSubdomain[sub].FcStar;
            }

            // Give them to the global matrix manager so that it can create the global FcStar
            matrixManagerGlobal.CalcCoarseProblemRhs(allFcStar);
        }

        public void CalcInverseCoarseProblemMatrix(ICornerNodeSelection cornerNodeSelection)
        {
            // Calculate KccStarTilde of each subdomain
            // globalKccStarTilde = sum_over_s(Lc[s]^T * KccStarTilde[s] * Lc[s]) -> delegated to the GlobalMatrixManager 
            // Here we will just prepare the data for GlobalMatrixManager
            var allKStarMatrices = new Dictionary<ISubdomain, (IMatrixView KccStar, IMatrixView KacStar, IMatrixView KaaStar)>();
            foreach (ISubdomain sub in model.EnumerateSubdomains())
            {
                IFetiDP3dSubdomainMatrixManager matrices = matrixManagersSubdomain[sub];
                if (sub.StiffnessModified)
                {
                    Debug.WriteLine(msgHeader + "Calculating Schur complement of remainder dofs"
                        + $" for the stiffness of subdomain {sub.ID}");
                    matrices.CalcSubdomainKStarMatrices(); //TODO: At this point Kcc and Krc can be cleared. Maybe Krr too.
                }
                allKStarMatrices[sub] = (matrices.KccStar, matrices.KacStar, matrices.KaaStar);
            }

            // Give them to the global matrix manager so that it can create the global KccStar
            matrixManagerGlobal.CalcInverseCoarseProblemMatrix(cornerNodeSelection, allKStarMatrices);
        }

        public void ClearCoarseProblemRhs() => matrixManagerGlobal.ClearCoarseProblemRhs();
        public void ClearInverseCoarseProblemMatrix() => matrixManagerGlobal.ClearInverseCoarseProblemMatrix();

        IFetiSubdomainMatrixManager IFetiMatrixManager.GetSubdomainMatrixManager(ISubdomain subdomain)
            => matrixManagersSubdomain[subdomain];

        public IFetiDP3dSubdomainMatrixManager GetFetiDPSubdomainMatrixManager(ISubdomain subdomain) 
            => matrixManagersSubdomain[subdomain];

        public Vector MultiplyInverseCoarseProblemMatrix(Vector vector) 
            => matrixManagerGlobal.MultiplyInverseCoarseProblemMatrixTimes(vector);

        public DofPermutation ReorderGlobalCornerDofs() => matrixManagerGlobal.ReorderCoarseProblemDofs();

        public DofPermutation ReorderSubdomainInternalDofs(ISubdomain subdomain) 
            => matrixManagersSubdomain[subdomain].ReorderInternalDofs();

        public DofPermutation ReorderSubdomainRemainderDofs(ISubdomain subdomain)
            => matrixManagersSubdomain[subdomain].ReorderRemainderDofs();
    }
}
