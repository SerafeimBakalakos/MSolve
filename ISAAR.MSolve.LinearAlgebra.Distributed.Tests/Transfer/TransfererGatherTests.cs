using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Distributed;
using ISAAR.MSolve.LinearAlgebra.Distributed.Tests;
using ISAAR.MSolve.LinearAlgebra.Distributed.Transfer;
using ISAAR.MSolve.LinearAlgebra.Tests.Utilities;
using MPI;
using Xunit;
using static ISAAR.MSolve.LinearAlgebra.Distributed.Tests.Tranfer.TransferrerTestsData;
using static ISAAR.MSolve.LinearAlgebra.Distributed.Tests.Tranfer.TransferrerTestUtilities;

namespace ISAAR.MSolve.LinearAlgebra.Distributed.Tests.Tranfer
{
    public static class TransferrerGatherTests
    {
        /// <summary>
        /// All tests need 4 MPI processes.
        /// </summary>
        /// <param name="suite"></param>
        public static void RegisterAllTests(MpiTestSuite suite)
        {
            // Tests for: TransferrerPerSubdomain
            suite.AddTheory(TestGatherFromAllPrimitive, typeof(TransferrerGatherTests).Name, "TestGatherFromAllPrimitive",
                TransferrerChoice.PerSubdomain, SubdomainDistribution.OnePerProcess);
            suite.AddTheory(TestGatherFromAllPrimitive, typeof(TransferrerGatherTests).Name, "TestGatherFromAllPrimitive",
                TransferrerChoice.PerSubdomain, SubdomainDistribution.Uniform);
            suite.AddTheory(TestGatherFromAllPrimitive, typeof(TransferrerGatherTests).Name, "TestGatherFromAllPrimitive",
                TransferrerChoice.PerSubdomain, SubdomainDistribution.Variable);

            suite.AddTheory(TestGatherFromAllArray, typeof(TransferrerGatherTests).Name, "TestGatherFromAllArray",
                TransferrerChoice.PerSubdomain, SubdomainDistribution.OnePerProcess);
            suite.AddTheory(TestGatherFromAllArray, typeof(TransferrerGatherTests).Name, "TestGatherFromAllArray",
                TransferrerChoice.PerSubdomain, SubdomainDistribution.Uniform);
            suite.AddTheory(TestGatherFromAllArray, typeof(TransferrerGatherTests).Name, "TestGatherFromAllArray",
                TransferrerChoice.PerSubdomain, SubdomainDistribution.Variable);

            suite.AddTheory(TestGatherFromAllClass, typeof(TransferrerGatherTests).Name, "TestGatherFromAllClass",
                TransferrerChoice.PerSubdomain, SubdomainDistribution.OnePerProcess);
            suite.AddTheory(TestGatherFromAllClass, typeof(TransferrerGatherTests).Name, "TestGatherFromAllClass",
                TransferrerChoice.PerSubdomain, SubdomainDistribution.Uniform);
            suite.AddTheory(TestGatherFromAllClass, typeof(TransferrerGatherTests).Name, "TestGatherFromAllClass",
                TransferrerChoice.PerSubdomain, SubdomainDistribution.Variable);

            suite.AddTheory(TestGatherFromAllClassPacked, typeof(TransferrerGatherTests).Name, "TestGatherFromAllClassPacked",
                TransferrerChoice.PerSubdomain, SubdomainDistribution.OnePerProcess);
            suite.AddTheory(TestGatherFromAllClassPacked, typeof(TransferrerGatherTests).Name, "TestGatherFromAllClassPacked",
                TransferrerChoice.PerSubdomain, SubdomainDistribution.Uniform);
            suite.AddTheory(TestGatherFromAllClassPacked, typeof(TransferrerGatherTests).Name, "TestGatherFromAllClassPacked",
                TransferrerChoice.PerSubdomain, SubdomainDistribution.Variable);

            suite.AddTheory(TestGatherFromAllClassPackedArray, typeof(TransferrerGatherTests).Name, "TestGatherFromAllClassPackedArray",
                TransferrerChoice.PerSubdomain, SubdomainDistribution.OnePerProcess);
            suite.AddTheory(TestGatherFromAllClassPackedArray, typeof(TransferrerGatherTests).Name, "TestGatherFromAllClassPackedArray",
                TransferrerChoice.PerSubdomain, SubdomainDistribution.Uniform);
            suite.AddTheory(TestGatherFromAllClassPackedArray, typeof(TransferrerGatherTests).Name, "TestGatherFromAllClassPackedArray",
                TransferrerChoice.PerSubdomain, SubdomainDistribution.Variable);

            suite.AddTheory(TestGatherFromSomePrimitive, typeof(TransferrerGatherTests).Name, "TestGatherFromSomePrimitive",
                TransferrerChoice.PerSubdomain, SubdomainDistribution.OnePerProcess);
            suite.AddTheory(TestGatherFromSomePrimitive, typeof(TransferrerGatherTests).Name, "TestGatherFromSomePrimitive",
                TransferrerChoice.PerSubdomain, SubdomainDistribution.Uniform);
            suite.AddTheory(TestGatherFromSomePrimitive, typeof(TransferrerGatherTests).Name, "TestGatherFromSomePrimitive",
                TransferrerChoice.PerSubdomain, SubdomainDistribution.Variable);

            suite.AddTheory(TestGatherFromSomeArray, typeof(TransferrerGatherTests).Name, "TestGatherFromSomeArray",
                TransferrerChoice.PerSubdomain, SubdomainDistribution.OnePerProcess);
            suite.AddTheory(TestGatherFromSomeArray, typeof(TransferrerGatherTests).Name, "TestGatherFromSomeArray",
                TransferrerChoice.PerSubdomain, SubdomainDistribution.Uniform);
            suite.AddTheory(TestGatherFromSomeArray, typeof(TransferrerGatherTests).Name, "TestGatherFromSomeArray",
                TransferrerChoice.PerSubdomain, SubdomainDistribution.Variable);

            suite.AddTheory(TestGatherFromSomeClass, typeof(TransferrerGatherTests).Name, "TestGatherFromSomeClass",
                TransferrerChoice.PerSubdomain, SubdomainDistribution.OnePerProcess);
            suite.AddTheory(TestGatherFromSomeClass, typeof(TransferrerGatherTests).Name, "TestGatherFromSomeClass",
                TransferrerChoice.PerSubdomain, SubdomainDistribution.Uniform);
            suite.AddTheory(TestGatherFromSomeClass, typeof(TransferrerGatherTests).Name, "TestGatherFromSomeClass",
                TransferrerChoice.PerSubdomain, SubdomainDistribution.Variable);

            suite.AddTheory(TestGatherFromSomeClassPacked, typeof(TransferrerGatherTests).Name, "TestGatherFromSomeClassPacked",
                TransferrerChoice.PerSubdomain, SubdomainDistribution.OnePerProcess);
            suite.AddTheory(TestGatherFromSomeClassPacked, typeof(TransferrerGatherTests).Name, "TestGatherFromSomeClassPacked",
                TransferrerChoice.PerSubdomain, SubdomainDistribution.Uniform);
            suite.AddTheory(TestGatherFromSomeClassPacked, typeof(TransferrerGatherTests).Name, "TestGatherFromSomeClassPacked",
                TransferrerChoice.PerSubdomain, SubdomainDistribution.Variable);

            suite.AddTheory(TestGatherFromSomeClassPackedArray, typeof(TransferrerGatherTests).Name, "TestGatherFromSomeClassPackedArray",
                TransferrerChoice.PerSubdomain, SubdomainDistribution.OnePerProcess);
            suite.AddTheory(TestGatherFromSomeClassPackedArray, typeof(TransferrerGatherTests).Name, "TestGatherFromSomeClassPackedArray",
                TransferrerChoice.PerSubdomain, SubdomainDistribution.Uniform);
            suite.AddTheory(TestGatherFromSomeClassPackedArray, typeof(TransferrerGatherTests).Name, "TestGatherFromSomeClassPackedArray",
                TransferrerChoice.PerSubdomain, SubdomainDistribution.Variable);

            // Tests for: TransferrerAltogetherFlattened
            suite.AddTheory(TestGatherFromAllPrimitive, typeof(TransferrerGatherTests).Name, "TestGatherFromAllPrimitive",
                TransferrerChoice.AltogetherFlattened, SubdomainDistribution.OnePerProcess);
            suite.AddTheory(TestGatherFromAllPrimitive, typeof(TransferrerGatherTests).Name, "TestGatherFromAllPrimitive",
                TransferrerChoice.AltogetherFlattened, SubdomainDistribution.Uniform);
            suite.AddTheory(TestGatherFromAllPrimitive, typeof(TransferrerGatherTests).Name, "TestGatherFromAllPrimitive",
                TransferrerChoice.AltogetherFlattened, SubdomainDistribution.Variable);

            suite.AddTheory(TestGatherFromAllArray, typeof(TransferrerGatherTests).Name, "TestGatherFromAllArray",
                TransferrerChoice.AltogetherFlattened, SubdomainDistribution.OnePerProcess);
            suite.AddTheory(TestGatherFromAllArray, typeof(TransferrerGatherTests).Name, "TestGatherFromAllArray",
                TransferrerChoice.AltogetherFlattened, SubdomainDistribution.Uniform);
            suite.AddTheory(TestGatherFromAllArray, typeof(TransferrerGatherTests).Name, "TestGatherFromAllArray",
                TransferrerChoice.AltogetherFlattened, SubdomainDistribution.Variable);

            suite.AddTheory(TestGatherFromAllClass, typeof(TransferrerGatherTests).Name, "TestGatherFromAllClass",
                TransferrerChoice.AltogetherFlattened, SubdomainDistribution.OnePerProcess);
            suite.AddTheory(TestGatherFromAllClass, typeof(TransferrerGatherTests).Name, "TestGatherFromAllClass",
                TransferrerChoice.AltogetherFlattened, SubdomainDistribution.Uniform);
            suite.AddTheory(TestGatherFromAllClass, typeof(TransferrerGatherTests).Name, "TestGatherFromAllClass",
                TransferrerChoice.AltogetherFlattened, SubdomainDistribution.Variable);

            suite.AddTheory(TestGatherFromAllClassPacked, typeof(TransferrerGatherTests).Name, "TestGatherFromAllClassPacked",
                TransferrerChoice.AltogetherFlattened, SubdomainDistribution.OnePerProcess);
            suite.AddTheory(TestGatherFromAllClassPacked, typeof(TransferrerGatherTests).Name, "TestGatherFromAllClassPacked",
                TransferrerChoice.AltogetherFlattened, SubdomainDistribution.Uniform);
            suite.AddTheory(TestGatherFromAllClassPacked, typeof(TransferrerGatherTests).Name, "TestGatherFromAllClassPacked",
                TransferrerChoice.AltogetherFlattened, SubdomainDistribution.Variable);

            suite.AddTheory(TestGatherFromAllClassPackedArray, typeof(TransferrerGatherTests).Name, "TestGatherFromAllClassPackedArray",
                TransferrerChoice.AltogetherFlattened, SubdomainDistribution.OnePerProcess);
            suite.AddTheory(TestGatherFromAllClassPackedArray, typeof(TransferrerGatherTests).Name, "TestGatherFromAllClassPackedArray",
                TransferrerChoice.AltogetherFlattened, SubdomainDistribution.Uniform);
            suite.AddTheory(TestGatherFromAllClassPackedArray, typeof(TransferrerGatherTests).Name, "TestGatherFromAllClassPackedArray",
                TransferrerChoice.AltogetherFlattened, SubdomainDistribution.Variable);
        }

