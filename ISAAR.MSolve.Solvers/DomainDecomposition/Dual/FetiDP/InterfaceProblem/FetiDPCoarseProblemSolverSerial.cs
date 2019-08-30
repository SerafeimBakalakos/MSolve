using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.Discretization.Transfer;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Matrices.Operators;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.CornerNodes;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.DofSeparation;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.Matrices;

namespace ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.InterfaceProblem
{
    public class FetiDPCoarseProblemSolverSerial
    {
        //TODO: There should be a IFetiDPMatrixManager which can be queried by ISubdomain or global, instead of these 2.
        private readonly IFetiDPGlobalMatrixManager matrixManagerGlobal;
        private readonly IFetiDPSubdomainMatrixManager matrixManagerSubdomain; 

        private readonly string msgHeader;
        private readonly IModel model;

        public FetiDPCoarseProblemSolverSerial(IModel model,
            IFetiDPSubdomainMatrixManager matrixManagerSubdomain, IFetiDPGlobalMatrixManager matrixManagerGlobal)
        {
            this.model = model;
            this.matrixManagerSubdomain = matrixManagerSubdomain;
            this.matrixManagerGlobal = matrixManagerGlobal;

            this.msgHeader = $"{this.GetType().Name}: ";
        }

        public void CalcInverseCoarseProblemMatrix(ICornerNodeSelection cornerNodeSelection,
            IFetiDPDofSeparator dofSeparator)
        {
            // Calculate KccStar of each subdomain
            var allKccStar = new Dictionary<ISubdomain, IMatrixView>();
            foreach (ISubdomain subdomain in model.EnumerateSubdomains())
            {
                // KccStar[s] = Kcc[s] - Krc[s]^T * inv(Krr[s]) * Krc[s]
                // globalKccStar = sum_over_s(Lc[s]^T * KccStar[s] * Lc[s]) -> delegated to the GlobalMatrixManager, 
                // since the process depends on matrix storage format
                if (subdomain.StiffnessModified)
                {
                    Debug.WriteLine(msgHeader + "Calculating Schur complement of remainder dofs"
                        + $" for the stiffness of subdomain {subdomain.ID}");
                    matrixManagerSubdomain.CondenseMatricesStatically(); //TODO: At this point Kcc and Krc can be cleared. Maybe Krr too.
                } //TODO: I prefer to have FETI logic elsewhere. This class should only make sure that data are in the correct process. E.g CalcCoarseProblemRhs does this correctly.
                allKccStar[subdomain] = matrixManagerSubdomain.KccStar;
            }

            // Give them to the global matrix manager so that it can create the global KccStar
            matrixManagerGlobal.AssembleAndInvertCoarseProblemMatrix(cornerNodeSelection, dofSeparator, allKccStar);
        }

        public void CalcCoarseProblemRhs(IFetiDPDofSeparator dofSeparator)
        {
            // Calculate FcStar of each subdomain
            var allFcStar = new Dictionary<ISubdomain, Vector>();
            foreach (ISubdomain subdomain in model.EnumerateSubdomains())
            {
                // fcStar[s] = fbc[s] - Krc[s]^T * inv(Krr[s]) * fr[s]
                // globalFcStar = sum_over_s(Lc[s]^T * fcStar[s]) -> delegated to the GlobalMatrixManager
                allFcStar[subdomain] = FetiDPCoarseProblemUtilities.CondenseSubdomainRemainderRhs(matrixManagerSubdomain);
            }

            // Give them to the global matrix manager so that it can create the global FcStar
            matrixManagerGlobal.AssembleCoarseProblemRhs(dofSeparator, allFcStar);
        }
    }
}
