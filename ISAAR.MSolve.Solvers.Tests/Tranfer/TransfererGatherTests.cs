using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Transfer;
using ISAAR.MSolve.LinearAlgebra.Tests.Utilities;
using MPI;
using Xunit;
using static ISAAR.MSolve.Solvers.Tests.Tranfer.TransfererTestsData;
using static ISAAR.MSolve.Solvers.Tests.Tranfer.TransfererTestUtilities;

namespace ISAAR.MSolve.Solvers.Tests.Tranfer
{
    public static class TransfererGatherTests
    {
        /// <summary>
        /// All tests need 4 MPI processes.
        /// </summary>
        /// <param name="suite"></param>
        public static void RegisterAllTests(MpiTestSuite suite)
        {
            // Tests for: TransfererPerSubdomain
            suite.AddTheory(TestGatherFromAllPrimitive, typeof(TransfererGatherTests).Name, "TestGatherFromAllPrimitive",
                TransfererChoice.PerSubdomain, SubdomainDistribution.OnePerProcess);
            suite.AddTheory(TestGatherFromAllPrimitive, typeof(TransfererGatherTests).Name, "TestGatherFromAllPrimitive",
                TransfererChoice.PerSubdomain, SubdomainDistribution.Uniform);
            suite.AddTheory(TestGatherFromAllPrimitive, typeof(TransfererGatherTests).Name, "TestGatherFromAllPrimitive",
                TransfererChoice.PerSubdomain, SubdomainDistribution.Variable);

            suite.AddTheory(TestGatherFromAllArray, typeof(TransfererGatherTests).Name, "TestGatherFromAllArray",
                TransfererChoice.PerSubdomain, SubdomainDistribution.OnePerProcess);
            suite.AddTheory(TestGatherFromAllArray, typeof(TransfererGatherTests).Name, "TestGatherFromAllArray",
                TransfererChoice.PerSubdomain, SubdomainDistribution.Uniform);
            suite.AddTheory(TestGatherFromAllArray, typeof(TransfererGatherTests).Name, "TestGatherFromAllArray",
                TransfererChoice.PerSubdomain, SubdomainDistribution.Variable);

            suite.AddTheory(TestGatherFromAllClass, typeof(TransfererGatherTests).Name, "TestGatherFromAllClass",
                TransfererChoice.PerSubdomain, SubdomainDistribution.OnePerProcess);
            suite.AddTheory(TestGatherFromAllClass, typeof(TransfererGatherTests).Name, "TestGatherFromAllClass",
                TransfererChoice.PerSubdomain, SubdomainDistribution.Uniform);
            suite.AddTheory(TestGatherFromAllClass, typeof(TransfererGatherTests).Name, "TestGatherFromAllClass",
                TransfererChoice.PerSubdomain, SubdomainDistribution.Variable);

            suite.AddTheory(TestGatherFromAllClassPacked, typeof(TransfererGatherTests).Name, "TestGatherFromAllClassPacked",
                TransfererChoice.PerSubdomain, SubdomainDistribution.OnePerProcess);
            suite.AddTheory(TestGatherFromAllClassPacked, typeof(TransfererGatherTests).Name, "TestGatherFromAllClassPacked",
                TransfererChoice.PerSubdomain, SubdomainDistribution.Uniform);
            suite.AddTheory(TestGatherFromAllClassPacked, typeof(TransfererGatherTests).Name, "TestGatherFromAllClassPacked",
                TransfererChoice.PerSubdomain, SubdomainDistribution.Variable);

            suite.AddTheory(TestGatherFromAllClassPackedArray, typeof(TransfererGatherTests).Name, "TestGatherFromAllClassPackedArray",
                TransfererChoice.PerSubdomain, SubdomainDistribution.OnePerProcess);
            suite.AddTheory(TestGatherFromAllClassPackedArray, typeof(TransfererGatherTests).Name, "TestGatherFromAllClassPackedArray",
                TransfererChoice.PerSubdomain, SubdomainDistribution.Uniform);
            suite.AddTheory(TestGatherFromAllClassPackedArray, typeof(TransfererGatherTests).Name, "TestGatherFromAllClassPackedArray",
                TransfererChoice.PerSubdomain, SubdomainDistribution.Variable);

            suite.AddTheory(TestGatherFromSomePrimitive, typeof(TransfererGatherTests).Name, "TestGatherFromSomePrimitive",
                TransfererChoice.PerSubdomain, SubdomainDistribution.OnePerProcess);
            suite.AddTheory(TestGatherFromSomePrimitive, typeof(TransfererGatherTests).Name, "TestGatherFromSomePrimitive",
                TransfererChoice.PerSubdomain, SubdomainDistribution.Uniform);
            suite.AddTheory(TestGatherFromSomePrimitive, typeof(TransfererGatherTests).Name, "TestGatherFromSomePrimitive",
                TransfererChoice.PerSubdomain, SubdomainDistribution.Variable);

            suite.AddTheory(TestGatherFromSomeArray, typeof(TransfererGatherTests).Name, "TestGatherFromSomeArray",
                TransfererChoice.PerSubdomain, SubdomainDistribution.OnePerProcess);
            suite.AddTheory(TestGatherFromSomeArray, typeof(TransfererGatherTests).Name, "TestGatherFromSomeArray",
                TransfererChoice.PerSubdomain, SubdomainDistribution.Uniform);
            suite.AddTheory(TestGatherFromSomeArray, typeof(TransfererGatherTests).Name, "TestGatherFromSomeArray",
                TransfererChoice.PerSubdomain, SubdomainDistribution.Variable);

            suite.AddTheory(TestGatherFromSomeClass, typeof(TransfererGatherTests).Name, "TestGatherFromSomeClass",
                TransfererChoice.PerSubdomain, SubdomainDistribution.OnePerProcess);
            suite.AddTheory(TestGatherFromSomeClass, typeof(TransfererGatherTests).Name, "TestGatherFromSomeClass",
                TransfererChoice.PerSubdomain, SubdomainDistribution.Uniform);
            suite.AddTheory(TestGatherFromSomeClass, typeof(TransfererGatherTests).Name, "TestGatherFromSomeClass",
                TransfererChoice.PerSubdomain, SubdomainDistribution.Variable);

            suite.AddTheory(TestGatherFromSomeClassPacked, typeof(TransfererGatherTests).Name, "TestGatherFromSomeClassPacked",
                TransfererChoice.PerSubdomain, SubdomainDistribution.OnePerProcess);
            suite.AddTheory(TestGatherFromSomeClassPacked, typeof(TransfererGatherTests).Name, "TestGatherFromSomeClassPacked",
                TransfererChoice.PerSubdomain, SubdomainDistribution.Uniform);
            suite.AddTheory(TestGatherFromSomeClassPacked, typeof(TransfererGatherTests).Name, "TestGatherFromSomeClassPacked",
                TransfererChoice.PerSubdomain, SubdomainDistribution.Variable);

            suite.AddTheory(TestGatherFromSomeClassPackedArray, typeof(TransfererGatherTests).Name, "TestGatherFromSomeClassPackedArray",
                TransfererChoice.PerSubdomain, SubdomainDistribution.OnePerProcess);
            suite.AddTheory(TestGatherFromSomeClassPackedArray, typeof(TransfererGatherTests).Name, "TestGatherFromSomeClassPackedArray",
                TransfererChoice.PerSubdomain, SubdomainDistribution.Uniform);
            suite.AddTheory(TestGatherFromSomeClassPackedArray, typeof(TransfererGatherTests).Name, "TestGatherFromSomeClassPackedArray",
                TransfererChoice.PerSubdomain, SubdomainDistribution.Variable);

            // Tests for: TransfererAltogetherFlattened
            suite.AddTheory(TestGatherFromAllPrimitive, typeof(TransfererGatherTests).Name, "TestGatherFromAllPrimitive",
                TransfererChoice.AltogetherFlattened, SubdomainDistribution.OnePerProcess);
            suite.AddTheory(TestGatherFromAllPrimitive, typeof(TransfererGatherTests).Name, "TestGatherFromAllPrimitive",
                TransfererChoice.AltogetherFlattened, SubdomainDistribution.Uniform);
            suite.AddTheory(TestGatherFromAllPrimitive, typeof(TransfererGatherTests).Name, "TestGatherFromAllPrimitive",
                TransfererChoice.AltogetherFlattened, SubdomainDistribution.Variable);

            suite.AddTheory(TestGatherFromAllArray, typeof(TransfererGatherTests).Name, "TestGatherFromAllArray",
                TransfererChoice.AltogetherFlattened, SubdomainDistribution.OnePerProcess);
            suite.AddTheory(TestGatherFromAllArray, typeof(TransfererGatherTests).Name, "TestGatherFromAllArray",
                TransfererChoice.AltogetherFlattened, SubdomainDistribution.Uniform);
            suite.AddTheory(TestGatherFromAllArray, typeof(TransfererGatherTests).Name, "TestGatherFromAllArray",
                TransfererChoice.AltogetherFlattened, SubdomainDistribution.Variable);

            suite.AddTheory(TestGatherFromAllClass, typeof(TransfererGatherTests).Name, "TestGatherFromAllClass",
                TransfererChoice.AltogetherFlattened, SubdomainDistribution.OnePerProcess);
            suite.AddTheory(TestGatherFromAllClass, typeof(TransfererGatherTests).Name, "TestGatherFromAllClass",
                TransfererChoice.AltogetherFlattened, SubdomainDistribution.Uniform);
            suite.AddTheory(TestGatherFromAllClass, typeof(TransfererGatherTests).Name, "TestGatherFromAllClass",
                TransfererChoice.AltogetherFlattened, SubdomainDistribution.Variable);

            suite.AddTheory(TestGatherFromAllClassPacked, typeof(TransfererGatherTests).Name, "TestGatherFromAllClassPacked",
                TransfererChoice.AltogetherFlattened, SubdomainDistribution.OnePerProcess);
            suite.AddTheory(TestGatherFromAllClassPacked, typeof(TransfererGatherTests).Name, "TestGatherFromAllClassPacked",
                TransfererChoice.AltogetherFlattened, SubdomainDistribution.Uniform);
            suite.AddTheory(TestGatherFromAllClassPacked, typeof(TransfererGatherTests).Name, "TestGatherFromAllClassPacked",
                TransfererChoice.AltogetherFlattened, SubdomainDistribution.Variable);

            suite.AddTheory(TestGatherFromAllClassPackedArray, typeof(TransfererGatherTests).Name, "TestGatherFromAllClassPackedArray",
                TransfererChoice.AltogetherFlattened, SubdomainDistribution.OnePerProcess);
            suite.AddTheory(TestGatherFromAllClassPackedArray, typeof(TransfererGatherTests).Name, "TestGatherFromAllClassPackedArray",
                TransfererChoice.AltogetherFlattened, SubdomainDistribution.Uniform);
            suite.AddTheory(TestGatherFromAllClassPackedArray, typeof(TransfererGatherTests).Name, "TestGatherFromAllClassPackedArray",
                TransfererChoice.AltogetherFlattened, SubdomainDistribution.Variable);
        }

