using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Transfer;
using ISAAR.MSolve.LinearAlgebra.Tests.Utilities;
using MPI;
using Xunit;
using static ISAAR.MSolve.Solvers.Tests.Tranfer.TransfererTestsData;

namespace ISAAR.MSolve.Solvers.Tests.Tranfer
{
    public static class TransfererTests
    {
        private const int numProcesses = 4;

        public enum SubdomainDistribution
        {
            OnePerProcess, Uniform, Variable
        }

        public enum TransfererChoice
        {
            PerSubdomain, AltogetherFlattened
        }

        /// <summary>
        /// All tests need 4 MPI processes.
        /// </summary>
        /// <param name="suite"></param>
        public static void RegisterAllTests(MpiTestSuite suite)
        {
            // Tests for: TransfererPerSubdomain
            suite.AddTheory(TestScatterAllPrimitive, typeof(TransfererTests).Name, "TestScatterAllPrimitive", 
                TransfererChoice.PerSubdomain, SubdomainDistribution.OnePerProcess);
            suite.AddTheory(TestScatterAllPrimitive, typeof(TransfererTests).Name, "TestScatterAllPrimitive",
                TransfererChoice.PerSubdomain, SubdomainDistribution.Uniform);
            suite.AddTheory(TestScatterAllPrimitive, typeof(TransfererTests).Name, "TestScatterAllPrimitive",
                TransfererChoice.PerSubdomain, SubdomainDistribution.Variable);

            suite.AddTheory(TestScatterAllArray, typeof(TransfererTests).Name, "TestScatterAllArray",
                TransfererChoice.PerSubdomain, SubdomainDistribution.OnePerProcess);
            suite.AddTheory(TestScatterAllArray, typeof(TransfererTests).Name, "TestScatterAllArray",
                TransfererChoice.PerSubdomain, SubdomainDistribution.Uniform);
            suite.AddTheory(TestScatterAllArray, typeof(TransfererTests).Name, "TestScatterAllArray",
                TransfererChoice.PerSubdomain, SubdomainDistribution.Variable);

            suite.AddTheory(TestScatterAllClass, typeof(TransfererTests).Name, "TestScatterAllClass",
                TransfererChoice.PerSubdomain, SubdomainDistribution.OnePerProcess);
            suite.AddTheory(TestScatterAllClass, typeof(TransfererTests).Name, "TestScatterAllClass",
                TransfererChoice.PerSubdomain, SubdomainDistribution.Uniform);
            suite.AddTheory(TestScatterAllClass, typeof(TransfererTests).Name, "TestScatterAllClass",
                TransfererChoice.PerSubdomain, SubdomainDistribution.Variable);

            suite.AddTheory(TestScatterAllClassPacked, typeof(TransfererTests).Name, "TestScatterAllClassPacked",
                TransfererChoice.PerSubdomain, SubdomainDistribution.OnePerProcess);
            suite.AddTheory(TestScatterAllClassPacked, typeof(TransfererTests).Name, "TestScatterAllClassPacked",
                TransfererChoice.PerSubdomain, SubdomainDistribution.Uniform);
            suite.AddTheory(TestScatterAllClassPacked, typeof(TransfererTests).Name, "TestScatterAllClassPacked",
                TransfererChoice.PerSubdomain, SubdomainDistribution.Variable);

            // Tests for: TransfererAltogetherFlattened
            suite.AddTheory(TestScatterAllPrimitive, typeof(TransfererTests).Name, "TestScatterAllPrimitive",
                TransfererChoice.AltogetherFlattened, SubdomainDistribution.OnePerProcess);
            suite.AddTheory(TestScatterAllPrimitive, typeof(TransfererTests).Name, "TestScatterAllPrimitive",
                TransfererChoice.AltogetherFlattened, SubdomainDistribution.Uniform);
            suite.AddTheory(TestScatterAllPrimitive, typeof(TransfererTests).Name, "TestScatterAllPrimitive",
                TransfererChoice.AltogetherFlattened, SubdomainDistribution.Variable);

            suite.AddTheory(TestScatterAllArray, typeof(TransfererTests).Name, "TestScatterAllArray",
                TransfererChoice.AltogetherFlattened, SubdomainDistribution.OnePerProcess);
            suite.AddTheory(TestScatterAllArray, typeof(TransfererTests).Name, "TestScatterAllArray",
                TransfererChoice.AltogetherFlattened, SubdomainDistribution.Uniform);
            suite.AddTheory(TestScatterAllArray, typeof(TransfererTests).Name, "TestScatterAllArray",
                TransfererChoice.AltogetherFlattened, SubdomainDistribution.Variable);

            suite.AddTheory(TestScatterAllClass, typeof(TransfererTests).Name, "TestScatterAllClass",
                TransfererChoice.AltogetherFlattened, SubdomainDistribution.OnePerProcess);
            suite.AddTheory(TestScatterAllClass, typeof(TransfererTests).Name, "TestScatterAllClass",
                TransfererChoice.AltogetherFlattened, SubdomainDistribution.Uniform);
            suite.AddTheory(TestScatterAllClass, typeof(TransfererTests).Name, "TestScatterAllClass",
                TransfererChoice.AltogetherFlattened, SubdomainDistribution.Variable);

            suite.AddTheory(TestScatterAllClassPacked, typeof(TransfererTests).Name, "TestScatterAllClassPacked",
                TransfererChoice.AltogetherFlattened, SubdomainDistribution.OnePerProcess);
            suite.AddTheory(TestScatterAllClassPacked, typeof(TransfererTests).Name, "TestScatterAllClassPacked",
                TransfererChoice.AltogetherFlattened, SubdomainDistribution.Uniform);
            suite.AddTheory(TestScatterAllClassPacked, typeof(TransfererTests).Name, "TestScatterAllClassPacked",
                TransfererChoice.AltogetherFlattened, SubdomainDistribution.Variable);
        }

