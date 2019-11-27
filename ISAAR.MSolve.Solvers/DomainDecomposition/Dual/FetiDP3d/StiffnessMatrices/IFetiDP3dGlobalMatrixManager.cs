using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Matrices.Operators;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.CornerNodes;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.DofSeparation;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.InterfaceProblem;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP3d.Augmentation;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.LagrangeMultipliers;
using ISAAR.MSolve.Solvers.Ordering.Reordering;

//TODO: Unify this with IFetiDPGlobalMatrixManager. Rename anything with GlobalCorner dofs as CoarseProblemDofs and assume more 
//      an array of stiffness matrices per subdomain
namespace ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP3d.StiffnessMatrices
{
    public interface IFetiDP3dGlobalMatrixManager
    {
        Vector CoarseProblemRhs { get; }

        void CalcCoarseProblemRhs(Dictionary<ISubdomain, Vector> condensedRhsVectors);

        void CalcInverseCoarseProblemMatrix(ICornerNodeSelection cornerNodeSelection,
            Dictionary<ISubdomain, (IMatrixView KccStar, IMatrixView KacStar, IMatrixView KaaStar)> matrices);

        void ClearCoarseProblemRhs();
        void ClearInverseCoarseProblemMatrix();

        Vector MultiplyInverseCoarseProblemMatrixTimes(Vector vector);

        DofPermutation ReorderCoarseProblemDofs();

    }
}
