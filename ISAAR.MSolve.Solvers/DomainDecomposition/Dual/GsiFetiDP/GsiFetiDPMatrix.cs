using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.LinearAlgebra.Iterative;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Vectors;

namespace ISAAR.MSolve.Solvers.DomainDecomposition.Dual.GsiFetiDP
{
    public class GsiFetiDPMatrix : ILinearTransformation
    {
        public readonly IModel model;

        public GsiFetiDPMatrix(IModel model)
        {
            this.model = model;
        }

        public Dictionary<ISubdomain, CsrMatrix> MatricesKff { get; set; }

        public int NumColumns => model.GlobalDofOrdering.NumGlobalFreeDofs;

        public int NumRows => NumColumns;

        public void Multiply(IVectorView lhsVector, IVector rhsVector)
        {
            rhsVector.Clear();
            foreach (ISubdomain subdomain in model.EnumerateSubdomains())
            {
                var subdomainLhs = Vector.CreateZero(subdomain.FreeDofOrdering.NumFreeDofs);
                model.GlobalDofOrdering.ExtractVectorSubdomainFromGlobal(subdomain, lhsVector, subdomainLhs);
                Vector subdomainRhs = MatricesKff[subdomain].Multiply(subdomainLhs);
                model.GlobalDofOrdering.AddVectorSubdomainToGlobal(subdomain, subdomainRhs, rhsVector);
            }
        }
    }
}
