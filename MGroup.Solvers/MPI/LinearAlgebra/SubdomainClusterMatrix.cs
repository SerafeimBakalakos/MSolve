using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Iterative;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Vectors;

//TODOMPI: Parallelization of operations per subdomain
namespace MGroup.Solvers.MPI.LinearAlgebra
{
    public class SubdomainClusterMatrix : ILinearTransformation
    {
        public SubdomainClusterMatrix(int order)
        {
            this.NumRows = order;
            this.NumColumns = order;
        }

        public int NumColumns { get; }

        public int NumRows { get; }

        public Dictionary<int, IMatrixView> SubdomainMatrices { get; } = new Dictionary<int, IMatrixView>();

        public Dictionary<int, int[]> SubdomainToClusterDofs { get; } = new Dictionary<int, int[]>();

        public void Multiply(IVectorView lhsVector, IVector rhsVector)
        {
            var x = (Vector)lhsVector;
            var y = (Vector)rhsVector;
            y.Clear();
            foreach (int s in SubdomainMatrices.Keys)
            {
                IMatrixView As = SubdomainMatrices[s];
                var xs = Vector.CreateZero(As.NumColumns); //TODO: This can be cached
                var ys = Vector.CreateZero(As.NumRows);    //TODO: This can be cached
                int[] subdomainToClusterDofs = SubdomainToClusterDofs[s];
                xs.CopyNonContiguouslyFrom(x, subdomainToClusterDofs);
                As.MultiplyIntoResult(xs, ys);
                y.AddIntoThisNonContiguouslyFrom(subdomainToClusterDofs, ys);
            }
        }
    }
}
