using System;
using System.Collections.Generic;
using System.Text;

namespace MGroup.Solvers_OLD.DistributedTry1.Distributed.Environments
{
    public static class IntGuids
    {
        private static readonly Random rng = new Random(13);

        public static int GetNewNonNegativeGuid() => rng.Next(); 
    }
}
