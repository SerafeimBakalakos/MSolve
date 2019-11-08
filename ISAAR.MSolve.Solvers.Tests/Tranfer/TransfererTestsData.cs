using System;
using System.Collections.Generic;
using System.Text;

namespace ISAAR.MSolve.Solvers.Tests.Tranfer
{
    internal static class TransfererTestsData
    {
        internal static bool CheckEquality<T>(T[] array1, T[] array2)
        {
            if (array1.Length != array2.Length) return false;
            for (int i = 0; i < array1.Length; ++i)
            {
                if (!array1[i].Equals(array2[i])) return false;
            }
            return true;
        }

        internal static double[] GetArrayDataOfSubdomain(int subdomainID)
        {
            int length = subdomainID + 4;
            var data = new double[length];
            for (int i = 0; i < length; ++i) data[i] = (subdomainID + 1) * i / 10;
            return data;
        }

        internal static SampleClass GetClassDataOfSubdomain(int subdomainID)
        {
            int length = 2 * subdomainID + 1;
            var data = new int[length];
            for (int i = 0; i < length; ++i) data[i] = subdomainID + 11;
            return new SampleClass(subdomainID, data);
        }

        internal static long GetPrimitiveDataOfSubdomain(int subdomainID) => subdomainID * 5 + 3;

        [Serializable]
        internal class SampleClass
        {
            private readonly int id;
            private readonly int[] data;

            public SampleClass(int id, int[] data)
            {
                this.id = id;
                this.data = data;
            }

            public int ID => id;
            public int[] Data => data;

            public bool Equals(SampleClass other)
            {
                if (this.id != other.id) return false;
                return CheckEquality(this.data, other.data);
            }
        }

        [Serializable]
        internal class SampleDto
        {
            private int[] data;

            public SampleDto(SampleClass obj)
            {
                this.data = new int[obj.Data.Length];
                this.data[0] = obj.ID;
                Array.Copy(obj.Data, 0, this.data, 1, obj.Data.Length);
            }

            public SampleClass Unpack()
            {
                var rawData = new int[this.data.Length - 1];
                Array.Copy(this.data, 1, rawData, 0, rawData.Length);
                return new SampleClass(this.data[0], rawData);
            }
        }
    }
}
