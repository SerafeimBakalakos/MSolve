using System;
using System.Collections.Generic;
using System.Text;

namespace MGroup.Solvers.Distributed.Environments
{
    public static class IntGuids
    {
        private static readonly Random rng = new Random(13);

        public static int GetNewNonNegativeGuid() => rng.Next(); 
    }
}
