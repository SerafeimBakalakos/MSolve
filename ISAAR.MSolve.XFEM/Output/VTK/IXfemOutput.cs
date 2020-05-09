using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.XFEM_OLD.FreedomDegrees.Ordering;
using System;
using System.Collections.Generic;
using System.Text;

namespace ISAAR.MSolve.XFEM_OLD.Output.VTK
{
    interface IXfemOutput
    {
        void WriteOutputData(IDofOrderer dofOrderer, Vector freeDisplacements, Vector constrainedDisplacements, int step);
    }
}
