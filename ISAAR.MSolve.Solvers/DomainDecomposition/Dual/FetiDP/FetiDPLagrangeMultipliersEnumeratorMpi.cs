using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.Discretization.Transfer;
using ISAAR.MSolve.LinearAlgebra.Matrices.Operators;
using ISAAR.MSolve.Solvers.DomainDecomposition.DofSeparation;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.LagrangeMultipliers;
using MPI;
    
namespace ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP
{
    /// <summary>
    /// Calculates the signed boolean matrices of the equations that enforce continuity between the multiple instances of 
    /// boundary dofs.
    /// Authors: Serafeim Bakalakos
    /// </summary>
    public class LagrangeMultipliersEnumeratorBaseMpi //: ILagrangeMultipliersEnumerator
    {
        private const int lagrangeDefinitionTag = 0;

        private readonly Intracommunicator comm;
        private readonly ICrosspointStrategy crosspointStrategy;
        private readonly FetiDPDofSeparatorMpi dofSeparator;
        private readonly LagrangeMultiplierSerializer lagrangeSerializer;
        private readonly int masterProcess;
        private readonly int rank;
        private readonly ISubdomain subdomain;
        private readonly Dictionary<int, INode> subdomainNodes;

        protected LagrangeMultipliersEnumeratorBaseMpi(ISubdomain subdomain, Dictionary<int, INode> subdomainNodes, 
            ICrosspointStrategy crosspointStrategy, FetiDPDofSeparatorMpi dofSeparator, Intracommunicator comm,
            int masterProcess, IDofSerializer dofSerializer)
        {
            this.subdomain = subdomain;
            this.subdomainNodes = subdomainNodes;
            this.crosspointStrategy = crosspointStrategy;
            this.dofSeparator = dofSeparator;
            this.comm = comm;
            this.rank = comm.Rank;
            this.masterProcess = masterProcess;
            this.lagrangeSerializer = new LagrangeMultiplierSerializer(dofSerializer);
        }

        /// <summary>
        /// One of this is stored per process.
        /// </summary>
        public SignedBooleanMatrixColMajor BooleanMatrix { get; private set; }

        /// <summary>
        /// This is only available in the master process.
        /// </summary>
        public LagrangeMultiplier[] LagrangeMultipliers { get; private set; }

        /// <summary>
        /// One of this is stored per process.
        /// </summary>
        public int NumLagrangeMultipliers { get; private set; }

        protected void CalcBooleanMatrices()
        { 
            // Define the lagrange multipliers and serialize and broadcast them to other processes
            int[] serializedLagranges = null;
            if (rank == masterProcess)
            {
                LagrangeMultipliers = LagrangeMultipliersUtilities.DefineLagrangeMultipliers(
                    dofSeparator.GlobalDofs, crosspointStrategy).ToArray();
                NumLagrangeMultipliers = LagrangeMultipliers.Length;
                serializedLagranges = lagrangeSerializer.Serialize(LagrangeMultipliers);
            }
            MpiUtilities.BroadcastArray(comm, ref serializedLagranges, masterProcess, lagrangeDefinitionTag);

            // Deserialize the lagrange multipliers in other processes and calculate the boolean matrices
            int numRemainderDofs = dofSeparator.SubdomainDofs.RemainderDofIndices.Length;
            DofTable remainderDofOrdering = dofSeparator.SubdomainDofs.RemainderDofOrdering;
            if (rank == masterProcess)
            {
                BooleanMatrix = LagrangeMultipliersUtilities.CalcBooleanMatrix(
                    subdomain, LagrangeMultipliers, numRemainderDofs, remainderDofOrdering);
            }
            else
            {
                (int numGlobalLagranges, List<SubdomainLagrangeMultiplier> subdomainLagranges) = 
                    lagrangeSerializer.Deserialize(serializedLagranges, subdomain, subdomainNodes);
                NumLagrangeMultipliers = numGlobalLagranges;
                BooleanMatrix = LagrangeMultipliersUtilities.CalcBooleanMatrix(
                    subdomain, numGlobalLagranges, subdomainLagranges, numRemainderDofs, remainderDofOrdering);

                // Alternatively I could call LagrangeMultiplierSerializer.DeserializeIncompletely(...) and its matching
                // LagrangeMultipliersUtilities.CalcBooleanMatrixFromIncompleteData(...), but that is too fragile.
            }
        }
    }
}
