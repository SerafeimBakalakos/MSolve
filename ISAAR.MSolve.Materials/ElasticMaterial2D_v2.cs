﻿using System;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Materials.Interfaces;

namespace ISAAR.MSolve.Materials
{
    public class ElasticMaterial2D_v2 : IIsotropicContinuumMaterial2D_v2
    {
        private readonly Vector strains = Vector.CreateZero(3);
        private readonly Vector stresses = Vector.CreateZero(3);
        private Matrix constitutiveMatrix = null;

        public double[] Coordinates { get; set; }
        public double PoissonRatio { get; set; }
        public StressState2D StressState { get; }
        public double YoungModulus { get; set; }

        public ElasticMaterial2D_v2(StressState2D stressState)
        {
            this.StressState = stressState;
        }

        #region IFiniteElementMaterial3D

        public IMatrixView ConstitutiveMatrix
        {
            get
            {
                if (constitutiveMatrix == null) UpdateMaterial(Vector.CreateZero(3));
                return constitutiveMatrix;
            }
        }

        public IVectorView Stresses => stresses;

        public void ClearState()
        {
            throw new NotImplementedException();
        }

        public void ClearStresses()
        {
            throw new NotImplementedException();
        }

        public void SaveState()
        {
            throw new NotImplementedException();
        }

        public void UpdateMaterial(IVectorView strains)
        {
            this.strains.CopyFrom(strains);
            constitutiveMatrix = Matrix.CreateZero(3, 3); //TODO: This should be cached in the constitutive matrix property and used here.
            if (StressState == StressState2D.PlaneStress)
            {
                double aux = YoungModulus / (1 - PoissonRatio * PoissonRatio);
                constitutiveMatrix[0, 0] = aux;
                constitutiveMatrix[1, 1] = aux;
                constitutiveMatrix[0, 1] = PoissonRatio * aux;
                constitutiveMatrix[1, 0] = PoissonRatio * aux;
                constitutiveMatrix[2, 2] = (1 - PoissonRatio) / 2 * aux;
            }
            else
            {
                double aux = YoungModulus / (1 + PoissonRatio) / (1 - 2 * PoissonRatio);
                constitutiveMatrix[0, 0] = aux * (1 - PoissonRatio);
                constitutiveMatrix[1, 1] = aux * (1 - PoissonRatio);
                constitutiveMatrix[0, 1] = PoissonRatio * aux;
                constitutiveMatrix[1, 0] = PoissonRatio * aux;
                constitutiveMatrix[2, 2] = (1 - 2 * PoissonRatio) / 2 * aux;
            }
        }

        #endregion

        #region IFiniteElementMaterial

        public int ID => 1;

        public bool Modified => false;

        public void ResetModified() { }

        #endregion

        #region ICloneable Members
        object ICloneable.Clone() => Clone();

        public ElasticMaterial2D_v2 Clone()
        {
            return new ElasticMaterial2D_v2(StressState)
            {
                PoissonRatio = this.PoissonRatio,
                YoungModulus = this.YoungModulus
            };
        }

        #endregion

    }
}