        public static void TestGatherFromAllArray(TransferrerChoice transferrerChoice,
            SubdomainDistribution subdomainDistribution)
        {
            TestGatherTemplate(transferrerChoice, subdomainDistribution, true,
                s => GetArrayDataOfSubdomain(s),
                (transf, allData, activeSubdomains) => transf.GatherFromAllSubdomains(allData),
                (s, computed) => Assert.True(CheckEquality(GetArrayDataOfSubdomain(s), computed)));
        }

        public static void TestGatherFromAllClass(TransferrerChoice transferrerChoice,
            SubdomainDistribution subdomainDistribution)
        {
            TestGatherTemplate(transferrerChoice, subdomainDistribution, true,
                s => GetClassDataOfSubdomain(s),
                (transf, allData, activeSubdomains) => transf.GatherFromAllSubdomains(allData),
                (s, computed) => Assert.True(GetClassDataOfSubdomain(s).Equals(computed)));
        }

        public static void TestGatherFromAllClassPacked(TransferrerChoice transferrerChoice,
            SubdomainDistribution subdomainDistribution)
        {
            TestGatherTemplate(transferrerChoice, subdomainDistribution, true,
                s => GetClassDataOfSubdomain(s),
                (transf, allData, activeSubdomains) =>
                {
                    PackSubdomainData<SampleClass, SampleDto> packData = (id, data) => new SampleDto(data);
                    UnpackSubdomainData<SampleClass, SampleDto> unpackData = (id, dto) => dto.Unpack();
                    return transf.GatherFromAllSubdomainsPacked(allData, packData, unpackData);
                },
                (s, computed) => Assert.True(GetClassDataOfSubdomain(s).Equals(computed)));
        }

