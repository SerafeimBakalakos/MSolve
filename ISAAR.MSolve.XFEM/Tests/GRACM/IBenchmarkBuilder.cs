﻿using System;
using System.Collections.Generic;
using System.Text;

namespace ISAAR.MSolve.XFEM.Tests.GRACM
{
    interface IBenchmarkBuilder
    {
        string LeftLsmPlotDirectory { get; set; }
        string TimingPath { get; }
        IBenchmark BuildBenchmark();
    }
}