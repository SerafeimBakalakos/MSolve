using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Solvers.Tests.DomainDecomposition.Dual.FetiDP.UnitTests;
using MPI;
using static ISAAR.MSolve.Solvers.Tests.DomainDecomposition.Dual.FetiDP.UnitTests.FetiDPMatrixManagerSerialTests;

//TODO: Needs printing the stack trace when a test fails. ASAP.
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

            suite.AddFact(FetiDPDofSeparatorMpiTests.TestDofSeparation, typeof(FetiDPDofSeparatorMpiTests).Name, "TestDofSeparation");
            suite.AddFact(FetiDPDofSeparatorMpiTests.TestCornerBooleanMatrices, typeof(FetiDPDofSeparatorMpiTests).Name, "TestCornerBooleanMatrices");
            suite.AddFact(LagrangeMultiplierEnumeratorMpiTests.TestBooleanMappingMatrices, typeof(LagrangeMultiplierEnumeratorMpiTests).Name, "TestBooleanMappingMatrices");
            suite.AddFact(HomogeneousStiffnessDistributionMpiTests.TestBooleanMappingMatrices, typeof(HomogeneousStiffnessDistributionMpiTests).Name, "TestBooleanMappingMatrices");

            suite.AddFact(FetiDPMatrixManagerMpiTests.TestVectorsFbcFr, typeof(FetiDPMatrixManagerMpiTests).Name, "TestVectorsFbcFr");
            suite.AddTheory(FetiDPMatrixManagerMpiTests.TestMatricesKccKcrKrr, typeof(FetiDPMatrixManagerMpiTests).Name, "TestMatricesKccKcrKrr", MatrixFormat.Dense);
            suite.AddTheory(FetiDPMatrixManagerMpiTests.TestMatricesKccKcrKrr, typeof(FetiDPMatrixManagerMpiTests).Name, "TestMatricesKccKcrKrr", MatrixFormat.Skyline);
            suite.AddTheory(FetiDPMatrixManagerMpiTests.TestMatricesKbbKbiKii, typeof(FetiDPMatrixManagerMpiTests).Name, "TestMatricesKbbKbiKii", MatrixFormat.Dense);
            suite.AddTheory(FetiDPMatrixManagerMpiTests.TestMatricesKbbKbiKii, typeof(FetiDPMatrixManagerMpiTests).Name, "TestMatricesKbbKbiKii", MatrixFormat.Skyline);
            suite.AddTheory(FetiDPMatrixManagerMpiTests.TestStaticCondensations, typeof(FetiDPMatrixManagerMpiTests).Name, "TestStaticCondensations", MatrixFormat.Dense);
            suite.AddTheory(FetiDPMatrixManagerMpiTests.TestStaticCondensations, typeof(FetiDPMatrixManagerMpiTests).Name, "TestStaticCondensations", MatrixFormat.Skyline);
            suite.AddTheory(FetiDPMatrixManagerMpiTests.TestCoarseProblemMatrixAndRhs, typeof(FetiDPMatrixManagerMpiTests).Name, "TestCoarseProblemMatrixAndRhs", MatrixFormat.Dense);
            suite.AddTheory(FetiDPMatrixManagerMpiTests.TestCoarseProblemMatrixAndRhs, typeof(FetiDPMatrixManagerMpiTests).Name, "TestCoarseProblemMatrixAndRhs", MatrixFormat.Skyline);

            suite.AddFact(FetiDPFlexibilityMatrixMpiTests.TestFlexibilityMatrices, typeof(FetiDPFlexibilityMatrixMpiTests).Name, "TestFlexibilityMatrices");

            suite.RunTests(args);
        }

        private readonly List<(Action test, string className, string methodName)> tests =
            new List<(Action test, string className, string methodName)>();

        public void AddFact(Action test, string className, string methodName) => tests.Add((test, className, methodName));

        public void AddTheory<TInput>(Action<TInput> test, string className, string methodName, TInput input)
            => tests.Add(( () => test(input), className, $"{methodName}(args = {input})" ));

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
                    catch (Exception ex)
                    {
                        Console.WriteLine(header + $"Test {t} - {className}.{methodName} failed! \n" + ex.StackTrace);
                    }
                }
                comm.Barrier();
                Console.WriteLine(header + "All tests were completed.");
            }
        }
    }
}
