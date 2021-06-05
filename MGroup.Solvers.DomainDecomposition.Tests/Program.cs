using System;
using MGroup.Solvers.DomainDecomposition.Tests.PSM;

namespace MGroup.Solvers.DomainDecomposition.Tests
{
    class Program
    {
        static void Main(string[] args)
        {
            MpiTestSuite.RunTestsWith4Processes();
        }
    }
}
