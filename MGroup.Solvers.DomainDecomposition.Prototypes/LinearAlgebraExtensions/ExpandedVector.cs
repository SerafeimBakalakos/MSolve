using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Vectors;

namespace MGroup.Solvers.DomainDecomposition.Prototypes.LinearAlgebraExtensions
{
    public class ExpandedVector
    {
        public SortedDictionary<int, Vector> SubdomainVectors = new SortedDictionary<int, Vector>();

        public Vector ToFullVector()
        {
            int totalLength = 0;
            foreach (int s in SubdomainVectors.Keys)
            {
                totalLength += SubdomainVectors[s].Length;
            }

            var result = Vector.CreateZero(totalLength);
            int start = 0;
            foreach (int s in SubdomainVectors.Keys)
            {
                result.CopySubvectorFrom(start, SubdomainVectors[s], 0, SubdomainVectors[s].Length);
                start += SubdomainVectors[s].Length;
            }

            return result;
        }
    }
}
