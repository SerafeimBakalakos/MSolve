﻿using System;
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

namespace ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.StiffnessMatrices
{
    public class FetiDPGlobalMatrixManagerDense : FetiDPGlobalMatrixManagerBase
    {
        private Matrix inverseGlobalKccStar;

        public FetiDPGlobalMatrixManagerDense(IModel model, IFetiDPDofSeparator dofSeparator) : base(model, dofSeparator)
        {
        }

        public override DofPermutation ReorderGlobalCornerDofs() => DofPermutation.CreateNoPermutation();

        protected override void CalcInverseCoarseProblemMatrixImpl(ICornerNodeSelection cornerNodeSelection,
            Dictionary<ISubdomain, IMatrixView> condensedMatrices)
        {
            // globalKccStar = sum_over_s(Lc[s]^T * KccStar[s] * Lc[s])
            var globalKccStar = Matrix.CreateZero(dofSeparator.NumGlobalCornerDofs, dofSeparator.NumGlobalCornerDofs);
            foreach (ISubdomain subdomain in model.EnumerateSubdomains())
            {
                int s = subdomain.ID;
                IMatrixView subdomainKccStar = condensedMatrices[subdomain];

                UnsignedBooleanMatrix Lc = dofSeparator.GetCornerBooleanMatrix(subdomain);
                globalKccStar.AddIntoThis(Lc.ThisTransposeTimesOtherTimesThis(subdomainKccStar));
            }

            inverseGlobalKccStar = globalKccStar;
            inverseGlobalKccStar.InvertInPlace();
        }

        protected override void ClearInverseCoarseProblemMatrixImpl() => inverseGlobalKccStar = null;

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

        protected override Vector MultiplyInverseCoarseProblemMatrixTimesImpl(Vector vector) => inverseGlobalKccStar * vector;
    }
}