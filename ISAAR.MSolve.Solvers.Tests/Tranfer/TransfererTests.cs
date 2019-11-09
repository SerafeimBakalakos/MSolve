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
            suite.AddTheory(TestScatterToAllPrimitive, typeof(TransfererTests).Name, "TestScatterToAllPrimitive",
                TransfererChoice.PerSubdomain, SubdomainDistribution.OnePerProcess);
            suite.AddTheory(TestScatterToAllPrimitive, typeof(TransfererTests).Name, "TestScatterToAllPrimitive",
                TransfererChoice.PerSubdomain, SubdomainDistribution.Uniform);
            suite.AddTheory(TestScatterToAllPrimitive, typeof(TransfererTests).Name, "TestScatterToAllPrimitive",
                TransfererChoice.PerSubdomain, SubdomainDistribution.Variable);

            suite.AddTheory(TestScatterToAllArray, typeof(TransfererTests).Name, "TestScatterToAllArray",
                TransfererChoice.PerSubdomain, SubdomainDistribution.OnePerProcess);
            suite.AddTheory(TestScatterToAllArray, typeof(TransfererTests).Name, "TestScatterToAllArray",
                TransfererChoice.PerSubdomain, SubdomainDistribution.Uniform);
            suite.AddTheory(TestScatterToAllArray, typeof(TransfererTests).Name, "TestScatterToAllArray",
                TransfererChoice.PerSubdomain, SubdomainDistribution.Variable);

            suite.AddTheory(TestScatterToAllClass, typeof(TransfererTests).Name, "TestScatterToAllClass",
                TransfererChoice.PerSubdomain, SubdomainDistribution.OnePerProcess);
            suite.AddTheory(TestScatterToAllClass, typeof(TransfererTests).Name, "TestScatterToAllClass",
                TransfererChoice.PerSubdomain, SubdomainDistribution.Uniform);
            suite.AddTheory(TestScatterToAllClass, typeof(TransfererTests).Name, "TestScatterToAllClass",
                TransfererChoice.PerSubdomain, SubdomainDistribution.Variable);

            suite.AddTheory(TestScatterToAllClassPacked, typeof(TransfererTests).Name, "TestScatterToAllClassPacked",
                TransfererChoice.PerSubdomain, SubdomainDistribution.OnePerProcess);
            suite.AddTheory(TestScatterToAllClassPacked, typeof(TransfererTests).Name, "TestScatterToAllClassPacked",
                TransfererChoice.PerSubdomain, SubdomainDistribution.Uniform);
            suite.AddTheory(TestScatterToAllClassPacked, typeof(TransfererTests).Name, "TestScatterToAllClassPacked",
                TransfererChoice.PerSubdomain, SubdomainDistribution.Variable);


            suite.AddTheory(TestScatterToSomePrimitive, typeof(TransfererTests).Name, "TestScatterToSomePrimitive",
                TransfererChoice.PerSubdomain, SubdomainDistribution.OnePerProcess);
            suite.AddTheory(TestScatterToSomePrimitive, typeof(TransfererTests).Name, "TestScatterToSomePrimitive",
                TransfererChoice.PerSubdomain, SubdomainDistribution.Uniform);
            suite.AddTheory(TestScatterToSomePrimitive, typeof(TransfererTests).Name, "TestScatterToSomePrimitive",
                TransfererChoice.PerSubdomain, SubdomainDistribution.Variable);

            suite.AddTheory(TestScatterToSomeArray, typeof(TransfererTests).Name, "TestScatterToSomeArray",
                TransfererChoice.PerSubdomain, SubdomainDistribution.OnePerProcess);
            suite.AddTheory(TestScatterToSomeArray, typeof(TransfererTests).Name, "TestScatterToSomeArray",
                TransfererChoice.PerSubdomain, SubdomainDistribution.Uniform);
            suite.AddTheory(TestScatterToSomeArray, typeof(TransfererTests).Name, "TestScatterToSomeArray",
                TransfererChoice.PerSubdomain, SubdomainDistribution.Variable);

            suite.AddTheory(TestScatterToSomeClass, typeof(TransfererTests).Name, "TestScatterToSomeClass",
                TransfererChoice.PerSubdomain, SubdomainDistribution.OnePerProcess);
            suite.AddTheory(TestScatterToSomeClass, typeof(TransfererTests).Name, "TestScatterToSomeClass",
                TransfererChoice.PerSubdomain, SubdomainDistribution.Uniform);
            suite.AddTheory(TestScatterToSomeClass, typeof(TransfererTests).Name, "TestScatterToSomeClass",
                TransfererChoice.PerSubdomain, SubdomainDistribution.Variable);

            suite.AddTheory(TestScatterToSomeClassPacked, typeof(TransfererTests).Name, "TestScatterToSomeClassPacked",
                TransfererChoice.PerSubdomain, SubdomainDistribution.OnePerProcess);
            suite.AddTheory(TestScatterToSomeClassPacked, typeof(TransfererTests).Name, "TestScatterToSomeClassPacked",
                TransfererChoice.PerSubdomain, SubdomainDistribution.Uniform);
            suite.AddTheory(TestScatterToSomeClassPacked, typeof(TransfererTests).Name, "TestScatterToSomeClassPacked",
                TransfererChoice.PerSubdomain, SubdomainDistribution.Variable);

            // Tests for: TransfererAltogetherFlattened
            suite.AddTheory(TestScatterToAllPrimitive, typeof(TransfererTests).Name, "TestScatterToAllPrimitive",
                TransfererChoice.AltogetherFlattened, SubdomainDistribution.OnePerProcess);
            suite.AddTheory(TestScatterToAllPrimitive, typeof(TransfererTests).Name, "TestScatterToAllPrimitive",
                TransfererChoice.AltogetherFlattened, SubdomainDistribution.Uniform);
            suite.AddTheory(TestScatterToAllPrimitive, typeof(TransfererTests).Name, "TestScatterToAllPrimitive",
                TransfererChoice.AltogetherFlattened, SubdomainDistribution.Variable);

            suite.AddTheory(TestScatterToAllArray, typeof(TransfererTests).Name, "TestScatterToAllArray",
                TransfererChoice.AltogetherFlattened, SubdomainDistribution.OnePerProcess);
            suite.AddTheory(TestScatterToAllArray, typeof(TransfererTests).Name, "TestScatterToAllArray",
                TransfererChoice.AltogetherFlattened, SubdomainDistribution.Uniform);
            suite.AddTheory(TestScatterToAllArray, typeof(TransfererTests).Name, "TestScatterToAllArray",
                TransfererChoice.AltogetherFlattened, SubdomainDistribution.Variable);

            suite.AddTheory(TestScatterToAllClass, typeof(TransfererTests).Name, "TestScatterToAllClass",
                TransfererChoice.AltogetherFlattened, SubdomainDistribution.OnePerProcess);
            suite.AddTheory(TestScatterToAllClass, typeof(TransfererTests).Name, "TestScatterToAllClass",
                TransfererChoice.AltogetherFlattened, SubdomainDistribution.Uniform);
            suite.AddTheory(TestScatterToAllClass, typeof(TransfererTests).Name, "TestScatterToAllClass",
                TransfererChoice.AltogetherFlattened, SubdomainDistribution.Variable);

            suite.AddTheory(TestScatterToAllClassPacked, typeof(TransfererTests).Name, "TestScatterToAllClassPacked",
                TransfererChoice.AltogetherFlattened, SubdomainDistribution.OnePerProcess);
            suite.AddTheory(TestScatterToAllClassPacked, typeof(TransfererTests).Name, "TestScatterToAllClassPacked",
                TransfererChoice.AltogetherFlattened, SubdomainDistribution.Uniform);
            suite.AddTheory(TestScatterToAllClassPacked, typeof(TransfererTests).Name, "TestScatterToAllClassPacked",
                TransfererChoice.AltogetherFlattened, SubdomainDistribution.Variable);
        }

        public static void TestScatterToAllArray(TransfererChoice transfererChoice,
            SubdomainDistribution subdomainDistribution)
        {
            TestScatterTemplate(transfererChoice, subdomainDistribution, true,
                s => GetArrayDataOfSubdomain(s),
                (transf, allData, activeSubdomains) => transf.ScatterToAllSubdomains(allData),
                (s, computed) => Assert.True(CheckEquality(GetArrayDataOfSubdomain(s), computed)));
        }

        public static void TestScatterToAllClass(TransfererChoice transfererChoice,
            SubdomainDistribution subdomainDistribution)
        {
            TestScatterTemplate(transfererChoice, subdomainDistribution, true,
                s => GetClassDataOfSubdomain(s),
                (transf, allData, activeSubdomains) => transf.ScatterToAllSubdomains(allData),
                (s, computed) => Assert.True(GetClassDataOfSubdomain(s).Equals(computed)));
        }

        public static void TestScatterToAllClassPacked(TransfererChoice transfererChoice,
            SubdomainDistribution subdomainDistribution)
        {
            TestScatterTemplate(transfererChoice, subdomainDistribution, true,
                s => GetClassDataOfSubdomain(s),
                (transf, allData, activeSubdomains) =>
                {
                    PackSubdomainData<SampleClass, SampleDto> packData = (id, data) => new SampleDto(data);
                    UnpackSubdomainData<SampleClass, SampleDto> unpackData = (id, dto) => dto.Unpack();
                    return transf.ScatterToAllSubdomainsPacked(allData, packData, unpackData);
                },
                (s, computed) => Assert.True(GetClassDataOfSubdomain(s).Equals(computed)));
        }

        public static void TestScatterToAllPrimitive(TransfererChoice transfererChoice, 
            SubdomainDistribution subdomainDistribution)
        {
            TestScatterTemplate(transfererChoice, subdomainDistribution, true,
                s => GetPrimitiveDataOfSubdomain(s),
                (transf, allData, activeSubdomains) => transf.ScatterToAllSubdomains(allData),
                (s, computed) => Assert.Equal(GetPrimitiveDataOfSubdomain(s), computed));
        }

        public static void TestScatterToSomeArray(TransfererChoice transfererChoice,
            SubdomainDistribution subdomainDistribution)
        {
            TestScatterTemplate(transfererChoice, subdomainDistribution, false,
                s => GetArrayDataOfSubdomain(s),
                (transf, allData, activeSubdomains) => transf.ScatterToSomeSubdomains(allData, activeSubdomains),
                (s, computed) => Assert.True(CheckEquality(GetArrayDataOfSubdomain(s), computed)));
        }

        public static void TestScatterToSomeClass(TransfererChoice transfererChoice,
            SubdomainDistribution subdomainDistribution)
        {
            TestScatterTemplate(transfererChoice, subdomainDistribution, false,
                s => GetClassDataOfSubdomain(s),
                (transf, allData, activeSubdomains) => transf.ScatterToSomeSubdomains(allData, activeSubdomains),
                (s, computed) => Assert.True(GetClassDataOfSubdomain(s).Equals(computed)));
        }

        public static void TestScatterToSomeClassPacked(TransfererChoice transfererChoice,
            SubdomainDistribution subdomainDistribution)
        {
            TestScatterTemplate(transfererChoice, subdomainDistribution, false,
                s => GetClassDataOfSubdomain(s),
                (transf, allData, activeSubdomains) =>
                {
                    PackSubdomainData<SampleClass, SampleDto> packData = (id, data) => new SampleDto(data);
                    UnpackSubdomainData<SampleClass, SampleDto> unpackData = (id, dto) => dto.Unpack();
                    return transf.ScatterToSomeSubdomainsPacked(allData, packData, unpackData, activeSubdomains);
                },
                (s, computed) => Assert.True(GetClassDataOfSubdomain(s).Equals(computed)));
        }

        public static void TestScatterToSomePrimitive(TransfererChoice transfererChoice,
            SubdomainDistribution subdomainDistribution)
        {
            TestScatterTemplate(transfererChoice, subdomainDistribution, false,
                s => GetPrimitiveDataOfSubdomain(s),
                (transf, allData, activeSubdomains) => transf.ScatterToSomeSubdomains(allData, activeSubdomains),
                (s, computed) => Assert.Equal(GetPrimitiveDataOfSubdomain(s), computed));
        }

        private static ActiveSubdomains DetermineActiveSubdomains(ProcessDistribution procs)
        {
            // Every third subdomain is active
            return new ActiveSubdomains(procs, s => (s + 1) % 3 == 0);
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

        private static void TestScatterTemplate<T>(TransfererChoice transfererChoice,
            SubdomainDistribution subdomainDistribution, bool scatterAll, Func<int, T> createSubdomainData,
            Func<ISubdomainDataTransferer, Dictionary<int, T>, ActiveSubdomains, Dictionary<int, T>> scatterSubdomainData,
            Action<int, T> checkReceivedData)
        {
            ProcessDistribution procs = DetermineProcesses(subdomainDistribution);
            ISubdomainDataTransferer transferer = DetermineTransferer(transfererChoice, procs);
            ActiveSubdomains activeSubdomains = DetermineActiveSubdomains(procs);

            // Prepare data in master
            Dictionary<int, T> allData_master = null;
            if (procs.IsMasterProcess)
            {
                allData_master = new Dictionary<int, T>();
                for (int p = 0; p < procs.Communicator.Size; ++p)
                {
                    foreach (int s in procs.GetSubdomainIdsOfProcess(p))
                    {
                        if (scatterAll || activeSubdomains.IsActive(s)) allData_master[s] = createSubdomainData(s);
                    }
                }
            }

            // Scatter them to other processes
            Dictionary<int, T> processData = scatterSubdomainData(transferer, allData_master, activeSubdomains);

            // Check the received data in each process other than master
            if (!procs.IsMasterProcess)
            {
                foreach (int s in procs.GetSubdomainIdsOfProcess(procs.OwnRank))
                {
                    if (scatterAll || activeSubdomains.IsActive(s))
                    {
                        checkReceivedData(s, processData[s]);
                    }
                }
            }
        }
    }
}
