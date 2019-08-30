﻿using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.DofSeparation;
using ISAAR.MSolve.Solvers.Ordering.Reordering;

//TODO: During initialization, the solver and its verious strategies should inform IFetiDPSubdomainMatrixManager what matrices
//      will be necessary. IFetiDPSubdomainMatrixManager should then determine the correct order they must be created in and
//      notify the solver and each strategy when they are ready for consumption. Also once a matrix has been fully used, 
//      it should be cleared to conserve memory. This also applies for Kff.
//TODO: Perhaps this class should only access FetiDPSubdomainDofSeparator instead of IFetiDPDofSeparator
namespace ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.Matrices
{
    /// <summary>
    /// Implements the linear algebra operations needed by <see cref="FetiDPSolver"/> depending on the underlying matrix storage
    /// format. All the matrices represented by this interface belong to a single subdomain.
    /// Authors: Serafeim Bakalakos
    /// </summary>
    public interface IFetiDPSubdomainMatrixManager : IFetiSubdomainMatrixManager
    {
        /// <summary>
        /// The subvector of rhs that corresponds to corner dofs.
        /// </summary>
        Vector Fbc { get; }

        /// <summary>
        /// The vector that results from static condensation of remainder dofs. Its entries correspond to corner dofs.
        /// </summary>
        Vector FcStar { get; }

        /// <summary>
        /// The subvector of rhs that corresponds to remainder dofs.
        /// </summary>
        Vector Fr { get; }

        /// <summary>
        /// The Schur complement of the submatrix corresponding to remainder dofs: K / Krr. Its rows and columns correspond to 
        /// corner dofs.
        /// </summary>
        IMatrixView KccStar { get; }

        void CondenseMatricesStatically();
        void CondenseVectorsStatically();

        void ClearVectors();

        //TODO: E.g. Once Kcc* is calculated Kcc and Krc can be cleared. There are 2 options:
        //      a) Each matrix must be able to be cleared independently, if the FETI-DP solver and its strategies decide when.
        //      b) Otherwise this matrix manager decides when to clear what and these methods are optional/risky.
        //void ClearKcc();
        //void ClearKcrKrc();

        void ExtractCornerRemainderSubmatrices();
        void ExtractCornerRemainderSubvectors();

        void InvertKrr(bool inPlace);

        Vector MultiplyInverseKrrTimes(Vector vector);
        Vector MultiplyKccTimes(Vector vector); //TODO: Not used anywhere. In fact Kcc can be overwritten with KccStar during CalcSchurComplementOfRemainderDofs() without any headaches.
        Vector MultiplyKcrTimes(Vector vector);
        Vector MultiplyKrcTimes(Vector vector);

        DofPermutation ReorderInternalDofs();
        DofPermutation ReorderRemainderDofs();
    }
}
