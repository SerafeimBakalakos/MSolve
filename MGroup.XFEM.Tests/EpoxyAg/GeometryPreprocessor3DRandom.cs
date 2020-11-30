using System;
using System.Collections.Generic;
using System.Text;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Entities;
using MGroup.XFEM.Geometry.LSM;
using MGroup.XFEM.Geometry.Primitives;
using Troschuetz.Random.Distributions.Continuous;

namespace MGroup.XFEM.Tests.EpoxyAg
{
    public class GeometryPreprocessor3DRandom
    {
        public double[] MinCoordinates { get; set; } = { -1.0, -1.0, -1.0 };
        public double[] MaxCoordinates { get; set; } = { +1.0, +1.0, +1.0 };

        public int RngSeed { get; set; } = 33;

        public int NumBalls { get; set; } = 20;

        public PhaseGeometryModel_OLD GeometricModel { get; set; }

        public string MatrixPhaseName { get; } = "matrix";

        public int MatrixPhaseID { get; set; }

        public string EpoxyPhaseName { get; } = "epoxy";

        public List<int> EpoxyPhaseIDs { get; set; } = new List<int>();

        public string SilverPhaseName { get; } = "silver";

        public List<int> SilverPhaseIDs { get; set; } = new List<int>();

        public void GeneratePhases(XModel<IXMultiphaseElement> physicalModel)
        {
            GeometricModel = new PhaseGeometryModel_OLD(3, physicalModel);
            var defaultPhase = new DefaultPhase(0);
            GeometricModel.Phases.Add(defaultPhase);
            MatrixPhaseID = 0;

            double margin = 0.0;
            //double margin = 0.01 * (MaxCoordinates[0] - MinCoordinates[0]);
            var minCoordsExtended = new double[3];
            minCoordsExtended[0] = MinCoordinates[0] - margin;
            minCoordsExtended[1] = MinCoordinates[1] - margin;
            minCoordsExtended[2] = MinCoordinates[2] - margin;
            var maxCoordsExtended = new double[3];
            maxCoordsExtended[0] = MaxCoordinates[0] + margin;
            maxCoordsExtended[1] = MaxCoordinates[1] + margin;
            maxCoordsExtended[2] = MaxCoordinates[2] + margin;

            var ballsInternal = new List<Sphere>();
            var ballsExternal = new List<Sphere>();

            var radiiOfEpoxyDistributionNormal = new NormalDistribution(27644437, 0, 1);
            double densitySilver = 10.49;
            double densityEpoxy = 1.2;
            double weightFraction = 0.3;
            //double weightFraction = 0.08;
            int b = 0;
            var rng = new Random(RngSeed);
            while (b < NumBalls)
            {
                var newCenter = new double[3];
                newCenter[0] = rng.NextDouble() * (maxCoordsExtended[0] - minCoordsExtended[0]) + minCoordsExtended[0];
                newCenter[1] = rng.NextDouble() * (maxCoordsExtended[1] - minCoordsExtended[1]) + minCoordsExtended[1];
                newCenter[2] = rng.NextDouble() * (maxCoordsExtended[2] - minCoordsExtended[2]) + minCoordsExtended[2];

                double radiusEpoxyPhase = 0.5 * Math.Exp(6.6032 + 0.2462 * radiiOfEpoxyDistributionNormal.NextDouble());

                // This is the correct one
                //double radiusExternal = Math.Pow(1 + densityEpoxy / densitySilver * weightFraction, 1.0 / 3) * radiusEpoxyPhase;
                // Hack to test code
                #region debug
                //double radiusExternal = Math.Pow(1 + densityEpoxy / densitySilver * weightFraction, 1.0 / 3) * radiusEpoxyPhase + 100;
                //double radiusExternal = radiusEpoxyPhase + 10;
                double radiusExternal = radiusEpoxyPhase + 15;
                #endregion

                var newBallInternal = new Sphere(newCenter[0], newCenter[1], newCenter[2], radiusEpoxyPhase);
                var newBallExternal = new Sphere(newCenter[0], newCenter[1], newCenter[2], radiusExternal);

                if (CollidesWithOtherBalls(newBallInternal, newBallExternal, ballsInternal, ballsExternal)) continue;
                ballsInternal.Add(newBallInternal);
                ballsExternal.Add(newBallExternal);

                // Create phases
                var phaseInternal = new LsmPhase(GeometricModel.Phases.Count, GeometricModel, -1);
                GeometricModel.Phases.Add(phaseInternal);
                EpoxyPhaseIDs.Add(phaseInternal.ID);
                var phaseExternal = new HollowLsmPhase(GeometricModel.Phases.Count, GeometricModel, 0);
                GeometricModel.Phases.Add(phaseExternal);
                SilverPhaseIDs.Add(phaseExternal.ID);

                // Create phase boundaries
                var lsmExternal = new SimpleLsm3D(phaseExternal.ID, physicalModel.XNodes, newBallExternal);
                var boundaryExternal = new PhaseBoundary(phaseExternal.ID, lsmExternal, defaultPhase, phaseExternal);
                defaultPhase.ExternalBoundaries.Add(boundaryExternal);
                defaultPhase.Neighbors.Add(phaseExternal);
                phaseExternal.ExternalBoundaries.Add(boundaryExternal);
                phaseExternal.Neighbors.Add(defaultPhase);

                var lsmInternal = new SimpleLsm3D(phaseInternal.ID, physicalModel.XNodes, newBallInternal);
                var boundaryInternal = new PhaseBoundary(phaseInternal.ID, lsmInternal, phaseExternal, phaseInternal);
                phaseExternal.InternalBoundaries.Add(boundaryInternal);
                phaseExternal.Neighbors.Add(phaseInternal);
                phaseExternal.InternalPhases.Add(phaseInternal);
                phaseInternal.ExternalBoundaries.Add(boundaryInternal);
                phaseInternal.Neighbors.Add(phaseExternal);

                ++b;
            }
        }

        public Dictionary<string, double> CalcPhaseVolumes()
        {
            var volumes = new Dictionary<string, double>();
            Dictionary<int, double> phaseVolumes = GeometricModel.CalcBulkSizeOfEachPhase();

            volumes[MatrixPhaseName] = phaseVolumes[MatrixPhaseID];

            volumes[EpoxyPhaseName] = 0;
            foreach (int phaseID in EpoxyPhaseIDs) volumes[EpoxyPhaseName] += phaseVolumes[phaseID];

            volumes[SilverPhaseName] = 0;
            foreach (int phaseID in SilverPhaseIDs)
            {
                try
                {
                    volumes[SilverPhaseName] += phaseVolumes[phaseID];
                }
                catch (KeyNotFoundException)
                {
                    // This phase has been merged into another one. Nothing more to do here.
                }
            }

            return volumes;
        }

        private bool CollidesWithOtherBalls(Sphere newBallInternal, Sphere newBallExternal,
            List<Sphere> ballsInternal, List<Sphere> ballsExternal)
        {
            for (int i = 0; i < ballsInternal.Count; ++i)
            {
                double centerDistance = XFEM.Geometry.Utilities.Distance3D(newBallInternal.Center, ballsInternal[i].Center);
                if (newBallInternal.Radius + ballsInternal[i].Radius >= centerDistance) return true;
                //if (newBallExternal.Radius + ballsInternal[i].Radius >= centerDistance) return true;
                //if (newBallInternal.Radius + ballsExternal[i].Radius >= centerDistance) return true;
            }
            return false;
        }
    }
	
}
