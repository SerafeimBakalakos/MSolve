using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ISAAR.MSolve.Analyzers.Optimization.Commons
{
    public static class BinaryUtilities
    {
        /// <summary>
        /// Converts a binary subarray to a decimal integer 
        /// </summary>
        /// <param name="bits"></param>
        /// <param name="start"></param>
        /// <param name="end">Exclusive</param>
        /// <returns></returns>
        public static int StandardBinaryToDecimal(bool[] bits, int start, int length)
        {
            int dec = 0;
            for (int i = start; i < start + length; ++i)
            {
                dec += bits[i] ? dec + 1: dec;
                //Console.WriteLine(dec);
            }
            return dec;
        }

        /// <summary>
        /// Converts a Gray-coded binary subarray to a decimal integer 
        /// </summary>
        /// <param name="genotype"></param>
        /// <param name="start"></param>
        /// <param name="end">Exclusive</param>
        /// <returns></returns>
        public static int GrayCodeToDecimal(bool[] bits, int start, int length)
        {
            bool bin = bits[start];
            int dec = bin ? 1 : 0;
            for (int i = start + 1; i < start + length; ++i) // n-1 repetitions
            {
                bin = bin != bits[i];
                dec += bin ? dec + 1 : dec;
            }
            return dec;
        }

        public static void Test()
        {
            bool[] bits1 = new bool[] { false, false, false };
            WriteRepresentations(bits1);
            bool[] bits2 = new bool[] { false, false, true };
            WriteRepresentations(bits2);
            bool[] bits3 = new bool[] { false, true, false };
            WriteRepresentations(bits3);
            bool[] bits4 = new bool[] { false, true, true };
            WriteRepresentations(bits4);
            bool[] bits5 = new bool[] { true, false, false };
            WriteRepresentations(bits5);
            bool[] bits6 = new bool[] { true, false, true };
            WriteRepresentations(bits6);
            bool[] bits7 = new bool[] { true, true, false };
            WriteRepresentations(bits7);
            bool[] bits8 = new bool[] { true, true, true };
            WriteRepresentations(bits8);
        }

        private static void WriteRepresentations(bool[] bits)
        {
            Console.Write("Bits = ");
            foreach (var entry in bits) Console.Write(entry ? 1 : 0);
            Console.Write(" -> from binary: " + StandardBinaryToDecimal(bits, 0, bits.Length));
            Console.WriteLine(" , from Gray code: " + GrayCodeToDecimal(bits, 0, bits.Length));
        }
    }
}
