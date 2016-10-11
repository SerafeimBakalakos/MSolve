﻿using ISAAR.MSolve.Analyzers.Optimization.Commons;
using ISAAR.MSolve.Analyzers.Optimization.Problems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Troschuetz.Random;

namespace ISAAR.MSolve.Analyzers.Optimization.Algorithms.Metaheuristics.GeneticAlgorithms.Encodings
{
    public class GrayCodeEncoding : AbstractBinaryEncoding
    {
        public GrayCodeEncoding(OptimizationProblem problem, int bitsPerContinuousVariable, int bitsPerIntegerVariable):
                        base(problem, bitsPerContinuousVariable, bitsPerIntegerVariable)
        {
        }

        protected sealed override int BitstringToDecimalInteger(bool[] bits, int start, int length)
        {
            return BinaryUtilities.GrayCodeToDecimal(bits, start, length);
        }

        protected override void DecimalIntegerToBitstring(int dec, bool[] bits, int start, int length)
        {
            BinaryUtilities.DecimalToGrayCode(dec, bits, start, length);
        }
    }
}