        public static void TestGatherFromAllClassPackedArray(TransferrerChoice transferrerChoice,
            SubdomainDistribution subdomainDistribution)
        {
            TestGatherTemplate(transferrerChoice, subdomainDistribution, true,
                s => GetClassDataOfSubdomain(s),
                (transf, allData, activeSubdomains) =>
                {
                    GetArrayLengthOfPackedData<SampleClass> getPackedDataLength = (id, data) => data.PackedArrayLength;
                    PackSubdomainDataIntoArray<SampleClass, int> packData = 
                        (id, data, buffer, offset) => data.PackIntoArray(buffer, offset);
                    UnpackSubdomainDataFromArray<SampleClass, int> unpackData = 
                        (id, buffer, start, end) => SampleClass.UnpackFromArray(id, buffer, start, end);
                    return transf.GatherFromAllSubdomainsPacked(allData, getPackedDataLength, packData, unpackData);
                },
                (s, computed) => Assert.True(GetClassDataOfSubdomain(s).Equals(computed)));
        }

        public static void TestGatherFromAllPrimitive(TransferrerChoice transferrerChoice, 
            SubdomainDistribution subdomainDistribution)
        {
            TestGatherTemplate(transferrerChoice, subdomainDistribution, true,
                s => GetPrimitiveDataOfSubdomain(s),
                (transf, allData, activeSubdomains) => transf.GatherFromAllSubdomains(allData),
                (s, computed) => Assert.Equal(GetPrimitiveDataOfSubdomain(s), computed));
        }

