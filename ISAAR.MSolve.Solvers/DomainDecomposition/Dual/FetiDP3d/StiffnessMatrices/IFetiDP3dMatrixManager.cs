using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Matrices.Operators;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.CornerNodes;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.DofSeparation;
using ISAAR.MSolve.Solvers.Ordering.Reordering;

//TODO: Unify this with IFetiDPMatrixManager. Rename anything with GlobalCorner dofs as CoarseProblemDofs
namespace ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP3d.StiffnessMatrices
{

    public interface IFetiDP3dMatrixManager : IFetiMatrixManager, IFetiDPSeparatedDofReordering
    {
        IFetiDP3dSubdomainMatrixManager GetFetiDPSubdomainMatrixManager(ISubdomain subdomain);

        Vector CoarseProblemRhs { get; }

        void CalcCoarseProblemRhs();
        void CalcInverseCoarseProblemMatrix(ICornerNodeSelection cornerNodeSelection);

        void ClearCoarseProblemRhs();
        void ClearInverseCoarseProblemMatrix();

        Vector MultiplyInverseCoarseProblemMatrix(Vector vector);
    }
}
