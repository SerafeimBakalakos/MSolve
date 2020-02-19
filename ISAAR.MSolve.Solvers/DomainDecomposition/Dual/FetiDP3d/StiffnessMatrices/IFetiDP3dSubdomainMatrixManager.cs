using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.StiffnessMatrices;
using ISAAR.MSolve.Solvers.Ordering.Reordering;

//TODO: Unify this with IFetiDPSubdomainMatrixManager. Rename anything with GlobalCorner dofs as CoarseProblemDofs and assume more 
//      an array of stiffness matrices per subdomain
namespace ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP3d.StiffnessMatrices
{
    public interface IFetiDP3dSubdomainMatrixManager : IFetiDPSubdomainMatrixManager
    {
        /// <summary>
        /// Farhat, eq.27 bottom right submatrix
        /// </summary>
        IMatrixView KaaStar { get; }

        /// <summary>
        /// Farhat, eq.27 top right submatrix
        /// </summary>
        IMatrixView KacStar { get; }


        //TODO: Use these names instead of CondenseMatricesStatically(), CondenseVectorsStatically
        //void CalcSubdomainKStarMatrices();
        //void CalcSubdomainFcStartVector();
    }
}