        public static void TestGatherFromSomeArray(TransferrerChoice transferrerChoice,
            SubdomainDistribution subdomainDistribution)
        {
            TestGatherTemplate(transferrerChoice, subdomainDistribution, false,
                s => GetArrayDataOfSubdomain(s),
                (transf, allData, activeSubdomains) => transf.GatherFromSomeSubdomains<double>(allData, activeSubdomains),
                (s, computed) => Assert.True(CheckEquality(GetArrayDataOfSubdomain(s), computed)));
        }

        public static void TestGatherFromSomeClass(TransferrerChoice transferrerChoice,
            SubdomainDistribution subdomainDistribution)
        {
            TestGatherTemplate(transferrerChoice, subdomainDistribution, false,
                s => GetClassDataOfSubdomain(s),
                (transf, allData, activeSubdomains) => transf.GatherFromSomeSubdomains(allData, activeSubdomains),
                (s, computed) => Assert.True(GetClassDataOfSubdomain(s).Equals(computed)));
        }

        public static void TestGatherFromSomeClassPacked(TransferrerChoice transferrerChoice,
            SubdomainDistribution subdomainDistribution)
        {
            TestGatherTemplate(transferrerChoice, subdomainDistribution, false,
                s => GetClassDataOfSubdomain(s),
                (transf, allData, activeSubdomains) =>
                {
                    PackSubdomainData<SampleClass, SampleDto> packData = (id, data) => new SampleDto(data);
                    UnpackSubdomainData<SampleClass, SampleDto> unpackData = (id, dto) => dto.Unpack();
                    return transf.GatherFromSomeSubdomainsPacked(allData, packData, unpackData, activeSubdomains);
                },
                (s, computed) => Assert.True(GetClassDataOfSubdomain(s).Equals(computed)));
        }

