﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using ISAAR.MSolve.Analyzers.Multiscale;
using ISAAR.MSolve.Discretization.Mesh;
using ISAAR.MSolve.LinearAlgebra;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.Materials;
using ISAAR.MSolve.Materials.Interfaces;
using ISAAR.MSolve.MultiscaleAnalysis;
using ISAAR.MSolve.Problems;
using ISAAR.MSolve.Solvers;
using ISAAR.MSolve.Solvers.Direct;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Enrichment.Enrichers;
using MGroup.XFEM.Entities;
using MGroup.XFEM.Geometry.LSM;
using MGroup.XFEM.Geometry.Mesh;
using MGroup.XFEM.Geometry.Primitives;
using MGroup.XFEM.Integration;
using MGroup.XFEM.Integration.Quadratures;
using MGroup.XFEM.Materials;
using MGroup.XFEM.Multiscale.FEM.RveBuilders;
using MGroup.XFEM.Phases;

namespace MGroup.XFEM.Multiscale.RveBuilders
{
    public class GmshFemMultiphaseMesoscale : IMesoscale
    {
        private const int dimension = 3;

        #region input 
        public double[] CoordsMin { get; set; }
        public double[] CoordsMax { get; set; }

        public IContinuumMaterial MatrixMaterial { get; set; }

        public IContinuumMaterial InclusionMaterial { get; set; }

        public ISolverBuilder SolverBuilder { get; set; } = new SuiteSparseSolver.Builder();

        public string GmshMeshFilePath { get; set; }

        public double[] TotalStrain { get; set; }

        public int NumLoadingIncrements { get; set; } = 10;
        #endregion

        #region output
        public IList<double[]> Strains { get; set; } = new List<double[]>();

        public IList<double[]> Stresses { get; set; } = new List<double[]>();

        public IList<IMatrixView> ConstitutiveMatrices { get; set; } = new List<IMatrixView>();
        #endregion

        public void RunAnalysis()
        {
            var phaseMaterials = new Dictionary<int, IContinuumMaterial>();
            phaseMaterials[1] = MatrixMaterial;
            phaseMaterials[2] = InclusionMaterial;
            var rveBuilder = GmshMultiphaseCoherentRveBuilder.CreateBuilder(
                CoordsMin, CoordsMax, GmshMeshFilePath, phaseMaterials);
            var microstructure = new Microstructure3D(rveBuilder, model => SolverBuilder.BuildSolver(model), false, 1);

            for (int i = 0; i < NumLoadingIncrements; ++i)
            {
                double[] macroStrain = TotalStrain.Scale((i + 1) / (double)NumLoadingIncrements);
                microstructure.UpdateMaterial(macroStrain);

                Strains.Add(macroStrain.Copy());
                Stresses.Add(microstructure.Stresses.Copy());
                ConstitutiveMatrices.Add(microstructure.ConstitutiveMatrix.Copy());

                microstructure.SaveState();
            }
        }

    }
}