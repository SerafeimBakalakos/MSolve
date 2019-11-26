using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Distributed.Tests.Iterative;
using ISAAR.MSolve.LinearAlgebra.Distributed.Tests.Tranfer;

namespace ISAAR.MSolve.LinearAlgebra.Distributed.Tests
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            RunTestsWith4MpiProcesses(args);
            //RunTestsWith8MpiProcesses(args);
        }

        private static void RunTestsWith4MpiProcesses(string[] args)
        {
            var suite = new MpiTestSuite();

            TransferrerScatterTests.RegisterAllTests(suite);
            TransferrerGatherTests.RegisterAllTests(suite);
            //suite.AddFact(PcgMpiTests.TestPosDefSystemOnMaster, typeof(PcgMpiTests).Name, "TestPosDefSystemOnMaster");

            suite.RunTests(args);
        }
    }
}
