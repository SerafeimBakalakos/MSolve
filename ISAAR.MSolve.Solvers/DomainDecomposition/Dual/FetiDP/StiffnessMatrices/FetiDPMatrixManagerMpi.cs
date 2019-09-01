using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.Discretization.Transfer;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.CornerNodes;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.DofSeparation;
using ISAAR.MSolve.Solvers.Ordering.Reordering;

namespace ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.StiffnessMatrices
{
    public class FetiDPMatrixManagerMpi : IFetiMatrixManager, IFetiDPMatrixManager
    {
        private readonly IFetiDPGlobalMatrixManager matrixManagerGlobal_master;
        private readonly IFetiDPSubdomainMatrixManager matrixManagerSubdomain;
        private readonly IModel model;
        private readonly string msgHeader;
        private readonly ProcessDistribution procs;
        private readonly ISubdomain subdomain;

        public FetiDPMatrixManagerMpi(ProcessDistribution processDistribution, IModel model, IFetiDPDofSeparator dofSeparator,
            IFetiDPMatrixManagerFactory matrixManagerFactory)
        {
            this.procs = processDistribution;
            this.model = model;
            this.subdomain = model.GetSubdomain(processDistribution.OwnSubdomainID);

            this.matrixManagerSubdomain = matrixManagerFactory.CreateSubdomainMatrixManager(subdomain, dofSeparator);
            if (processDistribution.IsMasterProcess)
            {
                matrixManagerGlobal_master = matrixManagerFactory.CreateGlobalMatrixManager(model, dofSeparator);
            }
            this.msgHeader = $"Process {processDistribution.OwnRank}, {this.GetType().Name}: ";
        }

        public Vector CoarseProblemRhs
        {
            get
            {
                procs.CheckProcessIsMaster();
                return matrixManagerGlobal_master.CoarseProblemRhs;
            }
        }

        public void CalcCoarseProblemRhs()
        {
            // Calculate the subdomain FcStar in each process
            // fcStar[s] = fbc[s] - Krc[s]^T * inv(Krr[s]) * fr[s] -> delegated to the SubdomainMatrixManager
            // globalFcStar = sum_over_s(Lc[s]^T * fcStar[s]) -> delegated to the GlobalMatrixManager
            matrixManagerSubdomain.CondenseRhsVectorsStatically();

            // Gather them in master
            Dictionary<ISubdomain, Vector> allFcStar_master = null;
            Vector[] receivedFcStar_master = GatherCondensedRhsVectors(matrixManagerSubdomain.FcStar);
            if (procs.IsMasterProcess)
            {
                allFcStar_master = new Dictionary<ISubdomain, Vector>();
                for (int p = 0; p < procs.Communicator.Size; ++p)
                {
                    ISubdomain sub = model.GetSubdomain(p);
                    allFcStar_master[sub] = receivedFcStar_master[p];
                }

                // Give them to the global matrix manager so that it can create the global FcStar
                matrixManagerGlobal_master.CalcCoarseProblemRhs(allFcStar_master);
            }
        }

        public void CalcInverseCoarseProblemMatrix(ICornerNodeSelection cornerNodeSelection)
        {
            // Calculate the subdomain KccStar in each process
            // KccStar[s] = Kcc[s] - Krc[s]^T * inv(Krr[s]) * Krc[s] -> delegated to the SubdomainMatrixManager
            // globalKccStar = sum_over_s(Lc[s]^T * KccStar[s] * Lc[s]) -> delegated to the GlobalMatrixManager 
            if (subdomain.StiffnessModified)
            {
                Debug.WriteLine(msgHeader + "Calculating Schur complement of remainder dofs"
                    + $" for the stiffness of subdomain {subdomain.ID}");
                matrixManagerSubdomain.CondenseMatricesStatically(); //TODO: At this point Kcc and Krc can be cleared. Maybe Krr too.
            }

            // Gather them in master
            Dictionary<ISubdomain, IMatrixView> allKccStar_master = null;
            IMatrixView[] receivedKccStar_master = GatherSchurComplementsOfRemainderDofs(matrixManagerSubdomain.KccStar);
            if (procs.IsMasterProcess)
            {
                allKccStar_master = new Dictionary<ISubdomain, IMatrixView>();
                for (int p = 0; p < procs.Communicator.Size; ++p)
                {
                    ISubdomain sub = model.GetSubdomain(p);
                    allKccStar_master[sub] = receivedKccStar_master[p];
                }

                // Give them to the global matrix manager so that it can create the global KccStar
                matrixManagerGlobal_master.CalcInverseCoarseProblemMatrix(cornerNodeSelection, allKccStar_master);
            }
        }

        public void ClearCoarseProblemRhs()
        {
            procs.CheckProcessIsMaster();
            matrixManagerGlobal_master.ClearCoarseProblemRhs();
        }

        public void ClearInverseCoarseProblemMatrix()
        {
            procs.CheckProcessIsMaster();
            matrixManagerGlobal_master.ClearInverseCoarseProblemMatrix();
        }

        IFetiSubdomainMatrixManager IFetiMatrixManager.GetSubdomainMatrixManager(ISubdomain subdomain) 
            => GetSubdomainMatrixManager(subdomain);

        public IFetiDPSubdomainMatrixManager GetSubdomainMatrixManager(ISubdomain subdomain)
        {
            procs.CheckProcessMatchesSubdomain(subdomain.ID);
            return matrixManagerSubdomain;
        }

        public Vector MultiplyInverseCoarseProblemMatrixTimes(Vector vector)
        {
            procs.CheckProcessIsMaster();
            return matrixManagerGlobal_master.MultiplyInverseCoarseProblemMatrixTimes(vector);
        }

        public DofPermutation ReorderGlobalCornerDofs()
        {
            procs.CheckProcessIsMaster();
            return matrixManagerGlobal_master.ReorderGlobalCornerDofs();
        }

        public DofPermutation ReorderSubdomainInternalDofs(ISubdomain subdomain)
        {
            procs.CheckProcessMatchesSubdomain(subdomain.ID);
            return matrixManagerSubdomain.ReorderInternalDofs();
        }

        public DofPermutation ReorderSubdomainRemainderDofs(ISubdomain subdomain)
        {
            procs.CheckProcessMatchesSubdomain(subdomain.ID);
            return matrixManagerSubdomain.ReorderRemainderDofs();
        }

        private Vector[] GatherCondensedRhsVectors(Vector subdomainVector)
        {
            //TODO: Perhaps I should cache them and reuse the unchanged ones. Use dedicated communication classes for this.
            return procs.Communicator.Gather(subdomainVector, procs.MasterProcess);
        }

        private IMatrixView[] GatherSchurComplementsOfRemainderDofs(IMatrixView subdomainMatrix)
        {
            //TODO: Perhaps I should cache them and reuse the unchanged ones. Use dedicated communication classes for this.
            return procs.Communicator.Gather(subdomainMatrix, procs.MasterProcess);
        }
    }
}
