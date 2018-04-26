﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.XFEM.Entities;
using ISAAR.MSolve.XFEM.Entities.FreedomDegrees;

namespace ISAAR.MSolve.XFEM.Solvers
{
    interface ISolver
    {
        IDOFEnumerator DOFEnumerator { get; }
        SolverLogger Logger { get; }
        Vector Solution { get; }

        void Initialize(Model2D model);
        void Solve();
    }
}