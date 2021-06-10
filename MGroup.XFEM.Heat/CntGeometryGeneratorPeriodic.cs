using System;
using System.Collections.Generic;
using System.Text;
using MGroup.XFEM.Geometry.Primitives;

namespace MGroup.XFEM.Heat
{
    public class CntGeometryGeneratorPeriodic : ICntGeometryGenerator
    {
        public double[] CoordsMin { get; set; }

        public double[] CoordsMax { get; set; }

        public double CntLength { get; set; }

        public double CntRadius { get; set; }

        public int NumCNTs { get; set; }

        public int RngSeed { get; set; }
        
        public IEnumerable<ISurface3D> GenerateInclusions()
        {
            var rng = new Random(RngSeed);
            var domain = new CubeDomain(CoordsMin, CoordsMax);

            var innerBoxMin = new double[3];
            var innerBoxMax = new double[3];
            for (int d = 0; d < 3; ++d)
            {
                innerBoxMin[d] = CoordsMin[d] + 0.1 * CntLength;
                innerBoxMax[d] = CoordsMax[d] - 0.1 * CntLength;
            }

            var inclusions = new List<ISurface3D>();
            for (int i = 0; i < NumCNTs; ++i) 
            {
                IList<Cylinder3D> cylinders = CreateCylinders(rng, domain, innerBoxMin, innerBoxMax);
                inclusions.AddRange(cylinders);
            }

            return inclusions;
        }

        public IList<Cylinder3D> CreateCylinders(Random rng, CubeDomain domain, double[] innerBoxMin, double[] innerBoxMax)
        {
            double[] axisStart = new double[3]; // This point will always lie inside the domain
            for (int d = 0; d < 3; ++d)
            {
                axisStart[d] = innerBoxMin[d] + rng.NextDouble() * (innerBoxMax[d] - innerBoxMin[d]);
            }

            //double theta = Math.PI / 2;
            //double phi = 0;
            double theta = rng.NextDouble() * Math.PI;      // 0 <= theta <= pi 
            double phi = rng.NextDouble() * 2 * Math.PI;    // 0 <= phi < 2*pi

            double[] axisEnd =
            {
                axisStart[0] + CntLength * Math.Cos(phi) * Math.Sin(theta),
                axisStart[1] + CntLength * Math.Sin(phi) * Math.Sin(theta),
                axisStart[2] + CntLength * Math.Cos(theta)
            }; // This point may lie outside the domain

            var trialCylinder = new Cylinder3D(axisStart, axisEnd, CntRadius);
            IList<Cylinder3D> periodicCylinders = domain.SplitCylinderPeriodicallyIfOutside(trialCylinder);
            return periodicCylinders;
        }
    }
}
