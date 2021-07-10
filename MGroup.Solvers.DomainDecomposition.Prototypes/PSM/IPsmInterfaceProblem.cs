using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.LinearAlgebra.Iterative;
using ISAAR.MSolve.LinearAlgebra.Iterative.PreconditionedConjugateGradient;
using ISAAR.MSolve.LinearAlgebra.Iterative.Preconditioning;
using MGroup.Solvers.DomainDecomposition.Prototypes.LinearAlgebraExtensions;

namespace MGroup.Solvers.DomainDecomposition.Prototypes.PSM
{
    public interface IPsmInterfaceProblem
    {
        PsmSubdomainDofs Dofs { get; set; }

        IStructuralModel Model { get; set; }

        void CalcMappingMatrices();

        IterativeStatistics Solve(PcgAlgorithm pcg, IPreconditioner preconditioner, 
            BlockMatrix expandedDomainMatrix, BlockVector expandedDomainRhs, BlockVector expandedDomainSolution);
    }
}
