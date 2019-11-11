using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Solvers.Ordering.Reordering;

namespace ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP3d.StiffnessMatrices
{
    public interface IFetiDP3dSubdomainMatrixManager : IFetiSubdomainMatrixManager
    {
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

        void ExtractCornerRemainderSubmatrices();

        void InvertKrr(bool inPlace);

        Vector MultiplyInverseKrrTimes(Vector vector);
        Vector MultiplyKcrTimes(Vector vector);
        Vector MultiplyKrcTimes(Vector vector);

        DofPermutation ReorderInternalDofs();
        DofPermutation ReorderRemainderDofs();
    }
}
