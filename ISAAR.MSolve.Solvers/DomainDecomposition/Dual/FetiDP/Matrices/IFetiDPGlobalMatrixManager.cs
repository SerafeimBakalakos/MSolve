using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.CornerNodes;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.DofSeparation;
using ISAAR.MSolve.Solvers.Ordering.Reordering;

//TODO: Not sure about the interface. These methods should be wrapped by IFetiDPMatrixManager
//TODO: Calculating the coarse problem rhs is also subdject to mpi/serial implementations, but it does not depend on any matrix format.
namespace ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.Matrices
{
    public interface IFetiDPGlobalMatrixManager 
    {
        //Vector CoarseProblemRhs { get; }

        //void ClearCoarseProblemMatrix();
        //void ClearCoarseProblemVector();

        //void AssembleAndInvertCoarseProblemMatrix(ICornerNodeSelection cornerNodeSelection,
        //    IFetiDPDofSeparator dofSeparator, Dictionary<ISubdomain, IMatrixView> schurComplementsOfRemainderDofs);

        //void AssembleCoarseProblemRhs(IFetiDPDofSeparator dofSeparator, Dictionary<ISubdomain, Vector> condensedRhsVectors);

        //Vector MultiplyInverseCoarseProblemMatrixTimes(Vector vector);

        //DofPermutation ReorderCornerDofs(IFetiDPDofSeparator dofSeparator);
    }
}
