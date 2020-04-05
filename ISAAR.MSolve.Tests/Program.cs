using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Distributed.Tests;
using ISAAR.MSolve.LinearAlgebra.Tests.Utilities;
using ISAAR.MSolve.Tests.FEM;

namespace ISAAR.MSolve.Solvers.Tests
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            //ProfileFetiDPCantileverBeam2D.Run();

            var suite = new MpiTestSuite();
            suite.AddFact(materialParrallelExecutionTest.TestMaterialUpdateOnly);
            //RegisterFetiDP2dUnitTests(args, suite);
            //RegisterFetiDP2dIntegrationTests(args, suite);
            //RegisterFetiDP2dPapagiannakisTests(args, suite);
            suite.RunTests(args);
        }
    }
}
