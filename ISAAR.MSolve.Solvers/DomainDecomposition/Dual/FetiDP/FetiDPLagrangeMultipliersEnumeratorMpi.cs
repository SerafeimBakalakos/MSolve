using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ISAAR.MSolve.Discretization.Exceptions;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.Discretization.Transfer;
using ISAAR.MSolve.LinearAlgebra.Matrices.Operators;
using ISAAR.MSolve.Solvers.DomainDecomposition.DofSeparation;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.DofSeparation;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.LagrangeMultipliers;
using MPI;
    
namespace ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP
{
    /// <summary>
    /// Calculates the signed boolean matrices of the equations that enforce continuity between the multiple instances of 
    /// boundary dofs.
    /// Authors: Serafeim Bakalakos
    /// </summary>
    public class FetiDPLagrangeMultipliersEnumeratorMpi : ILagrangeMultipliersEnumeratorMpi
    {
        private readonly ICrosspointStrategy crosspointStrategy;
        private readonly IFetiDPDofSeparator dofSeparator;
        private readonly LagrangeMultiplierSerializer lagrangeSerializer;
        private readonly IModel model;
        private readonly ProcessDistribution procs;
        private LagrangeMultiplier[] lagrangeMultipliers_master;

        public FetiDPLagrangeMultipliersEnumeratorMpi(ProcessDistribution processDistribution, IModel model, 
            ICrosspointStrategy crosspointStrategy, IFetiDPDofSeparator dofSeparator)
        {
            this.procs = processDistribution;
            this.model = model;
            this.crosspointStrategy = crosspointStrategy;
            this.dofSeparator = dofSeparator;
            this.lagrangeSerializer = new LagrangeMultiplierSerializer(model.DofSerializer);
        }

        /// <summary>
        /// One of this is stored per process.
        /// </summary>
        public SignedBooleanMatrixColMajor BooleanMatrix { get; private set; }

        public LagrangeMultiplier[] LagrangeMultipliers
        {
            get
            {
                procs.CheckProcessIsMaster();
                return lagrangeMultipliers_master;
            }
        }

        /// <summary>
        /// One of this is stored per process.
        /// </summary>
        public int NumLagrangeMultipliers { get; private set; }

        public void CalcBooleanMatrices()
        { 
            // Define the lagrange multipliers and serialize and broadcast them to other processes
            int[] serializedLagranges = null;
            if (procs.IsMasterProcess)
            {
                lagrangeMultipliers_master = LagrangeMultipliersUtilities.DefineLagrangeMultipliers(
                    dofSeparator, crosspointStrategy).ToArray();
                NumLagrangeMultipliers = lagrangeMultipliers_master.Length;
                serializedLagranges = lagrangeSerializer.Serialize(lagrangeMultipliers_master);
            }
            MpiUtilities.BroadcastArray(procs.Communicator, ref serializedLagranges, procs.MasterProcess);

            // Deserialize the lagrange multipliers in other processes and calculate the boolean matrices
            ISubdomain subdomain = model.GetSubdomain(procs.OwnSubdomainID);
            int numRemainderDofs = dofSeparator.GetRemainderDofIndices(subdomain).Length;
            DofTable remainderDofOrdering = dofSeparator.GetRemainderDofOrdering(subdomain);
            if (procs.IsMasterProcess)
            {
                BooleanMatrix = LagrangeMultipliersUtilities.CalcBooleanMatrix(subdomain,
                    lagrangeMultipliers_master, numRemainderDofs, remainderDofOrdering);
            }
            else
            {
                (int numGlobalLagranges, List<SubdomainLagrangeMultiplier> subdomainLagranges) = 
                    lagrangeSerializer.Deserialize(serializedLagranges, subdomain);
                NumLagrangeMultipliers = numGlobalLagranges;
                BooleanMatrix = LagrangeMultipliersUtilities.CalcBooleanMatrix(
                    subdomain, numGlobalLagranges, subdomainLagranges, numRemainderDofs, remainderDofOrdering);

                // Alternatively I could call LagrangeMultiplierSerializer.DeserializeIncompletely(...) and its matching
                // LagrangeMultipliersUtilities.CalcBooleanMatrixFromIncompleteData(...), but that is too fragile.
            }
        }
    }
}