        public static void TestGatherFromAllArray(TransfererChoice transfererChoice,
            SubdomainDistribution subdomainDistribution)
        {
            TestGatherTemplate(transfererChoice, subdomainDistribution, true,
                s => GetArrayDataOfSubdomain(s),
                (transf, allData, activeSubdomains) => transf.GatherFromAllSubdomains(allData),
                (s, computed) => Assert.True(CheckEquality(GetArrayDataOfSubdomain(s), computed)));
        }

        public static void TestGatherFromAllClass(TransfererChoice transfererChoice,
            SubdomainDistribution subdomainDistribution)
        {
            TestGatherTemplate(transfererChoice, subdomainDistribution, true,
                s => GetClassDataOfSubdomain(s),
                (transf, allData, activeSubdomains) => transf.GatherFromAllSubdomains(allData),
                (s, computed) => Assert.True(GetClassDataOfSubdomain(s).Equals(computed)));
        }

        public static void TestGatherFromAllClassPacked(TransfererChoice transfererChoice,
            SubdomainDistribution subdomainDistribution)
        {
            TestGatherTemplate(transfererChoice, subdomainDistribution, true,
                s => GetClassDataOfSubdomain(s),
                (transf, allData, activeSubdomains) =>
                {
                    PackSubdomainData<SampleClass, SampleDto> packData = (id, data) => new SampleDto(data);
                    UnpackSubdomainData<SampleClass, SampleDto> unpackData = (id, dto) => dto.Unpack();
                    return transf.GatherFromAllSubdomainsPacked(allData, packData, unpackData);
                },
                (s, computed) => Assert.True(GetClassDataOfSubdomain(s).Equals(computed)));
        }

