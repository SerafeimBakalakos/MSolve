using System;
using System.Collections.Generic;
using System.Text;

namespace MGroup.XFEM.Heat
{
    public class Simulation
    {
        #region parameters
        public int NumCNTs { get; set; }

        public int[] DimensionsSVE { get; set; }

        public double InterfaceConductivity { get; set; }
        #endregion


        #region other input
        public int Seed { get; set; } = 13;

        public double EffectiveMatrixConductivity { get; set; } = 5;

        public double CntConductivity { get; } = 500;

        public double CntLength { get; set; } = 10;

        public double CntDiameter { get; set; } = 0.168;

        public double EffectiveDensity { get; set; } // functions of CNTs

        public double EffectiveThermalCapacity { get; set; }

        #endregion

        #region output
        public double[,] EffectiveConductivity { get; set; }
        #endregion

        public void Run()
        {
            var composite = new CntsMatrixModel();
            composite.CoordsMin = new double[] { -1.0, -1.0, -1.0 };
            composite.CoordsMax = new double[] { +1.0, +1.0, +1.0 };
            composite.ConductivityMatrix = EffectiveMatrixConductivity;
            composite.ConductivityCNT = CntConductivity;
            composite.ConductivityInterface = InterfaceConductivity;

            composite.BuildModel();
            composite.PlotModel();
        }
    }
}
