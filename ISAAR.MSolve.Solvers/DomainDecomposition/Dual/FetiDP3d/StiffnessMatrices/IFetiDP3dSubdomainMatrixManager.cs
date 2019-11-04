using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Vectors;

namespace ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP3d.StiffnessMatrices
{
    public interface IFetiDP3dSubdomainMatrixManager : IFetiSubdomainMatrixManager
    {
        /// <summary>
        /// Farhat, eq.27 top left submatrix
        /// </summary>
        IMatrixView KccStar { get; }

        /// <summary>
        /// Farhat, eq.27 top right submatrix
        /// </summary>
        IMatrixView KcmStar { get; }

        /// <summary>
        /// Farhat, eq.27 bottom right submatrix
        /// </summary>
        IMatrixView KmmStar { get; }

        void CalcSubdomainKStarMatrices();

        void ExtractCornerRemainderSubmatrices();

        void InvertKrr(bool inPlace);

    }
}