        public static void TestGatherFromAllClassPackedArray(TransfererChoice transfererChoice,
            SubdomainDistribution subdomainDistribution)
        {
            TestGatherTemplate(transfererChoice, subdomainDistribution, true,
                s => GetClassDataOfSubdomain(s),
                (transf, allData, activeSubdomains) =>
                {
                    GetArrayLengthOfPackedData<SampleClass> getPackedDataLength = (id, data) => data.PackedArrayLength;
                    PackSubdomainDataIntoArray<SampleClass, int> packData = 
                        (id, data, packingArray, offset) => data.PackIntoArray(packingArray, offset);
                    UnpackSubdomainDataFromArray<SampleClass, int> unpackData = 
                        (id, packingArray, offset) => SampleClass.UnpackFromArray(id, packingArray, offset);
                    return transf.GatherFromAllSubdomainsPacked(allData, getPackedDataLength, packData, unpackData);
                },
                (s, computed) => Assert.True(GetClassDataOfSubdomain(s).Equals(computed)));
        }

        public static void TestGatherFromAllPrimitive(TransfererChoice transfererChoice, 
            SubdomainDistribution subdomainDistribution)
        {
            TestGatherTemplate(transfererChoice, subdomainDistribution, true,
                s => GetPrimitiveDataOfSubdomain(s),
                (transf, allData, activeSubdomains) => transf.GatherFromAllSubdomains(allData),
                (s, computed) => Assert.Equal(GetPrimitiveDataOfSubdomain(s), computed));
        }

