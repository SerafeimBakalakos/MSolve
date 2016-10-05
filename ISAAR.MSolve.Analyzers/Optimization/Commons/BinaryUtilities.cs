using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ISAAR.MSolve.Analyzers.Optimization.Commons
{
    static class BinaryUtilities
    {
        /// <summary>
        /// Converts a binary subarray to a decimal integer 
        /// </summary>
        /// <param name="bits"></param>
        /// <param name="start"></param>
        /// <param name="end">Exclusive</param>
        /// <returns></returns>
        public static int BinaryToDecimal(bool[] bits, long start, long length)
        {
            int sum = 0;
            for (int gene = 0; gene < length; ++gene)
            {
                if (bits[start + gene]) sum += (int)Math.Round(Math.Pow(2, gene));
            }
            return sum;
        }

        /// <summary>
        /// Converts a Gray-coded binary subarray to a decimal integer 
        /// </summary>
        /// <param name="genotype"></param>
        /// <param name="start"></param>
        /// <param name="end">Exclusive</param>
        /// <returns></returns>
        public static int GrayCodeToDecimal(bool[] bits, long start, long length)
        {
            throw new NotImplementedException();
        }
    }
}