        public static void TestScatterAllArray(TransfererChoice transfererChoice,
            SubdomainDistribution subdomainDistribution)
        {
            ProcessDistribution procs = DetermineProcesses(subdomainDistribution);
            ISubdomainDataTransferer transferer = DetermineTransferer(transfererChoice, procs);

            // Prepare data in master
            Dictionary<int, double[]> allData_master = null;
            if (procs.IsMasterProcess)
            {
                allData_master = new Dictionary<int, double[]>();
                for (int p = 0; p < procs.Communicator.Size; ++p)
                {
                    foreach (int s in procs.GetSubdomainIdsOfProcess(p))
                    {
                        allData_master[s] = GetArrayDataOfSubdomain(s);
                    }
                }
            }

            // Scatter them to other processes
            Dictionary<int, double[]> processData = transferer.ScatterToAllSubdomains(allData_master);

            // Check the received data in each process other than master
            if (!procs.IsMasterProcess)
            {
                foreach (int s in procs.GetSubdomainIdsOfProcess(procs.OwnRank))
                {
                    double[] dataExpected = GetArrayDataOfSubdomain(s);
                    Assert.True(CheckEquality(dataExpected, processData[s]));
                }
            }
        }

        public static void TestScatterAllClass(TransfererChoice transfererChoice,
            SubdomainDistribution subdomainDistribution)
        {
            ProcessDistribution procs = DetermineProcesses(subdomainDistribution);
            ISubdomainDataTransferer transferer = DetermineTransferer(transfererChoice, procs);

            // Prepare data in master
            Dictionary<int, SampleClass> allData_master = null;
            if (procs.IsMasterProcess)
            {
                allData_master = new Dictionary<int, SampleClass>();
                for (int p = 0; p < procs.Communicator.Size; ++p)
                {
                    foreach (int s in procs.GetSubdomainIdsOfProcess(p))
                    {
                        allData_master[s] = GetClassDataOfSubdomain(s);
                    }
                }
            }

            // Scatter them to other processes
            Dictionary<int, SampleClass> processData = transferer.ScatterToAllSubdomains(allData_master);

            // Check the received data in each process other than master
            if (!procs.IsMasterProcess)
            {
                foreach (int s in procs.GetSubdomainIdsOfProcess(procs.OwnRank))
                {
                    SampleClass dataExpected = GetClassDataOfSubdomain(s);
                    Assert.True(dataExpected.Equals(processData[s]));
                }
            }
        }

