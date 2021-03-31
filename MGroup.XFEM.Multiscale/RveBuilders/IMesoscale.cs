using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.Materials;
using ISAAR.MSolve.Materials.Interfaces;
using ISAAR.MSolve.Solvers;

namespace MGroup.XFEM.Multiscale
{
    public interface IMesoscale
    {
        #region input 
        int Seed { get; set; }

        double VolumeFraction { get; set; }

        IContinuumMaterial MatrixMaterial { get; set; }

        ElasticMaterial3D InclusionMaterial { get; set; }

        ISolverBuilder SolverBuilder { get; set; }
        #endregion

        #region output
        IList<double[]> Strains { get; set; }

        IList<double[]> Stresses { get; set; }

        IList<IMatrix> ElasticityTensors { get; set; }
        #endregion

        void RunAnalysis();
    }
}
