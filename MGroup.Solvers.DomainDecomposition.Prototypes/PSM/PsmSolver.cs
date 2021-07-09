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
    public class PsmSolver : ISAAR.MSolve.Solvers.ISolver
    {
        protected readonly IStructuralModel model;
        protected readonly double pcgTolerance;
        protected readonly int maxPcgIterations;
        protected readonly PsmDofs dofs;
        protected readonly IPrimalScaling scaling;
        protected readonly PsmStiffnesses stiffnesses;
        protected readonly PsmVectors vectors;
        protected readonly DenseMatrixAssembler assembler = new DenseMatrixAssembler();

        public PsmSolver(IStructuralModel model, bool homogeneousProblem, double pcgTolerance, int maxPcgIterations)
        {
            this.model = model;
            this.pcgTolerance = pcgTolerance;
            this.maxPcgIterations = maxPcgIterations;
            this.dofs = new PsmDofs(model);
            this.stiffnesses = new PsmStiffnesses(model, dofs);
            this.vectors = new PsmVectors(dofs, stiffnesses);

            var externalLinearSystems = new Dictionary<int, ILinearSystem>();
            LinearSystems = externalLinearSystems;
            InternalLinearSystems = new Dictionary<int, SingleSubdomainSystem<Matrix>>();
            foreach (ISubdomain subdomain in model.Subdomains)
            {
                var system = new SingleSubdomainSystem<Matrix>(subdomain);
                externalLinearSystems[subdomain.ID] = system;
                InternalLinearSystems[subdomain.ID] = system;
            }

            if (homogeneousProblem)
            {
                scaling = new HomogeneousScaling(model);
            }
            else
            {
                scaling = new HeterogeneousScaling(model);
            }
        }

        public Dictionary<int, SingleSubdomainSystem<Matrix>> InternalLinearSystems { get; }
        public IReadOnlyDictionary<int, ILinearSystem> LinearSystems { get; }

        public ISAAR.MSolve.Solvers.SolverLogger Logger => throw new NotImplementedException();

        public string Name => throw new NotImplementedException();

        public IterativeStatistics PcgStats { get; set; }

        public Dictionary<int, IMatrix> BuildGlobalMatrices(IElementMatrixProvider elementMatrixProvider)
        {
            var matricesKff = new Dictionary<int, IMatrix>();
            foreach (ISubdomain subdomain in model.Subdomains)
            {
                Matrix Kff = assembler.BuildGlobalMatrix(subdomain.FreeDofOrdering, subdomain.Elements, elementMatrixProvider);
                LinearSystems[subdomain.ID].Matrix = Kff;
                matricesKff[subdomain.ID] = Kff;
            }
            return matricesKff;
        }

        public Dictionary<int, (IMatrix matrixFreeFree, IMatrixView matrixFreeConstr, IMatrixView matrixConstrFree, IMatrixView matrixConstrConstr)> BuildGlobalSubmatrices(IElementMatrixProvider elementMatrixProvider)
        {
            throw new NotImplementedException();
        }

        public Dictionary<int, SparseVector> DistributeNodalLoads(Table<INode, IDofType, double> nodalLoads)
            => scaling.DistributeNodalLoads(nodalLoads);

        public void HandleMatrixWillBeSet()
        {
            
        }

        public void Initialize()
        {
            
        }

        public Dictionary<int, Matrix> InverseSystemMatrixTimesOtherMatrix(Dictionary<int, IMatrixView> otherMatrix)
        {
            throw new NotImplementedException();
        }

        public void OrderDofs(bool alsoOrderConstrainedDofs)
        {
            var dofOrderer = new Ordering.DofOrderer();
            foreach (ISubdomain subdomain in model.Subdomains)
            {
                subdomain.FreeDofOrdering = dofOrderer.OrderFreeDofs(subdomain);
            }
        }

        public void PreventFromOverwrittingSystemMatrices()
        {
        }

        public virtual void Solve()
        {
            BlockMatrix Lbe = PrepareDofs();
            BlockMatrix Sbbe = PrepareMatrices();
            BlockVector FbeCondensed = PrepareRhsVectors();

            // Interface problem
            Matrix fullLbe = Lbe.CopyToFullMatrix();
            Matrix fullSbbe = Sbbe.CopyToFullMatrix();
            Vector fullFbeCond = FbeCondensed.CopyToFullVector();
            Matrix interfaceMatrix = fullLbe.Transpose() * fullSbbe * fullLbe;
            Vector interfaceRhs = fullLbe.Transpose() * fullFbeCond;
            var interfaceSolution = Vector.CreateZero(interfaceRhs.Length);

            // Interface problem solution using CG
            var pcgBuilder = new PcgAlgorithm.Builder();
            pcgBuilder.ResidualTolerance = pcgTolerance;
            pcgBuilder.MaxIterationsProvider = new FixedMaxIterationsProvider(maxPcgIterations);
            PcgAlgorithm pcg = pcgBuilder.Build();
            this.PcgStats = pcg.Solve(interfaceMatrix, new IdentityPreconditioner(), interfaceRhs, interfaceSolution, true,
                () => Vector.CreateZero(interfaceRhs.Length));

            FindFreeDisplacements(interfaceSolution);
        }

        protected virtual BlockMatrix PrepareDofs()
        {
            dofs.FindDofs();
            var Lbe = BlockMatrix.CreateCol(dofs.NumSubdomainDofsBoundary, dofs.NumGlobalDofsBoundary);
            foreach (ISubdomain subdomain in model.Subdomains)
            {
                int s = subdomain.ID;
                Lbe.AddBlock(s, 0, dofs.SubdomainMatricesLb[s]);
            }
            return Lbe;
        }

        protected virtual BlockMatrix PrepareMatrices()
        {
            var Sbbe = BlockMatrix.Create(dofs.NumSubdomainDofsBoundary, dofs.NumSubdomainDofsBoundary);
            foreach (ISubdomain subdomain in model.Subdomains)
            {
                int s = subdomain.ID;
                stiffnesses.CalcSchurComplements(s, InternalLinearSystems[s].Matrix);
                Sbbe.AddBlock(s, s, stiffnesses.Sbb[s]);
            }
            return Sbbe;
        }

        protected virtual BlockVector PrepareRhsVectors()
        {
            var FbeCondensed = new BlockVector(dofs.NumSubdomainDofsBoundary);
            foreach (ISubdomain subdomain in model.Subdomains)
            {
                int s = subdomain.ID;
                vectors.CalcSubdomainForces(s, InternalLinearSystems[s].RhsConcrete);
                FbeCondensed.AddBlock(s, vectors.FbCondensed[s]);
            }
            return FbeCondensed;
        }

        protected virtual void FindFreeDisplacements(Vector globalBoundaryDisplacements)
        {
            foreach (ISubdomain subdomain in model.Subdomains)
            {
                int s = subdomain.ID;
                Vector Ub = dofs.SubdomainMatricesLb[s] * globalBoundaryDisplacements;
                vectors.CalcFreeDisplacements(s, Ub);
                InternalLinearSystems[s].SolutionConcrete = vectors.Uf[s];
            }
        }
    }
}
