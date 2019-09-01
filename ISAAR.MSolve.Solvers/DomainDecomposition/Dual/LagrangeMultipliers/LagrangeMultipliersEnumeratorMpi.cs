using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.Discretization.Transfer;
using ISAAR.MSolve.LinearAlgebra.Matrices.Operators;
using ISAAR.MSolve.Solvers.DomainDecomposition.DofSeparation;

namespace ISAAR.MSolve.Solvers.DomainDecomposition.Dual.LagrangeMultipliers
{
    public class LagrangeMultipliersEnumeratorMpi : ILagrangeMultipliersEnumerator
    {
        private readonly ICrosspointStrategy crosspointStrategy;
        private readonly IDofSeparator dofSeparator;
        private readonly LagrangeMultiplierSerializer lagrangeSerializer;
        private readonly IModel model;
        private readonly ProcessDistribution procs;

        private List<LagrangeMultiplier> lagrangeMultipliers_master;
        private SignedBooleanMatrixColMajor subdomainBooleanMatrix;

        public LagrangeMultipliersEnumeratorMpi(ProcessDistribution processDistribution, IModel model,
            ICrosspointStrategy crosspointStrategy, IDofSeparator dofSeparator)
        {
            this.procs = processDistribution;
            this.model = model;
            this.crosspointStrategy = crosspointStrategy;
            this.dofSeparator = dofSeparator;
            this.lagrangeSerializer = new LagrangeMultiplierSerializer(model.DofSerializer);
        }

        public IReadOnlyList<LagrangeMultiplier> LagrangeMultipliers
        {
            get
            {
                procs.CheckProcessIsMaster();
                return lagrangeMultipliers_master;
            }
        }

        public int NumLagrangeMultipliers { get; private set; }

        public SignedBooleanMatrixColMajor GetBooleanMatrix(ISubdomain subdomain)
        {
            procs.CheckProcessMatchesSubdomain(subdomain.ID);
            return subdomainBooleanMatrix;
        }

        public void CalcBooleanMatrices(Func<ISubdomain, DofTable> getSubdomainDofOrdering)
        {
            // Define the lagrange multipliers and serialize and broadcast them to other processes
            int[] serializedLagranges = null;
            if (procs.IsMasterProcess)
            {
                lagrangeMultipliers_master = LagrangeMultipliersUtilities.DefineLagrangeMultipliers(
                    dofSeparator.GlobalBoundaryDofs, crosspointStrategy);
                NumLagrangeMultipliers = lagrangeMultipliers_master.Count;
                serializedLagranges = lagrangeSerializer.Serialize(lagrangeMultipliers_master);
            }
            MpiUtilities.BroadcastArray(procs.Communicator, ref serializedLagranges, procs.MasterProcess);

            // Deserialize the lagrange multipliers in other processes and calculate the boolean matrices
            ISubdomain subdomain = model.GetSubdomain(procs.OwnSubdomainID);
            DofTable subdomainDofOrdering = getSubdomainDofOrdering(subdomain);
            if (procs.IsMasterProcess)
            {
                subdomainBooleanMatrix = LagrangeMultipliersUtilities.CalcSubdomainBooleanMatrix(subdomain,
                    lagrangeMultipliers_master, subdomainDofOrdering);
            }
            else
            {
                (int numGlobalLagranges, List<SubdomainLagrangeMultiplier> subdomainLagranges) =
                    lagrangeSerializer.Deserialize(serializedLagranges, subdomain);
                NumLagrangeMultipliers = numGlobalLagranges;
                subdomainBooleanMatrix = LagrangeMultipliersUtilities.CalcSubdomainBooleanMatrix(
                    numGlobalLagranges, subdomainLagranges, subdomainDofOrdering);

                // Alternatively I could call LagrangeMultiplierSerializer.DeserializeIncompletely(...) and its matching
                // LagrangeMultipliersUtilities.CalcBooleanMatrixFromIncompleteData(...), but that is too fragile.
            }
        }
    }
}