        public static void TestGatherFromSomeClassPackedArray(TransferrerChoice transferrerChoice,
            SubdomainDistribution subdomainDistribution)
        {
            TestGatherTemplate(transferrerChoice, subdomainDistribution, false,
                s => GetClassDataOfSubdomain(s),
                (transf, allData, activeSubdomains) =>
                {
                    GetArrayLengthOfPackedData<SampleClass> getPackedDataLength = (id, data) => data.PackedArrayLength;
                    PackSubdomainDataIntoArray<SampleClass, int> packData =
                        (id, data, buffer, offset) => data.PackIntoArray(buffer, offset);
                    UnpackSubdomainDataFromArray<SampleClass, int> unpackData =
                        (id, buffer, start, end) => SampleClass.UnpackFromArray(id, buffer, start, end);
                    return transf.GatherFromSomeSubdomainsPacked(allData, getPackedDataLength, packData, unpackData, 
                        activeSubdomains);
                },
                (s, computed) => Assert.True(GetClassDataOfSubdomain(s).Equals(computed)));
        }

        public static void TestGatherFromSomePrimitive(TransferrerChoice transferrerChoice,
            SubdomainDistribution subdomainDistribution)
        {
            TestGatherTemplate(transferrerChoice, subdomainDistribution, false,
                s => GetPrimitiveDataOfSubdomain(s),
                (transf, allData, activeSubdomains) => transf.GatherFromSomeSubdomains(allData, activeSubdomains),
                (s, computed) => Assert.Equal(GetPrimitiveDataOfSubdomain(s), computed));
        }

        private static void TestGatherTemplate<T>(TransferrerChoice transferrerChoice,
            SubdomainDistribution subdomainDistribution, bool gatherAll, Func<int, T> createSubdomainData,
            Func<ISubdomainDataTransferrer, Dictionary<int, T>, ActiveSubdomains, Dictionary<int, T>> gatherSubdomainData,
            Action<int, T> checkReceivedData)
        {
            ProcessDistribution procs = DetermineProcesses(subdomainDistribution);
            ISubdomainDataTransferrer transferrer = DetermineTransferrer(transferrerChoice, procs);
            ActiveSubdomains activeSubdomains = DetermineActiveSubdomains(procs);

            // Prepare data in each process
            Dictionary<int, T> processData = new Dictionary<int, T>();
            foreach (int s in procs.GetSubdomainIdsOfProcess(procs.OwnRank))
            {
                if (gatherAll || activeSubdomains.IsActive(s)) processData[s] = createSubdomainData(s);
            }

            // Gather them in master
            Dictionary<int, T> allData_master = gatherSubdomainData(transferrer, processData, activeSubdomains);

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
