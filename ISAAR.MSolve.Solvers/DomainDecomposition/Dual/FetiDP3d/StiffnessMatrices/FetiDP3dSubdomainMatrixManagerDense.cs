using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Triangulation;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Solvers.Assemblers;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.DofSeparation;
using ISAAR.MSolve.Solvers.LinearSystems;

namespace ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP3d.StiffnessMatrices
{
    public class FetiDP3dSubdomainMatrixManagerDense : IFetiDP3dSubdomainMatrixManager
    {
        private readonly SkylineAssembler assembler = new SkylineAssembler();
        private readonly IFetiDPDofSeparator dofSeparator;
        private readonly SingleSubdomainSystem<SkylineMatrix> linearSystem;
        private readonly ISubdomain subdomain;

        private CholeskyFull inverseKrr;
        private Matrix Kcc, Krc, Krr;
        private Matrix _KccStar, _KcmStar, _KmmStar;


        public FetiDP3dSubdomainMatrixManagerDense(ISubdomain subdomain, IFetiDPDofSeparator dofSeparator)
        {
            this.subdomain = subdomain;
            this.dofSeparator = dofSeparator;
            this.linearSystem = new SingleSubdomainSystem<SkylineMatrix>(subdomain);
        }

        public IMatrixView KccStar => _KccStar;

        public IMatrixView KcmStar => _KcmStar;

        public IMatrixView KmmStar => _KmmStar;

        public ISingleSubdomainLinearSystem LinearSystem => linearSystem;

        public (IMatrix Kff, IMatrixView Kfc, IMatrixView Kcf, IMatrixView Kcc) BuildFreeConstrainedMatrices(
            ISubdomainFreeDofOrdering freeDofOrdering, ISubdomainConstrainedDofOrdering constrainedDofOrdering, 
            IEnumerable<IElement> elements, IElementMatrixProvider matrixProvider)
        {
            throw new NotImplementedException();
        }

        public void BuildFreeDofsMatrix(ISubdomainFreeDofOrdering dofOrdering, IElementMatrixProvider matrixProvider)
        {
            linearSystem.Matrix = assembler.BuildGlobalMatrix(dofOrdering,
                linearSystem.Subdomain.EnumerateElements(), matrixProvider);
        }

        public void CalcInverseKii(bool diagonalOnly)
        {
            throw new NotImplementedException();
        }

        public void CalcSubdomainKStarMatrices()
        {
            // Top left
            // KccStar[s] = Kcc[s] - Krc[s]^T * inv(Krr[s]) * Krc[s]
            Matrix invKrrTimesKrc = inverseKrr.SolveLinearSystems(Krc);
            _KccStar = Kcc - Krc.MultiplyRight(invKrrTimesKrc, true);

            // Top right


            // Bottom right

        }

        public void ClearMatrices()
        {
            throw new NotImplementedException();

        }

        public void ExtractCornerRemainderSubmatrices()
        {
            int[] cornerDofs = dofSeparator.GetCornerDofIndices(subdomain);
            int[] remainderDofs = dofSeparator.GetRemainderDofIndices(subdomain);
            Kcc = linearSystem.Matrix.GetSubmatrixFull(cornerDofs, cornerDofs);
            Krc = linearSystem.Matrix.GetSubmatrixFull(remainderDofs, cornerDofs);
            Krr = linearSystem.Matrix.GetSubmatrixFull(remainderDofs, remainderDofs);
        }

        public void ExtractKbb()
        {
            throw new NotImplementedException();
        }

        public void ExtractKbiKib()
        {
            throw new NotImplementedException();
        }

        public void HandleDofOrderingWillBeModified()
        {
            throw new NotImplementedException();
        }

        public void InvertKrr(bool inPlace) => inverseKrr = Krr.FactorCholesky(inPlace);

        public Vector MultiplyInverseKiiTimes(Vector vector, bool diagonalOnly)
        {
            throw new NotImplementedException();
        }

        public Matrix MultiplyInverseKiiTimes(Matrix matrix, bool diagonalOnly)
        {
            throw new NotImplementedException();
        }

        public Vector MultiplyKbbTimes(Vector vector)
        {
            throw new NotImplementedException();
        }

        public Matrix MultiplyKbbTimes(Matrix matrix)
        {
            throw new NotImplementedException();
        }

        public Vector MultiplyKbiTimes(Vector vector)
        {
            throw new NotImplementedException();
        }

        public Matrix MultiplyKbiTimes(Matrix matrix)
        {
            throw new NotImplementedException();
        }

        public Vector MultiplyKibTimes(Vector vector)
        {
            throw new NotImplementedException();
        }

        public Matrix MultiplyKibTimes(Matrix matrix)
        {
            throw new NotImplementedException();
        }
    }
}
