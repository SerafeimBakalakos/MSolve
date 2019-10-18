using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Matrices;

namespace ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.DofSeparation
{
    public interface IAugmentationConstraints
    {
        /// <summary>
        /// Qr is a (nL x nQ) matrix where nL is the number of global lagrange multipliers and nQ is 
        /// <see cref="NumGlobalAugmentationConstraints"/>.
        /// </summary>
        Matrix MatrixQr { get; }

        /// <summary>
        /// The number of extra constraints for the 3D problem. E.g. in "A scalable dual–primal domain decomposition method, 
        /// Farhat et al, 2000" it is proposed to add 3 constraints (X,Y,Z) at the middle of each boundary edge between 
        /// subdomains.
        /// </summary>
        int NumGlobalAugmentationConstraints { get; }
    }
}
