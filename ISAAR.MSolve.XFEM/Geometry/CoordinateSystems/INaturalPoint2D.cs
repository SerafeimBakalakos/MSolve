﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ISAAR.MSolve.XFEM.Geometry.CoordinateSystems
{
    interface INaturalPoint2D
    {
        double Xi { get; }
        double Eta { get; }
    }
}
