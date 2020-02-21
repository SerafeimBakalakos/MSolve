using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Reordering;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP3d.StiffnessMatrices;

namespace ISAAR.MSolve.Solvers.Tests.DomainDecomposition.Dual.FetiDP3d
{
    public enum MatrixFormat
    {
        Dense, Skyline, SuiteSparse
    }

    public static class MatrixFormatSelection
    {
        public static IFetiDP3dMatrixManagerFactory DefineMatrixManagerFactory(MatrixFormat format)
        {
            if (format == MatrixFormat.Dense) return new FetiDP3dMatrixManagerFactoryDense();
            else if (format == MatrixFormat.Skyline) return new FetiDP3dMatrixManagerFactorySkyline(/*new OrderingAmdSuiteSparse()*/);
            else if (format == MatrixFormat.SuiteSparse) throw new NotImplementedException();/*return new FetiDPMatrixManagerFactorySuitesparse();*/
            else throw new NotImplementedException();
        }
    }
}
