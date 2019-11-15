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
    public static class TransfererScatterTests
    {
        /// <summary>
        /// All tests need 4 MPI processes.
        /// </summary>
        /// <param name="suite"></param>
        public static void RegisterAllTests(MpiTestSuite suite)
        {
            // Tests for: TransfererPerSubdomain
            suite.AddTheory(TestScatterToAllPrimitive, typeof(TransfererScatterTests).Name, "TestScatterToAllPrimitive",
                TransfererChoice.PerSubdomain, SubdomainDistribution.OnePerProcess);
            suite.AddTheory(TestScatterToAllPrimitive, typeof(TransfererScatterTests).Name, "TestScatterToAllPrimitive",
                TransfererChoice.PerSubdomain, SubdomainDistribution.Uniform);
            suite.AddTheory(TestScatterToAllPrimitive, typeof(TransfererScatterTests).Name, "TestScatterToAllPrimitive",
                TransfererChoice.PerSubdomain, SubdomainDistribution.Variable);

            suite.AddTheory(TestScatterToAllArray, typeof(TransfererScatterTests).Name, "TestScatterToAllArray",
                TransfererChoice.PerSubdomain, SubdomainDistribution.OnePerProcess);
            suite.AddTheory(TestScatterToAllArray, typeof(TransfererScatterTests).Name, "TestScatterToAllArray",
                TransfererChoice.PerSubdomain, SubdomainDistribution.Uniform);
            suite.AddTheory(TestScatterToAllArray, typeof(TransfererScatterTests).Name, "TestScatterToAllArray",
                TransfererChoice.PerSubdomain, SubdomainDistribution.Variable);

            suite.AddTheory(TestScatterToAllClass, typeof(TransfererScatterTests).Name, "TestScatterToAllClass",
                TransfererChoice.PerSubdomain, SubdomainDistribution.OnePerProcess);
            suite.AddTheory(TestScatterToAllClass, typeof(TransfererScatterTests).Name, "TestScatterToAllClass",
                TransfererChoice.PerSubdomain, SubdomainDistribution.Uniform);
            suite.AddTheory(TestScatterToAllClass, typeof(TransfererScatterTests).Name, "TestScatterToAllClass",
                TransfererChoice.PerSubdomain, SubdomainDistribution.Variable);

            suite.AddTheory(TestScatterToAllClassPacked, typeof(TransfererScatterTests).Name, "TestScatterToAllClassPacked",
                TransfererChoice.PerSubdomain, SubdomainDistribution.OnePerProcess);
            suite.AddTheory(TestScatterToAllClassPacked, typeof(TransfererScatterTests).Name, "TestScatterToAllClassPacked",
                TransfererChoice.PerSubdomain, SubdomainDistribution.Uniform);
            suite.AddTheory(TestScatterToAllClassPacked, typeof(TransfererScatterTests).Name, "TestScatterToAllClassPacked",
                TransfererChoice.PerSubdomain, SubdomainDistribution.Variable);

            suite.AddTheory(TestScatterToAllClassPackedArray, typeof(TransfererScatterTests).Name, "TestScatterToAllClassPackedArray",
                TransfererChoice.PerSubdomain, SubdomainDistribution.OnePerProcess);
            suite.AddTheory(TestScatterToAllClassPackedArray, typeof(TransfererScatterTests).Name, "TestScatterToAllClassPackedArray",
                TransfererChoice.PerSubdomain, SubdomainDistribution.Uniform);
            suite.AddTheory(TestScatterToAllClassPackedArray, typeof(TransfererScatterTests).Name, "TestScatterToAllClassPackedArray",
                TransfererChoice.PerSubdomain, SubdomainDistribution.Variable);

            suite.AddTheory(TestScatterToSomePrimitive, typeof(TransfererScatterTests).Name, "TestScatterToSomePrimitive",
                TransfererChoice.PerSubdomain, SubdomainDistribution.OnePerProcess);
            suite.AddTheory(TestScatterToSomePrimitive, typeof(TransfererScatterTests).Name, "TestScatterToSomePrimitive",
                TransfererChoice.PerSubdomain, SubdomainDistribution.Uniform);
            suite.AddTheory(TestScatterToSomePrimitive, typeof(TransfererScatterTests).Name, "TestScatterToSomePrimitive",
                TransfererChoice.PerSubdomain, SubdomainDistribution.Variable);

            suite.AddTheory(TestScatterToSomeArray, typeof(TransfererScatterTests).Name, "TestScatterToSomeArray",
                TransfererChoice.PerSubdomain, SubdomainDistribution.OnePerProcess);
            suite.AddTheory(TestScatterToSomeArray, typeof(TransfererScatterTests).Name, "TestScatterToSomeArray",
                TransfererChoice.PerSubdomain, SubdomainDistribution.Uniform);
            suite.AddTheory(TestScatterToSomeArray, typeof(TransfererScatterTests).Name, "TestScatterToSomeArray",
                TransfererChoice.PerSubdomain, SubdomainDistribution.Variable);

            suite.AddTheory(TestScatterToSomeClass, typeof(TransfererScatterTests).Name, "TestScatterToSomeClass",
                TransfererChoice.PerSubdomain, SubdomainDistribution.OnePerProcess);
            suite.AddTheory(TestScatterToSomeClass, typeof(TransfererScatterTests).Name, "TestScatterToSomeClass",
                TransfererChoice.PerSubdomain, SubdomainDistribution.Uniform);
            suite.AddTheory(TestScatterToSomeClass, typeof(TransfererScatterTests).Name, "TestScatterToSomeClass",
                TransfererChoice.PerSubdomain, SubdomainDistribution.Variable);

            suite.AddTheory(TestScatterToSomeClassPacked, typeof(TransfererScatterTests).Name, "TestScatterToSomeClassPacked",
                TransfererChoice.PerSubdomain, SubdomainDistribution.OnePerProcess);
            suite.AddTheory(TestScatterToSomeClassPacked, typeof(TransfererScatterTests).Name, "TestScatterToSomeClassPacked",
                TransfererChoice.PerSubdomain, SubdomainDistribution.Uniform);
            suite.AddTheory(TestScatterToSomeClassPacked, typeof(TransfererScatterTests).Name, "TestScatterToSomeClassPacked",
                TransfererChoice.PerSubdomain, SubdomainDistribution.Variable);

            suite.AddTheory(TestScatterToSomeClassPackedArray, typeof(TransfererScatterTests).Name, "TestScatterToSomeClassPackedArray",
                TransfererChoice.PerSubdomain, SubdomainDistribution.OnePerProcess);
            suite.AddTheory(TestScatterToSomeClassPackedArray, typeof(TransfererScatterTests).Name, "TestScatterToSomeClassPackedArray",
                TransfererChoice.PerSubdomain, SubdomainDistribution.Uniform);
            suite.AddTheory(TestScatterToSomeClassPackedArray, typeof(TransfererScatterTests).Name, "TestScatterToSomeClassPackedArray",
                TransfererChoice.PerSubdomain, SubdomainDistribution.Variable);

            // Tests for: TransfererAltogetherFlattened
            suite.AddTheory(TestScatterToAllPrimitive, typeof(TransfererScatterTests).Name, "TestScatterToAllPrimitive",
                TransfererChoice.AltogetherFlattened, SubdomainDistribution.OnePerProcess);
            suite.AddTheory(TestScatterToAllPrimitive, typeof(TransfererScatterTests).Name, "TestScatterToAllPrimitive",
                TransfererChoice.AltogetherFlattened, SubdomainDistribution.Uniform);
            suite.AddTheory(TestScatterToAllPrimitive, typeof(TransfererScatterTests).Name, "TestScatterToAllPrimitive",
                TransfererChoice.AltogetherFlattened, SubdomainDistribution.Variable);

            suite.AddTheory(TestScatterToAllArray, typeof(TransfererScatterTests).Name, "TestScatterToAllArray",
                TransfererChoice.AltogetherFlattened, SubdomainDistribution.OnePerProcess);
            suite.AddTheory(TestScatterToAllArray, typeof(TransfererScatterTests).Name, "TestScatterToAllArray",
                TransfererChoice.AltogetherFlattened, SubdomainDistribution.Uniform);
            suite.AddTheory(TestScatterToAllArray, typeof(TransfererScatterTests).Name, "TestScatterToAllArray",
                TransfererChoice.AltogetherFlattened, SubdomainDistribution.Variable);

            suite.AddTheory(TestScatterToAllClass, typeof(TransfererScatterTests).Name, "TestScatterToAllClass",
                TransfererChoice.AltogetherFlattened, SubdomainDistribution.OnePerProcess);
            suite.AddTheory(TestScatterToAllClass, typeof(TransfererScatterTests).Name, "TestScatterToAllClass",
                TransfererChoice.AltogetherFlattened, SubdomainDistribution.Uniform);
            suite.AddTheory(TestScatterToAllClass, typeof(TransfererScatterTests).Name, "TestScatterToAllClass",
                TransfererChoice.AltogetherFlattened, SubdomainDistribution.Variable);

            suite.AddTheory(TestScatterToAllClassPacked, typeof(TransfererScatterTests).Name, "TestScatterToAllClassPacked",
                TransfererChoice.AltogetherFlattened, SubdomainDistribution.OnePerProcess);
            suite.AddTheory(TestScatterToAllClassPacked, typeof(TransfererScatterTests).Name, "TestScatterToAllClassPacked",
                TransfererChoice.AltogetherFlattened, SubdomainDistribution.Uniform);
            suite.AddTheory(TestScatterToAllClassPacked, typeof(TransfererScatterTests).Name, "TestScatterToAllClassPacked",
                TransfererChoice.AltogetherFlattened, SubdomainDistribution.Variable);

            suite.AddTheory(TestScatterToAllClassPackedArray, typeof(TransfererScatterTests).Name, "TestScatterToAllClassPackedArray",
                TransfererChoice.AltogetherFlattened, SubdomainDistribution.OnePerProcess);
            suite.AddTheory(TestScatterToAllClassPackedArray, typeof(TransfererScatterTests).Name, "TestScatterToAllClassPackedArray",
                TransfererChoice.AltogetherFlattened, SubdomainDistribution.Uniform);
            suite.AddTheory(TestScatterToAllClassPackedArray, typeof(TransfererScatterTests).Name, "TestScatterToAllClassPackedArray",
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

        public static void TestScatterToAllClassPackedArray(TransfererChoice transfererChoice,
            SubdomainDistribution subdomainDistribution)
        {
            TestScatterTemplate(transfererChoice, subdomainDistribution, true,
                s => GetClassDataOfSubdomain(s),
                (transf, allData, activeSubdomains) =>
                {
                    GetArrayLengthOfPackedData<SampleClass> getPackedDataLength = (id, data) => data.PackedArrayLength;
                    PackSubdomainDataIntoArray<SampleClass, int> packData = 
                        (id, data, packingArray, offset) => data.PackIntoArray(packingArray, offset);
                    UnpackSubdomainDataFromArray<SampleClass, int> unpackData = 
                        (id, packingArray, start, end) => SampleClass.UnpackFromArray(id, packingArray, start, end);
                    return transf.ScatterToAllSubdomainsPacked(allData, getPackedDataLength, packData, unpackData);
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

        public static void TestScatterToSomeClassPackedArray(TransfererChoice transfererChoice,
            SubdomainDistribution subdomainDistribution)
        {
            TestScatterTemplate(transfererChoice, subdomainDistribution, false,
                s => GetClassDataOfSubdomain(s),
                (transf, allData, activeSubdomains) =>
                {
                    GetArrayLengthOfPackedData<SampleClass> getPackedDataLength = (id, data) => data.PackedArrayLength;
                    PackSubdomainDataIntoArray<SampleClass, int> packData =
                        (id, data, packingArray, offset) => data.PackIntoArray(packingArray, offset);
                    UnpackSubdomainDataFromArray<SampleClass, int> unpackData =
                        (id, packingArray, start, end) => SampleClass.UnpackFromArray(id, packingArray, start, end);
                    return transf.ScatterToSomeSubdomainsPacked(allData, getPackedDataLength, packData, unpackData, 
                        activeSubdomains);
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
