//using System;
//using System.Collections.Generic;
//using System.Text;
//using ISAAR.MSolve.Discretization.Commons;
//using ISAAR.MSolve.Discretization.FreedomDegrees;
//using ISAAR.MSolve.Discretization.Interfaces;
//using ISAAR.MSolve.LinearAlgebra.Iterative;
//using ISAAR.MSolve.LinearAlgebra.Iterative.PreconditionedConjugateGradient;
//using ISAAR.MSolve.LinearAlgebra.Iterative.Preconditioning;
//using ISAAR.MSolve.LinearAlgebra.Iterative.Termination;
//using ISAAR.MSolve.LinearAlgebra.Matrices;
//using ISAAR.MSolve.LinearAlgebra.Vectors;
//using ISAAR.MSolve.Solvers.Assemblers;
//using ISAAR.MSolve.Solvers.LinearSystems;
//using MGroup.Solvers.DomainDecomposition.Prototypes.LinearAlgebraExtensions;

//namespace MGroup.Solvers.DomainDecomposition.Prototypes.PSM
//{
//    public class PsmDistributedSolver : PsmSolver
//    {
//        public PsmDistributedSolver(IStructuralModel model, bool homogeneousProblem, double pcgTolerance, int maxPcgIterations)
//            : base(model, homogeneousProblem, pcgTolerance, maxPcgIterations)
//        {
//        }

//        public override void Solve()
//        {
//            BlockMatrix Lbe = PrepareDofs();
//            BlockMatrix Sbbe = PrepareMatrices();
//            BlockVector FbeCondensed = PrepareRhsVectors();

//            // Interface problem
//            var interfaceMatrix = new ChainVectorMultiplieable(Lbe.Transpose(), Sbbe, Lbe);
//            Vector interfaceRhs = Lbe.Transpose() * FbeCondensed;
//            var interfaceSolution = Vector.CreateZero(interfaceRhs.Length);

//            // Interface problem solution using CG
//            var pcgBuilder = new PcgAlgorithm.Builder();
//            pcgBuilder.ResidualTolerance = pcgTolerance;
//            pcgBuilder.MaxIterationsProvider = new FixedMaxIterationsProvider(maxPcgIterations);
//            PcgAlgorithm pcg = pcgBuilder.Build();
//            this.PcgStats = pcg.Solve(interfaceMatrix, new IdentityPreconditioner(), interfaceRhs, interfaceSolution, true,
//                () => Vector.CreateZero(interfaceRhs.Length));

//            FindFreeDisplacements(interfaceSolution);
//        }
//    }
//}