        public static void TestGatherFromSomeArray(TransfererChoice transfererChoice,
            SubdomainDistribution subdomainDistribution)
        {
            TestGatherTemplate(transfererChoice, subdomainDistribution, false,
                s => GetArrayDataOfSubdomain(s),
                (transf, allData, activeSubdomains) => transf.GatherFromSomeSubdomains(allData, activeSubdomains),
                (s, computed) => Assert.True(CheckEquality(GetArrayDataOfSubdomain(s), computed)));
        }

        public static void TestGatherFromSomeClass(TransfererChoice transfererChoice,
            SubdomainDistribution subdomainDistribution)
        {
            TestGatherTemplate(transfererChoice, subdomainDistribution, false,
                s => GetClassDataOfSubdomain(s),
                (transf, allData, activeSubdomains) => transf.GatherFromSomeSubdomains(allData, activeSubdomains),
                (s, computed) => Assert.True(GetClassDataOfSubdomain(s).Equals(computed)));
        }

        public static void TestGatherFromSomeClassPacked(TransfererChoice transfererChoice,
            SubdomainDistribution subdomainDistribution)
        {
            TestGatherTemplate(transfererChoice, subdomainDistribution, false,
                s => GetClassDataOfSubdomain(s),
                (transf, allData, activeSubdomains) =>
                {
                    PackSubdomainData<SampleClass, SampleDto> packData = (id, data) => new SampleDto(data);
                    UnpackSubdomainData<SampleClass, SampleDto> unpackData = (id, dto) => dto.Unpack();
                    return transf.GatherFromSomeSubdomainsPacked(allData, packData, unpackData, activeSubdomains);
                },
                (s, computed) => Assert.True(GetClassDataOfSubdomain(s).Equals(computed)));
        }

        public static void TestGatherFromSomeClassPackedArray(TransfererChoice transfererChoice,
            SubdomainDistribution subdomainDistribution)
        {
            TestGatherTemplate(transfererChoice, subdomainDistribution, false,
                s => GetClassDataOfSubdomain(s),
                (transf, allData, activeSubdomains) =>
                {
                    GetArrayLengthOfPackedData<SampleClass> getPackedDataLength = (id, data) => data.PackedArrayLength;
                    PackSubdomainDataIntoArray<SampleClass, int> packData =
                        (id, data, packingArray, offset) => data.PackIntoArray(packingArray, offset);
                    UnpackSubdomainDataFromArray<SampleClass, int> unpackData =
                        (id, packingArray, offset) => SampleClass.UnpackFromArray(id, packingArray, offset);
                    return transf.GatherFromSomeSubdomainsPacked(allData, getPackedDataLength, packData, unpackData, 
                        activeSubdomains);
                },
                (s, computed) => Assert.True(GetClassDataOfSubdomain(s).Equals(computed)));
        }

        public static void TestGatherFromSomePrimitive(TransfererChoice transfererChoice,
            SubdomainDistribution subdomainDistribution)
        {
            TestGatherTemplate(transfererChoice, subdomainDistribution, false,
                s => GetPrimitiveDataOfSubdomain(s),
                (transf, allData, activeSubdomains) => transf.GatherFromSomeSubdomains(allData, activeSubdomains),
                (s, computed) => Assert.Equal(GetPrimitiveDataOfSubdomain(s), computed));
        }

        private static void TestGatherTemplate<T>(TransfererChoice transfererChoice,
            SubdomainDistribution subdomainDistribution, bool gatherAll, Func<int, T> createSubdomainData,
            Func<ISubdomainDataTransferer, Dictionary<int, T>, ActiveSubdomains, Dictionary<int, T>> gatherSubdomainData,
            Action<int, T> checkReceivedData)
        {
            ProcessDistribution procs = DetermineProcesses(subdomainDistribution);
            ISubdomainDataTransferer transferer = DetermineTransferer(transfererChoice, procs);
            ActiveSubdomains activeSubdomains = DetermineActiveSubdomains(procs);

            // Prepare data in each process
            Dictionary<int, T> processData = new Dictionary<int, T>();
            foreach (int s in procs.GetSubdomainIdsOfProcess(procs.OwnRank))
            {
                if (gatherAll || activeSubdomains.IsActive(s)) processData[s] = createSubdomainData(s);
            }

            // Gather them in master
            Dictionary<int, T> allData_master = gatherSubdomainData(transferer, processData, activeSubdomains);

            // Check the received data in master
            if (procs.IsMasterProcess)
            {
                for (int p = 0; p < procs.Communicator.Size; ++p)
                {
                    foreach (int s in procs.GetSubdomainIdsOfProcess(p))
                    {
                        if (gatherAll || activeSubdomains.IsActive(s))
                        {
                            checkReceivedData(s, allData_master[s]);
                        }
                    }
                }
            }
        }
    }
}
