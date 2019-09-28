using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Tests.Utilities;
using ISAAR.MSolve.XFEM.Tests.Transfer;

namespace ISAAR.MSolve.XFEM.Tests
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            //DcbTransferTests.TestModelTransfer(args);
            //DcbTransferTests.TestEnrichmentTransfer(args);

            //HolesTransferTests.TestModelTransfer(args);
            //HolesTransferTests.TestEnrichmentTransfer(args);

            //RunMpiTests(args);

            //Paper1.DoubleCantileverBeam.Run();
            //Paper1.Holes.Run();

            COMPDYN2019.DoubleCantileverBeam.Run();
            COMPDYN2019.Holes.Run();
            //COMPDYN2019.Fillet.Run();
        }

        private static void RunMpiTests(string[] args)
        {
            var suite = new MpiTestSuite();

            //suite.AddFact(XModelMpiTests.TestModelTransfer, typeof(XModelMpiTests).Name, "TestModelTransfer");

            suite.RunTests(args);
        }
    }
}