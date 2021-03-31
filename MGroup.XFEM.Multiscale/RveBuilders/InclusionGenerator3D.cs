using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using MGroup.XFEM.Geometry.Primitives;

namespace MGroup.XFEM.Multiscale
{
    /// <summary>
    /// Generates spherical inclusions in a 3D box domain. Inclusions will not intersect the domain boundary and will not 
    /// collide with each other.
    /// </summary>
    public class InclusionGenerator3D
    {
        /// <summary>
        /// Minimum coordinates of the box-shaped domain
        /// </summary>
        public double[] CoordsMin { get; set; }

        /// <summary>
        /// Maximum coordinates of the box-shaped domain
        /// </summary>
        public double[] CoordsMax { get; set; }

        /// <summary>
        /// The ratio of the minimum distance between points on the boundaries of 2 inclusions over the average length of the 
        /// domain in all dimensions.
        /// </summary>
        public double InclusionsMinDistanceOverDomainLength { get; set; }

        /// <summary>
        /// Lower limit for the radius of the spherical inclusions.
        /// </summary>
        public double RadiusMin { get; set; }

        /// <summary>
        /// Upper limit for the radius of the spherical inclusions.
        /// </summary>
        public double RadiusMax { get; set; }

        /// <summary>
        /// The seed used for all random operations.
        /// </summary>
        public int Seed { get; set; }

        /// <summary>
        /// The volume fraction of inclusions over total material, that we want to reach.
        /// </summary>
        public double TargetVolumeFraction { get; set; }

        /// <summary>
        /// Tolerance for <see cref="TargetVolumeFraction"/>: the final volume fraction will be in the range
        /// from (1 - <see cref="TargetVolumeFraction"/>) * <see cref="TargetVolumeFraction"/> 
        /// to (1 + <see cref="TargetVolumeFraction"/>) * <see cref="TargetVolumeFraction"/> 
        /// </summary>
        public double TargetVolumeFractionToleranceRatio { get; set; }

        public IList<ISurface3D> CreateInclusions()
        {
            var rng = new Random(Seed);
            var allInclusions = new List<Sphere>();
            double inclusionsMinDistance = CalcMinInclusionDistance();
            double volumeFraction = 0.0;
            double cutoffVolumeFraction = (1.0 - TargetVolumeFractionToleranceRatio) * TargetVolumeFraction;
            double maxVolumeFraction = (1.0 + TargetVolumeFractionToleranceRatio) * TargetVolumeFraction;
            double totalVolume = (CoordsMax[0] - CoordsMin[0]) * (CoordsMax[1] - CoordsMin[1]) * (CoordsMax[2] - CoordsMin[2]);

            while (volumeFraction < cutoffVolumeFraction)
            {
                // Find radius, so that the new inclusion fits without exceeding the target volume fraction
                double remainingVolume = (maxVolumeFraction - volumeFraction) * totalVolume;
                double upperLimit = Sphere.CalcRadiusFromVolume(remainingVolume);
                if (upperLimit <= RadiusMin)
                {
                    // Stop even though the min target v.f. has not been reached, because no more inclusions can be added
                    break; 
                }
                else
                {
                    // Adjust the max allowable radius, so that the max target v.f. is not exceeded. 
                    // This will probably only happen for the last inclusions.
                    upperLimit = Math.Min(upperLimit, RadiusMax);
                }
                double radius = RadiusMin + rng.NextDouble() * (upperLimit - RadiusMin);

                // Find the position of this inclusion, so that it does not collide with other inclusions or the domain boundary
                var center = new double[3];
                var inclusion = new Sphere(center, radius);
                Debug.Write("Trying to fit a new sphere. Attempts: ");
                int numAttempts = 0;
                while (true) //TODO: infinite loops: add a timer/counter and when an upper limit is exceeded, decrease the radius and retry
                {
                    ++numAttempts;
                    if (numAttempts % 10 == 0) Debug.Write($"{numAttempts} ");
                    //Debug.Write(".");
                    for (int d = 0; d < 3; ++d)
                    {
                        //TODO: Should I let it create a random number and then clamp it between min and max, if necessary?
                        double min = CoordsMin[d] + radius;
                        double max = CoordsMax[d] - radius;
                        inclusion.Center[d] = min + rng.NextDouble() * (max - min);
                    }

                    bool collision = CollidesWithOtherInclusions(inclusion, allInclusions, inclusionsMinDistance);
                    if (!collision)
                    {
                        Debug.WriteLine($"Success at attempt {numAttempts}");
                        break;
                    }
                }

                volumeFraction += inclusion.Volume() / totalVolume;
                allInclusions.Add(inclusion);
            }
            return allInclusions.ToList<ISurface3D>();
        }

        private bool CollidesWithOtherInclusions(Sphere newInclusion, List<Sphere> currentInclusions, 
            double inclusionsMinDistance)
        {
            for (int i = 0; i < currentInclusions.Count; ++i)
            {
                double centerDistance = XFEM.Geometry.Utilities.Distance3D(newInclusion.Center, currentInclusions[i].Center);
                if (centerDistance <= newInclusion.Radius + inclusionsMinDistance + currentInclusions[i].Radius)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Takes the requested ration of the average domain length in all dimensions
        /// </summary>
        /// <returns></returns>
        private double CalcMinInclusionDistance()
        {
            int dim = CoordsMin.Length;
            double avgDomainLength = 0.0;
            for (int d = 0; d < dim; ++d)
            {
                avgDomainLength += CoordsMax[d] - CoordsMin[d];
            }
            avgDomainLength /= dim;
            return InclusionsMinDistanceOverDomainLength * avgDomainLength; 
        }
    }
}
