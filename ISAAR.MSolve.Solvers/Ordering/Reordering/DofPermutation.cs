using System;
using System.Collections.Generic;
using System.Text;

namespace ISAAR.MSolve.Solvers.Ordering.Reordering
{
    public class DofPermutation
    {
        public DofPermutation(bool isBetter, int[] permutationArray, bool permutationIsOldToNew)
        {
            this.IsBetter = isBetter;
            this.PermutationArray = permutationArray;
            this.PermutationIsOldToNew = permutationIsOldToNew;
        }

        public bool IsBetter { get; }
        public int[] PermutationArray { get; }
        public bool PermutationIsOldToNew { get; }

        public int[] ReorderKeysOfDofIndicesMap(int[] dofIndicesMap)
        {
            int numDofs = PermutationArray.Length;
            var result = new int[numDofs];
            if (PermutationIsOldToNew)
            {
                for (int i = 0; i < numDofs; ++i) result[PermutationArray[i]] = dofIndicesMap[i]; // i is old index
            }
            else
            {
                for (int i = 0; i < numDofs; ++i) result[i] = dofIndicesMap[PermutationArray[i]]; // i is new index
            }
            return result;
        }
    }
}
