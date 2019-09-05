using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Iterative.PreconditionedConjugateGradient;
using ISAAR.MSolve.LinearAlgebra.Tests.Iterative;
using ISAAR.MSolve.LinearAlgebra.Tests.Utilities;

namespace ISAAR.MSolve.LinearAlgebra.Tests
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var suite = new MpiTestSuite();

            suite.AddFact(PcgMpiTests.TestPosDefSystemOnMaster, typeof(PcgMpiTests).Name, "TestPosDefSystemOnMaster");
            suite.RunTests(args);
        }
    }
}
