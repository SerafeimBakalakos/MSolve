using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Commons;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.LinearAlgebra.Iterative;
using ISAAR.MSolve.LinearAlgebra.Iterative.PreconditionedConjugateGradient;
using ISAAR.MSolve.LinearAlgebra.Iterative.Preconditioning;
using ISAAR.MSolve.LinearAlgebra.Iterative.Termination;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Solvers.Assemblers;
using ISAAR.MSolve.Solvers.LinearSystems;
using MGroup.Solvers.DomainDecomposition.Prototypes.LinearAlgebraExtensions;

namespace MGroup.Solvers.DomainDecomposition.Prototypes.PSM
{
    public class PsmDistributedSolver : PsmSolver
    {
        private readonly PsmDistributedDofs distributedDofs;

        public PsmDistributedSolver(IStructuralModel model, bool homogeneousProblem, double pcgTolerance, int maxPcgIterations)
            : base(model, homogeneousProblem, pcgTolerance, maxPcgIterations)
        {
            this.distributedDofs = new PsmDistributedDofs(model, dofs);
        }

        public override void Solve()
        {
            BlockMatrix Mbe = PrepareDofs();
            int[][] multiplicities = PrepareMultiplicities();
            Mbe.RowMultiplicities = multiplicities;
            Mbe.ColMultiplicities = multiplicities;
            BlockMatrix Sbbe = PrepareMatrices();
            Sbbe.RowMultiplicities = multiplicities;
            Sbbe.ColMultiplicities = multiplicities;
            BlockVector FbeCondensed = PrepareRhsVectors();
            FbeCondensed.Multiplicities = multiplicities;

            // Interface problem
            var interfaceMatrix = new ChainVectorMultipliable(Mbe, Sbbe);
            BlockVector interfaceRhs = Mbe * FbeCondensed;
            var interfaceSolution = interfaceRhs.CreateZeroVectorWithSameFormat();

            // Interface problem solution using CG
            var pcgBuilder = new PcgAlgorithm.Builder();
            pcgBuilder.ResidualTolerance = pcgTolerance;
            pcgBuilder.MaxIterationsProvider = new FixedMaxIterationsProvider(maxPcgIterations);
            PcgAlgorithm pcg = pcgBuilder.Build();
            this.PcgStats = pcg.Solve(interfaceMatrix, new IdentityPreconditioner(), interfaceRhs, interfaceSolution, true,
                interfaceSolution.CreateZeroVectorWithSameFormat);

            FindFreeDisplacements(interfaceSolution);
        }

        protected override BlockMatrix PrepareDofs()
        {
            dofs.FindDofs();
            distributedDofs.Prepare();
            var Mbe = BlockMatrix.Create(dofs.NumSubdomainDofsBoundary, dofs.NumSubdomainDofsBoundary);
            foreach (ISubdomain rowSub in model.Subdomains)
            {
                foreach (ISubdomain colSub in model.Subdomains)
                {
                    Mbe.AddBlock(rowSub.ID, colSub.ID, distributedDofs.SubdomainMatricesMb[rowSub.ID][colSub.ID]);
                }
            }
            return Mbe;
        }

        private void FindFreeDisplacements(BlockVector boundaryDisplacements)
        {
            foreach (ISubdomain subdomain in model.Subdomains)
            {
                int s = subdomain.ID;
                Vector Ub = boundaryDisplacements.Blocks[s];
                vectors.CalcFreeDisplacements(s, Ub);
                InternalLinearSystems[s].SolutionConcrete = vectors.Uf[s];
            }
        }

        private int[][] PrepareMultiplicities()
        {
            var multiplicities = new int[model.Subdomains.Count][];
            foreach (ISubdomain subdomain in model.Subdomains)
            {
                int s = subdomain.ID;
                multiplicities[s] = distributedDofs.SubdomainBoundaryDofMultiplicities[s];
            }
            return multiplicities;
        }
    }
}