        public static void TestScatterAllClassPacked(TransfererChoice transfererChoice,
            SubdomainDistribution subdomainDistribution)
        {
            ProcessDistribution procs = DetermineProcesses(subdomainDistribution);
            ISubdomainDataTransferer transferer = DetermineTransferer(transfererChoice, procs);

            // Prepare data in master
            Dictionary<int, SampleClass> allData_master = null;
            if (procs.IsMasterProcess)
            {
                allData_master = new Dictionary<int, SampleClass>();
                for (int p = 0; p < procs.Communicator.Size; ++p)
                {
                    foreach (int s in procs.GetSubdomainIdsOfProcess(p))
                    {
                        allData_master[s] = GetClassDataOfSubdomain(s);
                    }
                }
            }

            // Scatter them to other processes
            PackSubdomainData<SampleClass, SampleDto> packData = (id, data) => new SampleDto(data);
            UnpackSubdomainData<SampleClass, SampleDto> unpackData = (id, dto) => dto.Unpack();
            Dictionary<int, SampleClass> processData = transferer.ScatterToAllSubdomains(allData_master);

            // Check the received data in each process other than master
            if (!procs.IsMasterProcess)
            {
                foreach (int s in procs.GetSubdomainIdsOfProcess(procs.OwnRank))
                {
                    SampleClass dataExpected = GetClassDataOfSubdomain(s);
                    Assert.True(dataExpected.Equals(processData[s]));
                }
            }
        }

        public static void TestScatterAllPrimitive(TransfererChoice transfererChoice, 
            SubdomainDistribution subdomainDistribution)
        {
            ProcessDistribution procs = DetermineProcesses(subdomainDistribution);
            ISubdomainDataTransferer transferer = DetermineTransferer(transfererChoice, procs);

            // Prepare data in master
            Dictionary<int, long> allData_master = null;
            if (procs.IsMasterProcess)
            {
                allData_master = new Dictionary<int, long>();
                for (int p = 0; p < procs.Communicator.Size; ++p)
                {
                    foreach (int s in procs.GetSubdomainIdsOfProcess(p))
                    {
                        allData_master[s] = GetPrimitiveDataOfSubdomain(s);
                    }
                }
            }

            // Scatter them to other processes
            Dictionary<int, long> processData = transferer.ScatterToAllSubdomains(allData_master);

            // Check the received data in each process other than master
            if (!procs.IsMasterProcess)
            {
                foreach (int s in procs.GetSubdomainIdsOfProcess(procs.OwnRank))
                {
                    long dataExpected = GetPrimitiveDataOfSubdomain(s);
                    Assert.Equal(dataExpected, processData[s]);
                }
            }
        }

        private static ProcessDistribution DetermineProcesses(SubdomainDistribution subdomainDistribution)
        {
            int master = 0;
            var processesToSubdomains = new int[numProcesses][];
            int numSubdomains = 0;
            for (int p = 0; p < numProcesses; ++p)
            {
                int numSubdomainsOfThisProcess;
                if (subdomainDistribution == SubdomainDistribution.OnePerProcess) numSubdomainsOfThisProcess = 1;
                else if (subdomainDistribution == SubdomainDistribution.Uniform) numSubdomainsOfThisProcess = 5;
                else if (subdomainDistribution == SubdomainDistribution.Variable) numSubdomainsOfThisProcess = p + 1;
                else throw new NotImplementedException();
                processesToSubdomains[p] = new int[numSubdomainsOfThisProcess];
                {
                    for (int i = 0; i < numSubdomainsOfThisProcess; ++i)
                    {
                        processesToSubdomains[p][i] = numSubdomains++;
                    }
                }
            }
            return new ProcessDistribution(Communicator.world, master, processesToSubdomains);
        }

        private static ISubdomainDataTransferer DetermineTransferer(TransfererChoice transfererChoice, ProcessDistribution procs)
        {
            if (transfererChoice == TransfererChoice.PerSubdomain) return new TransfererPerSubdomain(procs);
            else if (transfererChoice == TransfererChoice.AltogetherFlattened) return new TransfererAltogetherFlattened(procs);
            else throw new NotImplementedException();
        }
    }
}
