using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ISAAR.MSolve.Discretization.Exceptions;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.Discretization.Transfer;
using ISAAR.MSolve.LinearAlgebra.Matrices.Operators;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.LagrangeMultipliers;

namespace ISAAR.MSolve.Solvers.DomainDecomposition.Dual.StiffnessDistribution
{
    public class HomogeneousStiffnessDistributionMpi : IStiffnessDistributionMpi
    {
        private const int multiplicityTag = 0;

        /// <summary>
        /// Each process stores only the ones corresponding to its subdomain. Master stores all of them.
        /// </summary>
        private readonly Dictionary<int, double[]> inverseBoundaryDofMultiplicities; 

        private readonly FetiDPDofSeparatorMpi dofSeparator;
        private readonly IModel model;
        private readonly ProcessDistribution procs;

        public HomogeneousStiffnessDistributionMpi(ProcessDistribution processDistribution, IModel model,
            FetiDPDofSeparatorMpi dofSeparator)
        {
            this.procs = processDistribution;
            this.model = model;
            this.dofSeparator = dofSeparator;
            this.inverseBoundaryDofMultiplicities = new Dictionary<int, double[]>();
        }

        public double[] CalcBoundaryDofCoefficients(ISubdomain subdomain) => inverseBoundaryDofMultiplicities[subdomain.ID];

        public Dictionary<int, double> CalcBoundaryDofCoefficients(INode node, IDofType dofType)
            => HomogeneousStiffnessDistributionUtilities.CalcBoundaryDofCoefficients(node, dofType);

        public IMappingMatrix CalcBoundaryPreconditioningSignedBooleanMatrices(ILagrangeMultipliersEnumerator lagrangeEnumerator,
            ISubdomain subdomain, SignedBooleanMatrixColMajor boundarySignedBooleanMatrix)
        {
            if (subdomain.ID == procs.OwnSubdomainID)
            {
                return new HomogeneousStiffnessDistributionUtilities.ScalingBooleanMatrixImplicit(
                    inverseBoundaryDofMultiplicities[procs.OwnSubdomainID], boundarySignedBooleanMatrix);
            }
            else
            {
                throw new MpiException(
                    $"Process {procs.OwnRank}: Only defined for master process (rank = {procs.MasterProcess})");
            }
        }

        public void Update() 
        {
            // Calculate and store the inverse boundary dof multiplicities of each subdomain in its process
            ISubdomain subdomain = model.GetSubdomain(procs.OwnSubdomainID);
            int[] multiplicities = null; // Will be sent to master process
            if (subdomain.ConnectivityModified) //TODO: Is this what I should check?
            {
                (multiplicities, this.inverseBoundaryDofMultiplicities[subdomain.ID]) =
                    HomogeneousStiffnessDistributionUtilities.CalcBoundaryDofMultiplicities(
                        subdomain, dofSeparator.SubdomainDofs.BoundaryDofs);
            }

            // Gather all boundary dof multiplicites in master
            if (procs.IsMasterProcess)
            {
                // Receive the boundary multiplicities of each subdomain
                IEnumerable<ISubdomain> modifiedSubdomains = model.EnumerateSubdomains().Where(
                    sub => sub.ConnectivityModified && sub.ID != procs.OwnSubdomainID); 
                var gatheredMultiplicities = new Dictionary<ISubdomain, int[]>();
                foreach (ISubdomain sub in modifiedSubdomains)
                {
                    int source = procs.GetProcessOfSubdomain(sub.ID);
                    //Console.WriteLine($"Process {procs.OwnRank} (master): Started receiving multiplicites from process {source}.");
                    gatheredMultiplicities[sub] = MpiUtilities.ReceiveArray<int>(procs.Communicator, source, multiplicityTag);
                    //Console.WriteLine($"Process {procs.OwnRank} (master): Finished receiving multiplicites from process {source}.");
                }

                // After finishing with all comunications, invert the multiplicities. //TODO: Perhaps this should be done concurrently with the transfers, by another thread.
                foreach (ISubdomain sub in modifiedSubdomains)
                {
                    //Console.WriteLine($"Process {procs.OwnRank} (master): Started inverting multiplicites of subdomain {subdomain.ID}.");
                    this.inverseBoundaryDofMultiplicities[sub.ID] = Invert(gatheredMultiplicities[sub]);
                    //Console.WriteLine($"Process {procs.OwnRank} (master): Finished inverting multiplicites of subdomain {subdomain.ID}.");
                    gatheredMultiplicities.Remove(sub); // Free up some temporary memory.
                }
            }
            else
            {
                if (subdomain.ConnectivityModified)
                {
                    //Console.WriteLine($"Process {procs.OwnRank}: Started sending multiplicities it to master");
                    MpiUtilities.SendArray(procs.Communicator, multiplicities, procs.MasterProcess, multiplicityTag);
                    //Console.WriteLine($"Process {procs.OwnRank}: Finished sending multiplicities to master.");
                }
            }
        }

        private static double[] Invert(int[] multiplicities)
        {
            var inverse = new double[multiplicities.Length];
            for (int i = 0; i < multiplicities.Length; ++i) inverse[i] = 1.0 / multiplicities[i];
            return inverse;
        }
    }
}