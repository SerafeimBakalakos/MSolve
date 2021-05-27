using System;
using System.Collections.Generic;
using System.Text;

namespace MGroup.XFEM.Heat
{
    public class Simulation
    {
        #region parameters
        public int NumCNTs { get; set; }

        public double[] DimensionsSVE { get; set; }

        public double InterfaceConductivity { get; set; }
        #endregion


        #region other input
        public int RngSeed { get; set; } = 13;

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
            //TODO: this causes geometric bug
            //var geometryGenerator = new CntGeometryGenerator();
            //geometryGenerator.RngSeed = 0;
            //geometryGenerator.CoordsMin = new double[] { -1.0, -1.0, -1.0 };
            //geometryGenerator.CoordsMax = new double[] { +1.0, +1.0, +1.0 };
            //geometryGenerator.NumCNTs = 6;
            //geometryGenerator.CntLength = 0.5;
            //geometryGenerator.CntRadius = 0.1;

            var geometryGenerator = new CntGeometryGeneratorPeriodic();
            geometryGenerator.RngSeed = 13;
            geometryGenerator.CoordsMin = new double[] { -1.0, -1.0, -1.0 };
            geometryGenerator.CoordsMax = new double[] { +1.0, +1.0, +1.0 };
            geometryGenerator.NumCNTs = 3;
            geometryGenerator.CntLength = 0.8;
            geometryGenerator.CntRadius = 0.1;

            //var geometryGenerator = new CntGeometryGenerator();
            //geometryGenerator.RngSeed = this.RngSeed;
            //geometryGenerator.CoordsMin = new double[] { -0.5 * DimensionsSVE[0], -0.5 * DimensionsSVE[1] , -0.5 * DimensionsSVE[2] };
            //geometryGenerator.CoordsMax = new double[] { +0.5 * DimensionsSVE[0], +0.5 * DimensionsSVE[1] , +0.5 * DimensionsSVE[2] };
            //geometryGenerator.NumCNTs = this.NumCNTs;
            //geometryGenerator.CntLength = this.CntLength;
            //geometryGenerator.CntRadius = 0.5 * this.CntDiameter;

            var composite = new CntsMatrixModel();
            composite.CoordsMin = geometryGenerator.CoordsMin;
            composite.CoordsMax = geometryGenerator.CoordsMax;
            composite.ConductivityMatrix = EffectiveMatrixConductivity;
            composite.ConductivityCNT = CntConductivity;
            composite.ConductivityInterface = InterfaceConductivity;
            composite.GeometryGenerator = geometryGenerator;

            composite.BuildModel();
            composite.PlotModel();
        }
    }
}
