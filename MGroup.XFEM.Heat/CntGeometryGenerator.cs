using System;
using System.Collections.Generic;
using System.Text;
using MGroup.XFEM.Geometry.Primitives;

namespace MGroup.XFEM.Heat
{
    public class CntGeometryGenerator : ICntGeometryGenerator
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

            var outerBoxMin = new double[3];
            var outerBoxMax = new double[3];
            for (int d = 0; d < 3; ++d)
            {
                outerBoxMin[d] = CoordsMin[d] - 0.5 * CntLength;
                outerBoxMax[d] = CoordsMax[d] + 0.5 * CntLength;
            }

            var inclusions = new List<ISurface3D>();
            for (int i = 0; i <NumCNTs; ++i) 
            {
                inclusions.Add(CreateCylinder(outerBoxMin, outerBoxMax, rng));
            }

            return inclusions;
        }

        public Cylinder3D CreateCylinder(double[] outerBoxMin, double[] outerBoxMax, Random rng)
        {
            while (true)
            {
                double[] axisStart = new double[3];
                for (int d = 0; d < 3; ++d)
                {
                    axisStart[d] = outerBoxMin[d] + rng.NextDouble() * (outerBoxMax[d] - outerBoxMin[d]);
                }

                double theta = rng.NextDouble() * Math.PI;
                double phi = rng.NextDouble() * 2 * Math.PI;

                double[] axisEnd =
                {
                    axisStart[0] + CntLength * Math.Cos(phi) * Math.Sin(theta),
                    axisStart[1] + CntLength * Math.Sin(phi) * Math.Sin(theta),
                    axisStart[2] + CntLength * Math.Cos(theta)
                };

                var cylinder = new Cylinder3D(axisStart, axisEnd, CntRadius);
                if (IsInsideDomain(cylinder))
                {
                    return cylinder;
                }
            }
        }

        private bool IsInsideDomain(Cylinder3D cylinder)
        {
            //TODO: Also take into account the radius: the axis could be just outside and parallel to a face, so that parts of 
            //      the cylinder are inside.   
            bool axisStartInside = true;
            for (int d = 0; d < 3; ++d)
            {
                axisStartInside &= (CoordsMin[d] < cylinder.Start[d]) && (cylinder.Start[d] < CoordsMax[d]);
            }


            bool axisEndInside = true;
            for (int d = 0; d < 3; ++d)
            {
                axisEndInside &= (CoordsMin[d] < cylinder.End[d]) && (cylinder.End[d] < CoordsMax[d]);
            }

            return axisStartInside || axisEndInside;
        }
    }
}
