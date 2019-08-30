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
    public class FetiDPCoarseProblemSolverMpi : IFetiDPGlobalMatrixManager
    {
        //TODO: There should be a IFetiDPMatrixManager which can be queried by ISubdomain or global, instead of these 2.
        private readonly IFetiDPGlobalMatrixManager matrixManagerGlobal_master;
        private readonly IFetiDPSubdomainMatrixManager matrixManagerSubdomain; 

        private readonly string msgHeader;
        private readonly IModel model;
        private readonly ProcessDistribution procs;

        public FetiDPCoarseProblemSolverMpi(ProcessDistribution processDistribution, IModel model,
            IFetiDPSubdomainMatrixManager matrixManagerSubdomain, IFetiDPGlobalMatrixManager matrixManagerGlobal_master)
        {
            this.procs = processDistribution;
            this.model = model;
            this.matrixManagerSubdomain = matrixManagerSubdomain;
            this.matrixManagerGlobal_master = matrixManagerGlobal_master;

            this.msgHeader = $"Process {processDistribution.OwnRank}, {this.GetType().Name}: ";
        }

        public void CalcInverseCoarseProblemMatrix(ICornerNodeSelection cornerNodeSelection,
            IFetiDPDofSeparator dofSeparator)
        {
            ISubdomain subdomain = model.GetSubdomain(procs.OwnSubdomainID);

            // Calculate the subdomain KccStar in each process
            // KccStar[s] = Kcc[s] - Krc[s]^T * inv(Krr[s]) * Krc[s]
            // globalKccStar = sum_over_s(Lc[s]^T * KccStar[s] * Lc[s]) -> delegated to the GlobalMatrixManager, 
            // since the process depends on matrix storage format
            if (subdomain.StiffnessModified)
            {
                Debug.WriteLine(msgHeader + "Calculating Schur complement of remainder dofs"
                    + $" for the stiffness of subdomain {subdomain.ID}");
                matrixManagerSubdomain.CondenseMatricesStatically(); //TODO: At this point Kcc and Krc can be cleared. Maybe Krr too.
            }

            // Gather them in master
            Dictionary<ISubdomain, IMatrixView> allKccStar_master = null;
            IMatrixView[] receivedKccStar_master = GatherSchurComplementsOfRemainderDofs();
            if (procs.IsMasterProcess)
            {
                allKccStar_master = new Dictionary<ISubdomain, IMatrixView>();
                for (int p = 0; p < procs.Communicator.Size; ++p)
                {
                    ISubdomain sub = model.GetSubdomain(p);
                    allKccStar_master[sub] = receivedKccStar_master[p];
                }

                // Give them to the global matrix manager so that it can create the global KccStar
                matrixManagerGlobal_master.AssembleAndInvertCoarseProblemMatrix(cornerNodeSelection, dofSeparator,
                    allKccStar_master);
            }
        }

        public void CalcCoarseProblemRhs(IFetiDPDofSeparator dofSeparator)
        {
            // Calculate the subdomain FcStar in each process
            // fcStar[s] = fbc[s] - Krc[s]^T * inv(Krr[s]) * fr[s]
            // globalFcStar = sum_over_s(Lc[s]^T * fcStar[s]) -> delegated to the GlobalMatrixManager
            Vector fcStar = FetiDPCoarseProblemUtilities.CondenseSubdomainRemainderRhs(matrixManagerSubdomain);

            // Gather them in master
            Dictionary<ISubdomain, Vector> allFcStar_master = null;
            Vector[] receivedFcStar_master = GatherCondensedRhsVectors(fcStar);
            if (procs.IsMasterProcess)
            {
                allFcStar_master = new Dictionary<ISubdomain, Vector>();
                for (int p = 0; p < procs.Communicator.Size; ++p)
                {
                    ISubdomain sub = model.GetSubdomain(p);
                    allFcStar_master[sub] = receivedFcStar_master[p];
                }

                // Give them to the global matrix manager so that it can create the global FcStar
                matrixManagerGlobal_master.AssembleCoarseProblemRhs(dofSeparator, allFcStar_master);
            }
        }

        private Vector[] GatherCondensedRhsVectors(Vector subdomainVector)
        {
            //TODO: Perhaps I should cache them and reuse the unchanged ones. Use dedicated communication classes for this.
            return procs.Communicator.Gather(subdomainVector, procs.MasterProcess);
        }

        private IMatrixView[] GatherSchurComplementsOfRemainderDofs()
        {
            //TODO: Perhaps I should cache them and reuse the unchanged ones. Use dedicated communication classes for this.
            return procs.Communicator.Gather(matrixManagerSubdomain.KccStar, procs.MasterProcess);
        }
    }
}
