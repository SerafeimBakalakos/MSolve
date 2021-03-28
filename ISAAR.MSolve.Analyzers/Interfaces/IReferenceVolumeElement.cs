using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.LinearAlgebra.Matrices;

namespace ISAAR.MSolve.Analyzers.Interfaces
{
    public interface IReferenceVolumeElement
    {
        /// <summary>
        /// For homogenization with linear boundary displacements 
        /// </summary>
        void ApplyBoundaryConditionsLinear(double[] macroscopicStrains);

        /// <summary>
        /// All displacements at boundary dofs become 0. Useful when we only need the macroscopic modulus.
        /// </summary>
        void ApplyBoundaryConditionsZero(); // Can I not pass macroscopicStrains = 0?

        IMatrixView CalculateKinematicRelationsMatrix(ISubdomain subdomain);

        double CalculateRveVolume();
    }
}
