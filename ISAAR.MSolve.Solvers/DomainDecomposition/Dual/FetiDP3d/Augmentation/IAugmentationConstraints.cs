using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.LinearAlgebra.Matrices;

//TODO: Qr matrix should be defined per subdomain. Crucial for MPI
namespace ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP3d.Augmentation
{
    public interface IAugmentationConstraints
    {
        /// <summary>
        /// Qr is a (nL x na) matrix where nL is the number of global lagrange multipliers and na is 
        /// <see cref="NumGlobalAugmentationConstraints"/>.
        /// </summary>
        Matrix MatrixGlobalQr { get; }

        IMidsideNodesSelection MidsideNodesSelection { get; }

        /// <summary>
        /// The number of extra constraints for the 3D problem. E.g. in "A scalable dual–primal domain decomposition method, 
        /// Farhat et al, 2000" it is proposed to add 3 constraints (X,Y,Z) at the middle of each boundary edge between 
        /// subdomains.
        /// </summary>
        int NumGlobalAugmentationConstraints { get; }

        void CalcAugmentationMappingMatrices();

        Matrix GetMatrixBa(ISubdomain subdomain);
        Matrix GetMatrixQ1(ISubdomain subdomain);


    }
}
