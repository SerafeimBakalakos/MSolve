using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Solvers.Ordering.Reordering;

//TODO: Unify this with IFetiDPSubdomainMatrixManager. Rename anything with GlobalCorner dofs as CoarseProblemDofs and assume more 
//      an array of stiffness matrices per subdomain
namespace ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP3d.StiffnessMatrices
{
    public interface IFetiDP3dSubdomainMatrixManager : IFetiSubdomainMatrixManager
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
        /// Farhat, eq.27 bottom right submatrix
        /// </summary>
        IMatrixView KaaStar { get; }

        /// <summary>
        /// Farhat, eq.27 top right submatrix
        /// </summary>
        IMatrixView KacStar { get; }

        /// <summary>
        /// Farhat, eq.27 top left submatrix
        /// </summary>
        IMatrixView KccStar { get; }

        void CalcSubdomainKStarMatrices();

        void CalcSubdomainFcStartVector();

        void ClearRhsVectors();

        void ExtractCornerRemainderRhsSubvectors();
        void ExtractCornerRemainderSubmatrices();

        void InvertKrr(bool inPlace);

        Vector MultiplyInverseKrrTimes(Vector vector);
        Vector MultiplyKcrTimes(Vector vector);
        Vector MultiplyKrcTimes(Vector vector);

        DofPermutation ReorderInternalDofs();
        DofPermutation ReorderRemainderDofs();
    }
}
