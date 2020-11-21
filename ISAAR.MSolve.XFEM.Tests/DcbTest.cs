using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Solvers.Direct;
using Xunit;

namespace ISAAR.MSolve.XFEM_OLD.Tests
{
    public class DcbTest
    {
        [Fact]
        public static void Test()
        {
            var benchmark = new DcbBenchmarkBelytschko.Builder(20, 1, 1).BuildBenchmark();
            benchmark.InitializeModel();
            benchmark.Analyze(new SkylineSolver.Builder().BuildSolver(benchmark.Model));
        }
    }
}
