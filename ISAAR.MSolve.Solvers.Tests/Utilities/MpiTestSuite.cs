using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Solvers.Tests.DomainDecomposition.Dual.FetiDP.UnitTests;
using MPI;

//TODO: Avoid forcing the user to pass the class and method names. At least find a better way to pass the method name.
//TODO: Perhaps the static method should be moved to Solvers.Tests.Program.Main(string[] args) instead of being here and called
//      by SamplesConsole.Program.Main(string[] args).
namespace ISAAR.MSolve.Solvers.Tests.Utilities
{
    public class MpiTestSuite
    {
        public static void StartTesting(string[] args)
        {
            var suite = new MpiTestSuite();
            suite.AddTest(FetiDPDofSeparatorMpiTests.TestDofSeparation, typeof(FetiDPDofSeparatorMpiTests).Name, "TestDofSeparation");
            suite.AddTest(FetiDPDofSeparatorMpiTests.TestCornerBooleanMatrices, typeof(FetiDPDofSeparatorMpiTests).Name, "TestCornerBooleanMatrices");
            suite.AddTest(LagrangeMultiplierEnumeratorMpiTests.TestBooleanMappingMatrices, typeof(LagrangeMultiplierEnumeratorMpiTests).Name, "TestBooleanMappingMatrices");
            suite.RunTests(args);
        }

        private readonly List<(Action test, string className, string methodName)> tests =
            new List<(Action test, string className, string methodName)>();

        public void AddTest(Action test, string className, string methodName) => tests.Add((test, className, methodName));

        public void RunTests(string[] args)
        {
            using (new MPI.Environment(ref args))
            {
                Intracommunicator comm = Communicator.world;
                string header = $"Process {comm.Rank}: ";

                Console.WriteLine(header + "Starting running tests.");
                for (int t = 0; t < tests.Count; ++t)
                {
                    (Action test, string className, string methodName) = tests[t];
                    comm.Barrier();
                    try
                    {
                        test();
                        Console.WriteLine(header + $"Test {t} - {className}.{methodName} passed!");
                    }
                    catch (Exception)
                    {
                        Console.WriteLine(header + $"Test {t} - {className}.{methodName} failed!");
                    }
                }
                comm.Barrier();
                Console.WriteLine(header + "All tests were completed.");
            }
        }
    }
}
