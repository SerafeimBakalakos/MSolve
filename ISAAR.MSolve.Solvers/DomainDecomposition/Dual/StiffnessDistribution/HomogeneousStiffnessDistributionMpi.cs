using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.Discretization.Transfer;
using ISAAR.MSolve.LinearAlgebra.Matrices.Operators;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.DofSeparation;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.LagrangeMultipliers;

// 
namespace ISAAR.MSolve.Solvers.DomainDecomposition.Dual.StiffnessDistribution
{
    public class HomogeneousStiffnessDistributionMpi : IStiffnessDistribution
    {
        private const int multiplicityTag = 0;

        private readonly IFetiDPDofSeparator dofSeparator;

        /// <summary>
        /// Each process stores only the ones corresponding to its subdomain. Master stores all of them.
        /// </summary>
        private readonly Dictionary<ISubdomain, double[]> inverseBoundaryDofMultiplicities;

        private readonly IHomogeneousDistributionLoadScaling loadScaling;
        private readonly IModel model;
        private readonly ProcessDistribution procs;

        public HomogeneousStiffnessDistributionMpi(ProcessDistribution processDistribution, IModel model,
            IFetiDPDofSeparator dofSeparator, IHomogeneousDistributionLoadScaling loadScaling)
        {
            this.procs = processDistribution;
            this.model = model;
            this.dofSeparator = dofSeparator;
            this.loadScaling = loadScaling;
            this.inverseBoundaryDofMultiplicities = new Dictionary<ISubdomain, double[]>();
        }

        public double[] GetBoundaryDofCoefficients(ISubdomain subdomain)
        {
            procs.CheckProcessMatchesSubdomain(subdomain.ID);
            return inverseBoundaryDofMultiplicities[subdomain];
        }

        public IMappingMatrix CalcBoundaryPreconditioningSignedBooleanMatrix(ILagrangeMultipliersEnumerator lagrangeEnumerator,
            ISubdomain subdomain, SignedBooleanMatrixColMajor boundarySignedBooleanMatrix)
        {
            procs.CheckProcessMatchesSubdomain(subdomain.ID);
            return new HomogeneousStiffnessDistributionUtilities.ScalingBooleanMatrixImplicit(
                inverseBoundaryDofMultiplicities[subdomain], boundarySignedBooleanMatrix);
        }

        /// <summary>
        /// This is not necessary in a typical execution. It would be useful e.g. to create a global vector with all 
        /// displacements.
        /// </summary>
        public void GatherDataInMaster()
        {
            // Gather all boundary dof multiplicites in master. It is faster to send int[] than double[].
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
                    this.inverseBoundaryDofMultiplicities[sub] = Invert(gatheredMultiplicities[sub]);
                    //Console.WriteLine($"Process {procs.OwnRank} (master): Finished inverting multiplicites of subdomain {subdomain.ID}.");
                    gatheredMultiplicities.Remove(sub); // Free up some temporary memory.
                }
            }
            else
            {
                ISubdomain subdomain = model.GetSubdomain(procs.OwnSubdomainID);
                if (subdomain.ConnectivityModified)
                {
                    //Console.WriteLine($"Process {procs.OwnRank}: Started sending multiplicities it to master");
                    int[] multiplicities = Invert(inverseBoundaryDofMultiplicities[subdomain]);
                    MpiUtilities.SendArray(procs.Communicator, multiplicities, procs.MasterProcess, multiplicityTag);
                    //Console.WriteLine($"Process {procs.OwnRank}: Finished sending multiplicities to master.");
                }
            }
        }

        public double ScaleNodalLoad(ISubdomain subdomain, INodalLoad load) => loadScaling.ScaleNodalLoad(subdomain, load);

        public void Update() 
        {
            // Calculate and store the inverse boundary dof multiplicities of each subdomain in its process
            ISubdomain subdomain = model.GetSubdomain(procs.OwnSubdomainID);
            if (subdomain.ConnectivityModified) //TODO: Is this what I should check?
            {
                inverseBoundaryDofMultiplicities[subdomain] = 
                    HomogeneousStiffnessDistributionUtilities.CalcBoundaryDofInverseMultiplicities(
                        subdomain, dofSeparator.GetBoundaryDofs(subdomain));
            }
        }

        private static int[] Invert(double[] oneOverIntegers)
        {
            var inverse = new int[oneOverIntegers.Length];
            for (int i = 0; i < oneOverIntegers.Length; ++i) inverse[i] = (int)(Math.Round(1.0 / oneOverIntegers[i]));
            return inverse;
        }

        private static double[] Invert(int[] integers)
        {
            var inverse = new double[integers.Length];
            for (int i = 0; i < integers.Length; ++i) inverse[i] = 1.0 / integers[i];
            return inverse;
        }
    }
}