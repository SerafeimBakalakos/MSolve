using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Matrices.Operators;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.CornerNodes;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.DofSeparation;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.InterfaceProblem;
using ISAAR.MSolve.Solvers.Ordering.Reordering;

namespace ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.Matrices
{
    public class FetiDPGlobalMatrixManagerDense : IFetiDPGlobalMatrixManager
    {
        private readonly IModel model;
        private Matrix inverseGlobalKccStar;

        public FetiDPGlobalMatrixManagerDense(IModel model)
        {
            this.model = model;
        }

        public Vector CoarseProblemRhs { get; private set; }

        public void ClearCoarseProblemMatrix()
        {
            inverseGlobalKccStar = null;
        }

        public void AssembleAndInvertCoarseProblemMatrix(ICornerNodeSelection cornerNodeSelection, 
            IFetiDPDofSeparator dofSeparator, Dictionary<ISubdomain, IMatrixView> schurComplementsOfRemainderDofs)
        {
            // globalKccStar = sum_over_s(Lc[s]^T * KccStar[s] * Lc[s])
            var globalKccStar = Matrix.CreateZero(dofSeparator.NumGlobalCornerDofs, dofSeparator.NumGlobalCornerDofs);
            foreach (ISubdomain subdomain in model.EnumerateSubdomains())
            {
                int s = subdomain.ID;
                IMatrixView subdomainKccStar = schurComplementsOfRemainderDofs[subdomain];

                UnsignedBooleanMatrix Lc = dofSeparator.GetCornerBooleanMatrix(subdomain);
                globalKccStar.AddIntoThis(Lc.ThisTransposeTimesOtherTimesThis(subdomainKccStar));
            }

            inverseGlobalKccStar = globalKccStar;
            inverseGlobalKccStar.InvertInPlace();
        }

        public void AssembleCoarseProblemRhs(IFetiDPDofSeparator dofSeparator, Dictionary<ISubdomain, Vector> condensedRhsVectors)
            => CoarseProblemRhs = FetiDPCoarseProblemUtilities.AssembleCoarseProblemRhs(dofSeparator, condensedRhsVectors);

        public Vector MultiplyInverseCoarseProblemMatrixTimes(Vector vector) => inverseGlobalKccStar * vector;

        public DofPermutation ReorderCornerDofs(IFetiDPDofSeparator dofSeparator) => DofPermutation.CreateNoPermutation();
    }
}
