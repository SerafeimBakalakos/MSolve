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
    public static class TransferrerScatterTests
    {
        /// <summary>
        /// All tests need 4 MPI processes.
        /// </summary>
        /// <param name="suite"></param>
        public static void RegisterAllTests(MpiTestSuite suite)
        {
            // Tests for: TransferrerPerSubdomain
            suite.AddTheory(TestScatterToAllPrimitive, typeof(TransferrerScatterTests).Name, "TestScatterToAllPrimitive",
                TransferrerChoice.PerSubdomain, SubdomainDistribution.OnePerProcess);
            suite.AddTheory(TestScatterToAllPrimitive, typeof(TransferrerScatterTests).Name, "TestScatterToAllPrimitive",
                TransferrerChoice.PerSubdomain, SubdomainDistribution.Uniform);
            suite.AddTheory(TestScatterToAllPrimitive, typeof(TransferrerScatterTests).Name, "TestScatterToAllPrimitive",
                TransferrerChoice.PerSubdomain, SubdomainDistribution.Variable);

            suite.AddTheory(TestScatterToAllArray, typeof(TransferrerScatterTests).Name, "TestScatterToAllArray",
                TransferrerChoice.PerSubdomain, SubdomainDistribution.OnePerProcess);
            suite.AddTheory(TestScatterToAllArray, typeof(TransferrerScatterTests).Name, "TestScatterToAllArray",
                TransferrerChoice.PerSubdomain, SubdomainDistribution.Uniform);
            suite.AddTheory(TestScatterToAllArray, typeof(TransferrerScatterTests).Name, "TestScatterToAllArray",
                TransferrerChoice.PerSubdomain, SubdomainDistribution.Variable);

            suite.AddTheory(TestScatterToAllClass, typeof(TransferrerScatterTests).Name, "TestScatterToAllClass",
                TransferrerChoice.PerSubdomain, SubdomainDistribution.OnePerProcess);
            suite.AddTheory(TestScatterToAllClass, typeof(TransferrerScatterTests).Name, "TestScatterToAllClass",
                TransferrerChoice.PerSubdomain, SubdomainDistribution.Uniform);
            suite.AddTheory(TestScatterToAllClass, typeof(TransferrerScatterTests).Name, "TestScatterToAllClass",
                TransferrerChoice.PerSubdomain, SubdomainDistribution.Variable);

            suite.AddTheory(TestScatterToAllClassPacked, typeof(TransferrerScatterTests).Name, "TestScatterToAllClassPacked",
                TransferrerChoice.PerSubdomain, SubdomainDistribution.OnePerProcess);
            suite.AddTheory(TestScatterToAllClassPacked, typeof(TransferrerScatterTests).Name, "TestScatterToAllClassPacked",
                TransferrerChoice.PerSubdomain, SubdomainDistribution.Uniform);
            suite.AddTheory(TestScatterToAllClassPacked, typeof(TransferrerScatterTests).Name, "TestScatterToAllClassPacked",
                TransferrerChoice.PerSubdomain, SubdomainDistribution.Variable);

            suite.AddTheory(TestScatterToAllClassPackedArray, typeof(TransferrerScatterTests).Name, "TestScatterToAllClassPackedArray",
                TransferrerChoice.PerSubdomain, SubdomainDistribution.OnePerProcess);
            suite.AddTheory(TestScatterToAllClassPackedArray, typeof(TransferrerScatterTests).Name, "TestScatterToAllClassPackedArray",
                TransferrerChoice.PerSubdomain, SubdomainDistribution.Uniform);
            suite.AddTheory(TestScatterToAllClassPackedArray, typeof(TransferrerScatterTests).Name, "TestScatterToAllClassPackedArray",
                TransferrerChoice.PerSubdomain, SubdomainDistribution.Variable);

            suite.AddTheory(TestScatterToSomePrimitive, typeof(TransferrerScatterTests).Name, "TestScatterToSomePrimitive",
                TransferrerChoice.PerSubdomain, SubdomainDistribution.OnePerProcess);
            suite.AddTheory(TestScatterToSomePrimitive, typeof(TransferrerScatterTests).Name, "TestScatterToSomePrimitive",
                TransferrerChoice.PerSubdomain, SubdomainDistribution.Uniform);
            suite.AddTheory(TestScatterToSomePrimitive, typeof(TransferrerScatterTests).Name, "TestScatterToSomePrimitive",
                TransferrerChoice.PerSubdomain, SubdomainDistribution.Variable);

            suite.AddTheory(TestScatterToSomeArray, typeof(TransferrerScatterTests).Name, "TestScatterToSomeArray",
                TransferrerChoice.PerSubdomain, SubdomainDistribution.OnePerProcess);
            suite.AddTheory(TestScatterToSomeArray, typeof(TransferrerScatterTests).Name, "TestScatterToSomeArray",
                TransferrerChoice.PerSubdomain, SubdomainDistribution.Uniform);
            suite.AddTheory(TestScatterToSomeArray, typeof(TransferrerScatterTests).Name, "TestScatterToSomeArray",
                TransferrerChoice.PerSubdomain, SubdomainDistribution.Variable);

            suite.AddTheory(TestScatterToSomeClass, typeof(TransferrerScatterTests).Name, "TestScatterToSomeClass",
                TransferrerChoice.PerSubdomain, SubdomainDistribution.OnePerProcess);
            suite.AddTheory(TestScatterToSomeClass, typeof(TransferrerScatterTests).Name, "TestScatterToSomeClass",
                TransferrerChoice.PerSubdomain, SubdomainDistribution.Uniform);
            suite.AddTheory(TestScatterToSomeClass, typeof(TransferrerScatterTests).Name, "TestScatterToSomeClass",
                TransferrerChoice.PerSubdomain, SubdomainDistribution.Variable);

            suite.AddTheory(TestScatterToSomeClassPacked, typeof(TransferrerScatterTests).Name, "TestScatterToSomeClassPacked",
                TransferrerChoice.PerSubdomain, SubdomainDistribution.OnePerProcess);
            suite.AddTheory(TestScatterToSomeClassPacked, typeof(TransferrerScatterTests).Name, "TestScatterToSomeClassPacked",
                TransferrerChoice.PerSubdomain, SubdomainDistribution.Uniform);
            suite.AddTheory(TestScatterToSomeClassPacked, typeof(TransferrerScatterTests).Name, "TestScatterToSomeClassPacked",
                TransferrerChoice.PerSubdomain, SubdomainDistribution.Variable);

            suite.AddTheory(TestScatterToSomeClassPackedArray, typeof(TransferrerScatterTests).Name, "TestScatterToSomeClassPackedArray",
                TransferrerChoice.PerSubdomain, SubdomainDistribution.OnePerProcess);
            suite.AddTheory(TestScatterToSomeClassPackedArray, typeof(TransferrerScatterTests).Name, "TestScatterToSomeClassPackedArray",
                TransferrerChoice.PerSubdomain, SubdomainDistribution.Uniform);
            suite.AddTheory(TestScatterToSomeClassPackedArray, typeof(TransferrerScatterTests).Name, "TestScatterToSomeClassPackedArray",
                TransferrerChoice.PerSubdomain, SubdomainDistribution.Variable);

            // Tests for: TransferrerAltogetherFlattened
            suite.AddTheory(TestScatterToAllPrimitive, typeof(TransferrerScatterTests).Name, "TestScatterToAllPrimitive",
                TransferrerChoice.AltogetherFlattened, SubdomainDistribution.OnePerProcess);
            suite.AddTheory(TestScatterToAllPrimitive, typeof(TransferrerScatterTests).Name, "TestScatterToAllPrimitive",
                TransferrerChoice.AltogetherFlattened, SubdomainDistribution.Uniform);
            suite.AddTheory(TestScatterToAllPrimitive, typeof(TransferrerScatterTests).Name, "TestScatterToAllPrimitive",
                TransferrerChoice.AltogetherFlattened, SubdomainDistribution.Variable);

            suite.AddTheory(TestScatterToAllArray, typeof(TransferrerScatterTests).Name, "TestScatterToAllArray",
                TransferrerChoice.AltogetherFlattened, SubdomainDistribution.OnePerProcess);
            suite.AddTheory(TestScatterToAllArray, typeof(TransferrerScatterTests).Name, "TestScatterToAllArray",
                TransferrerChoice.AltogetherFlattened, SubdomainDistribution.Uniform);
            suite.AddTheory(TestScatterToAllArray, typeof(TransferrerScatterTests).Name, "TestScatterToAllArray",
                TransferrerChoice.AltogetherFlattened, SubdomainDistribution.Variable);

            suite.AddTheory(TestScatterToAllClass, typeof(TransferrerScatterTests).Name, "TestScatterToAllClass",
                TransferrerChoice.AltogetherFlattened, SubdomainDistribution.OnePerProcess);
            suite.AddTheory(TestScatterToAllClass, typeof(TransferrerScatterTests).Name, "TestScatterToAllClass",
                TransferrerChoice.AltogetherFlattened, SubdomainDistribution.Uniform);
            suite.AddTheory(TestScatterToAllClass, typeof(TransferrerScatterTests).Name, "TestScatterToAllClass",
                TransferrerChoice.AltogetherFlattened, SubdomainDistribution.Variable);

            suite.AddTheory(TestScatterToAllClassPacked, typeof(TransferrerScatterTests).Name, "TestScatterToAllClassPacked",
                TransferrerChoice.AltogetherFlattened, SubdomainDistribution.OnePerProcess);
            suite.AddTheory(TestScatterToAllClassPacked, typeof(TransferrerScatterTests).Name, "TestScatterToAllClassPacked",
                TransferrerChoice.AltogetherFlattened, SubdomainDistribution.Uniform);
            suite.AddTheory(TestScatterToAllClassPacked, typeof(TransferrerScatterTests).Name, "TestScatterToAllClassPacked",
                TransferrerChoice.AltogetherFlattened, SubdomainDistribution.Variable);

            suite.AddTheory(TestScatterToAllClassPackedArray, typeof(TransferrerScatterTests).Name, "TestScatterToAllClassPackedArray",
                TransferrerChoice.AltogetherFlattened, SubdomainDistribution.OnePerProcess);
            suite.AddTheory(TestScatterToAllClassPackedArray, typeof(TransferrerScatterTests).Name, "TestScatterToAllClassPackedArray",
                TransferrerChoice.AltogetherFlattened, SubdomainDistribution.Uniform);
            suite.AddTheory(TestScatterToAllClassPackedArray, typeof(TransferrerScatterTests).Name, "TestScatterToAllClassPackedArray",
                TransferrerChoice.AltogetherFlattened, SubdomainDistribution.Variable);
        }

        public static void TestScatterToAllArray(TransferrerChoice transferrerChoice,
            SubdomainDistribution subdomainDistribution)
        {
            TestScatterTemplate(transferrerChoice, subdomainDistribution, true,
                s => GetArrayDataOfSubdomain(s),
                (transf, allData, activeSubdomains) => transf.ScatterToAllSubdomains(allData),
                (s, computed) => Assert.True(CheckEquality(GetArrayDataOfSubdomain(s), computed)));
        }

        public static void TestScatterToAllClass(TransferrerChoice transferrerChoice,
            SubdomainDistribution subdomainDistribution)
        {
            TestScatterTemplate(transferrerChoice, subdomainDistribution, true,
                s => GetClassDataOfSubdomain(s),
                (transf, allData, activeSubdomains) => transf.ScatterToAllSubdomains(allData),
                (s, computed) => Assert.True(GetClassDataOfSubdomain(s).Equals(computed)));
        }

        public static void TestScatterToAllClassPacked(TransferrerChoice transferrerChoice,
            SubdomainDistribution subdomainDistribution)
        {
            TestScatterTemplate(transferrerChoice, subdomainDistribution, true,
                s => GetClassDataOfSubdomain(s),
                (transf, allData, activeSubdomains) =>
                {
                    PackSubdomainData<SampleClass, SampleDto> packData = (id, data) => new SampleDto(data);
                    UnpackSubdomainData<SampleClass, SampleDto> unpackData = (id, dto) => dto.Unpack();
                    return transf.ScatterToAllSubdomainsPacked(allData, packData, unpackData);
                },
                (s, computed) => Assert.True(GetClassDataOfSubdomain(s).Equals(computed)));
        }

        public static void TestScatterToAllClassPackedArray(TransferrerChoice transferrerChoice,
            SubdomainDistribution subdomainDistribution)
        {
            TestScatterTemplate(transferrerChoice, subdomainDistribution, true,
                s => GetClassDataOfSubdomain(s),
                (transf, allData, activeSubdomains) =>
                {
                    GetArrayLengthOfPackedData<SampleClass> getPackedDataLength = (id, data) => data.PackedArrayLength;
                    PackSubdomainDataIntoArray<SampleClass, int> packData = 
                        (id, data, buffer, offset) => data.PackIntoArray(buffer, offset);
                    UnpackSubdomainDataFromArray<SampleClass, int> unpackData = 
                        (id, buffer, start, end) => SampleClass.UnpackFromArray(id, buffer, start, end);
                    return transf.ScatterToAllSubdomainsPacked(allData, getPackedDataLength, packData, unpackData);
                },
                (s, computed) => Assert.True(GetClassDataOfSubdomain(s).Equals(computed)));
        }

        public static void TestScatterToAllPrimitive(TransferrerChoice transferrerChoice, 
            SubdomainDistribution subdomainDistribution)
        {
            TestScatterTemplate(transferrerChoice, subdomainDistribution, true,
                s => GetPrimitiveDataOfSubdomain(s),
                (transf, allData, activeSubdomains) => transf.ScatterToAllSubdomains(allData),
                (s, computed) => Assert.Equal(GetPrimitiveDataOfSubdomain(s), computed));
        }

        public static void TestScatterToSomeArray(TransferrerChoice transferrerChoice,
            SubdomainDistribution subdomainDistribution)
        {
            TestScatterTemplate(transferrerChoice, subdomainDistribution, false,
                s => GetArrayDataOfSubdomain(s),
                (transf, allData, activeSubdomains) => transf.ScatterToSomeSubdomains<double>(allData, activeSubdomains),
                (s, computed) => Assert.True(CheckEquality(GetArrayDataOfSubdomain(s), computed)));
        }

        public static void TestScatterToSomeClass(TransferrerChoice transferrerChoice,
            SubdomainDistribution subdomainDistribution)
        {
            TestScatterTemplate(transferrerChoice, subdomainDistribution, false,
                s => GetClassDataOfSubdomain(s),
                (transf, allData, activeSubdomains) => transf.ScatterToSomeSubdomains(allData, activeSubdomains),
                (s, computed) => Assert.True(GetClassDataOfSubdomain(s).Equals(computed)));
        }

        public static void TestScatterToSomeClassPacked(TransferrerChoice transferrerChoice,
            SubdomainDistribution subdomainDistribution)
        {
            TestScatterTemplate(transferrerChoice, subdomainDistribution, false,
                s => GetClassDataOfSubdomain(s),
                (transf, allData, activeSubdomains) =>
                {
                    PackSubdomainData<SampleClass, SampleDto> packData = (id, data) => new SampleDto(data);
                    UnpackSubdomainData<SampleClass, SampleDto> unpackData = (id, dto) => dto.Unpack();
                    return transf.ScatterToSomeSubdomainsPacked(allData, packData, unpackData, activeSubdomains);
                },
                (s, computed) => Assert.True(GetClassDataOfSubdomain(s).Equals(computed)));
        }

        public static void TestScatterToSomeClassPackedArray(TransferrerChoice transferrerChoice,
            SubdomainDistribution subdomainDistribution)
        {
            TestScatterTemplate(transferrerChoice, subdomainDistribution, false,
                s => GetClassDataOfSubdomain(s),
                (transf, allData, activeSubdomains) =>
                {
                    GetArrayLengthOfPackedData<SampleClass> getPackedDataLength = (id, data) => data.PackedArrayLength;
                    PackSubdomainDataIntoArray<SampleClass, int> packData =
                        (id, data, buffer, offset) => data.PackIntoArray(buffer, offset);
                    UnpackSubdomainDataFromArray<SampleClass, int> unpackData =
                        (id, buffer, start, end) => SampleClass.UnpackFromArray(id, buffer, start, end);
                    return transf.ScatterToSomeSubdomainsPacked(allData, getPackedDataLength, packData, unpackData, 
                        activeSubdomains);
                },
                (s, computed) => Assert.True(GetClassDataOfSubdomain(s).Equals(computed)));
        }

        public static void TestScatterToSomePrimitive(TransferrerChoice transferrerChoice,
            SubdomainDistribution subdomainDistribution)
        {
            TestScatterTemplate(transferrerChoice, subdomainDistribution, false,
                s => GetPrimitiveDataOfSubdomain(s),
                (transf, allData, activeSubdomains) => transf.ScatterToSomeSubdomains(allData, activeSubdomains),
                (s, computed) => Assert.Equal(GetPrimitiveDataOfSubdomain(s), computed));
        }

        private static void TestScatterTemplate<T>(TransferrerChoice transferrerChoice,
            SubdomainDistribution subdomainDistribution, bool scatterAll, Func<int, T> createSubdomainData,
            Func<ISubdomainDataTransferrer, Dictionary<int, T>, ActiveSubdomains, Dictionary<int, T>> scatterSubdomainData,
            Action<int, T> checkReceivedData)
        {
            ProcessDistribution procs = DetermineProcesses(subdomainDistribution);
            ISubdomainDataTransferrer transferrer = DetermineTransferrer(transferrerChoice, procs);
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
            Dictionary<int, T> processData = scatterSubdomainData(transferrer, allData_master, activeSubdomains);

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
