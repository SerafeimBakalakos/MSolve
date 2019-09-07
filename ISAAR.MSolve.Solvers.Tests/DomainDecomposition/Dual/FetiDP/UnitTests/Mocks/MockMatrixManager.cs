using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.CornerNodes;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.StiffnessMatrices;
using ISAAR.MSolve.Solvers.LinearSystems;
using ISAAR.MSolve.Solvers.Ordering.Reordering;

namespace ISAAR.MSolve.Solvers.Tests.DomainDecomposition.Dual.FetiDP.UnitTests.Mocks
{
    public class MockMatrixManager : IFetiDPMatrixManager
    {
        public Vector CoarseProblemRhs => Example4x4QuadsHomogeneous.VectorGlobalFcStar;

        public void CalcCoarseProblemRhs() { }

        public void CalcInverseCoarseProblemMatrix(ICornerNodeSelection cornerNodeSelection) { }

        public void ClearCoarseProblemRhs() { }

        public void ClearInverseCoarseProblemMatrix() { }

        public IFetiDPSubdomainMatrixManager GetFetiDPSubdomainMatrixManager(ISubdomain subdomain)
            => new MockSubdomainMatrixManager(subdomain);

        public IFetiSubdomainMatrixManager GetSubdomainMatrixManager(ISubdomain subdomain)
            => new MockSubdomainMatrixManager(subdomain);

        public Vector MultiplyInverseCoarseProblemMatrix(Vector vector)
        {
            if (vector != null) return Example4x4QuadsHomogeneous.MatrixGlobalKccStar.Invert() * vector;
            else return null;
        }

        public DofPermutation ReorderGlobalCornerDofs() => DofPermutation.CreateNoPermutation();

        public DofPermutation ReorderSubdomainInternalDofs(ISubdomain subdomain) => DofPermutation.CreateNoPermutation();

        public DofPermutation ReorderSubdomainRemainderDofs(ISubdomain subdomain) => DofPermutation.CreateNoPermutation();

        private class MockSubdomainMatrixManager : IFetiDPSubdomainMatrixManager
        {
            private readonly DiagonalMatrix invDii;
            private readonly Matrix invKii, invKrr, Kbb, Kbi, Kcc, Kff, Krc, Krr;

            internal MockSubdomainMatrixManager(ISubdomain subdomain)
            {
                int s = subdomain.ID;
                LinearSystem = new SingleSubdomainSystem<Matrix>(subdomain);

                Fbc = Example4x4QuadsHomogeneous.GetVectorFbc(s);
                FcStar = Example4x4QuadsHomogeneous.GetVectorFcStar(s);
                Fr = Example4x4QuadsHomogeneous.GetVectorFr(s);

                invDii = DiagonalMatrix.CreateFromArray(Example4x4QuadsHomogeneous.GetMatrixKii(s).GetDiagonalAsArray());
                invDii.Invert();

                invKii = Example4x4QuadsHomogeneous.GetMatrixKii(s).Invert();
                Kbb = Example4x4QuadsHomogeneous.GetMatrixKbb(s);
                Kbi = Example4x4QuadsHomogeneous.GetMatrixKbi(s);
                Kcc = Example4x4QuadsHomogeneous.GetMatrixKcc(s);
                KccStar = Example4x4QuadsHomogeneous.GetMatrixKccStar(s);
                Kff = Example4x4QuadsHomogeneous.GetMatrixKff(s);
                Krc = Example4x4QuadsHomogeneous.GetMatrixKrc(s);
                Krr = Example4x4QuadsHomogeneous.GetMatrixKrr(s);
                invKrr = Example4x4QuadsHomogeneous.GetMatrixKrr(s).Invert();
            }

            public Vector Fbc { get; }
            public Vector FcStar { get; }
            public Vector Fr { get; }
            public IMatrixView KccStar { get; }

            public ISingleSubdomainLinearSystem LinearSystem { get; }

            public (IMatrix Kff, IMatrixView Kfc, IMatrixView Kcf, IMatrixView Kcc) BuildFreeConstrainedMatrices(
                ISubdomainFreeDofOrdering freeDofOrdering, ISubdomainConstrainedDofOrdering constrainedDofOrdering, 
                IEnumerable<IElement> elements, IElementMatrixProvider matrixProvider)
            {
                throw new NotImplementedException();
            }

            public void BuildFreeDofsMatrix(ISubdomainFreeDofOrdering dofOrdering, IElementMatrixProvider matrixProvider) { }

            public void CalcInverseKii(bool diagonalOnly) { }
            public void ClearMatrices() { }
            public void ClearRhsVectors() { }
            public void CondenseMatricesStatically() { }
            public void CondenseRhsVectorsStatically() { }
            public void ExtractCornerRemainderRhsSubvectors() { }
            public void ExtractCornerRemainderSubmatrices() { }
            public void ExtractKbb() { }
            public void ExtractKbiKib() { }
            public void HandleDofOrderingWillBeModified() { }
            public void InvertKrr(bool inPlace) { }

            public Vector MultiplyInverseKiiTimes(Vector vector, bool diagonalOnly)
            {
                if (diagonalOnly) return invDii * vector;
                else return invKii * vector;
            }

            public Matrix MultiplyInverseKiiTimes(Matrix matrix, bool diagonalOnly)
            {
                if (diagonalOnly) return invDii * matrix;
                else return invKii * matrix;
            }

            public Vector MultiplyInverseKrrTimes(Vector vector) => invKrr * vector;
            public Vector MultiplyKbbTimes(Vector vector) => Kbb * vector;
            public Matrix MultiplyKbbTimes(Matrix matrix) => Kbb * matrix;
            public Vector MultiplyKbiTimes(Vector vector) => Kbi * vector;
            public Matrix MultiplyKbiTimes(Matrix matrix) => Kbi * matrix;
            public Vector MultiplyKccTimes(Vector vector) => Kcc * vector;
            public Vector MultiplyKcrTimes(Vector vector) => Krc.Multiply(vector, true);
            public Vector MultiplyKibTimes(Vector vector) => Kbi.Multiply(vector, true);
            public Matrix MultiplyKibTimes(Matrix matrix) => Kbi.MultiplyRight(matrix, true);
            public Vector MultiplyKrcTimes(Vector vector) => Krc * vector;

            public DofPermutation ReorderInternalDofs() => DofPermutation.CreateNoPermutation();
            public DofPermutation ReorderRemainderDofs() => DofPermutation.CreateNoPermutation();
        }
    }
}
