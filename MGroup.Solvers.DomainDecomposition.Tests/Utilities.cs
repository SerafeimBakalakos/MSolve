using System;
using System.Collections.Generic;
using System.Text;

namespace MGroup.Solvers.DomainDecomposition.Tests
{
    public static class Utilities
    {
        public static bool AreEqual(int[] expected, int[] computed)
        {
            if (expected.Length != computed.Length)
            {
                return false;
            }
            for (int i = 0; i < expected.Length; ++i)
            {
                if (expected[i] != computed[i])
                        {
                    return false;
                }
            }
            return true;
        }
    }
}
