using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Matrices.Operators;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.DofSeparation;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.Matrices;

namespace ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.InterfaceProblem
{
    internal static class FetiDPCoarseProblemUtilities
    {
        private const string msgHeader = "FetiDPCoarseProblemUtilities: ";

        internal static Vector AssembleCoarseProblemRhs(IFetiDPDofSeparator dofSeparator, 
            Dictionary<ISubdomain, Vector> condensedRhsVectors)
        {
            // globalFcStar = sum_over_s(Lc[s]^T * fcStar[s])
            var globalFcStar = Vector.CreateZero(dofSeparator.NumGlobalCornerDofs);
            foreach (ISubdomain subdomain in condensedRhsVectors.Keys)
            {
                UnsignedBooleanMatrix Lc = dofSeparator.GetCornerBooleanMatrix(subdomain);
                Vector fcStar = condensedRhsVectors[subdomain];
                globalFcStar.AddIntoThis(Lc.Multiply(fcStar, true));
            }
            return globalFcStar;
        }

        internal static IMatrixView CondenseSubdomainRemainderMatrix(IFetiDPSubdomainMatrixManager matrixManager)
        {
            // KccStar[s] = Kcc[s] - Krc[s]^T * inv(Krr[s]) * Krc[s]
            // globalKccStar = sum_over_s(Lc[s]^T * KccStar[s] * Lc[s]) -> delegated to the GlobalMatrixManager, 
            // since the process depends on matrix storage format
            ISubdomain subdomain = matrixManager.LinearSystem.Subdomain;
            if (subdomain.StiffnessModified)
            {
                Debug.WriteLine(msgHeader + "Calculating Schur complement of remainder dofs"
                    + $" for the stiffness of subdomain {subdomain.ID}");
                matrixManager.CondenseMatricesStatically(); //TODO: At this point Kcc and Krc can be cleared. Maybe Krr too.
            }
            return matrixManager.KccStar;
        }

        internal static Vector CondenseSubdomainRemainderRhs(IFetiDPSubdomainMatrixManager matrixManager)
        {
            // fcStar[s] = fbc[s] - Krc[s]^T * inv(Krr[s]) * fr[s]
            Vector temp = matrixManager.MultiplyInverseKrrTimes(matrixManager.Fr);
            temp = matrixManager.MultiplyKcrTimes(temp);
            Vector fcStar = matrixManager.Fbc - temp;
            return fcStar;
        }
    }
}
