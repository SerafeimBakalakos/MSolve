using System;
using System.Collections.Generic;
using System.Text;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Entities;
using MGroup.XFEM.Geometry.LSM;
using MGroup.XFEM.Geometry.Primitives;

namespace MGroup.XFEM.Tests.EpoxyAg
{
    public class GeometryPreprocessor2D
    {
        public double[] MinCoordinates { get; set; } = { -1.0, -1.0};
        public double[] MaxCoordinates { get; set; } = { +1.0, +1.0 };

        public double Thickness { get; set; } = 1.0;

        public int RngSeed { get; set; } = 33;

        public int NumBalls { get; set; } = 20;

        public double RadiusEpoxyPhase { get; set; } = 0.2;

        public double ThicknessSilverPhase { get; set; } = 0.05;

        public PhaseGeometryModel_OLD GeometricModel { get; set; }

        public string MatrixPhaseName { get; } = "matrix";

        public int MatrixPhaseID { get; set; }

        public string EpoxyPhaseName { get; } = "epoxy";

        public List<int> EpoxyPhaseIDs { get; set; } = new List<int>();

        public string SilverPhaseName { get; } = "silver";

        public List<int> SilverPhaseIDs { get; set; } = new List<int>();

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

        public void GeneratePhases(XModel<IXMultiphaseElement> physicalModel)
        {
            GeometricModel = new PhaseGeometryModel_OLD(2, physicalModel);
            var defaultPhase = new DefaultPhase(0);
            GeometricModel.Phases.Add(defaultPhase);
            MatrixPhaseID = 0;

            double margin = ThicknessSilverPhase;
            var minCoordsExtended = new double[2];
            minCoordsExtended[0] = MinCoordinates[0] - margin;
            minCoordsExtended[1] = MinCoordinates[1] - margin;
            var maxCoordsExtended = new double[2];
            maxCoordsExtended[0] = MaxCoordinates[0] + margin;
            maxCoordsExtended[1] = MaxCoordinates[1] + margin;

            var ballsInternal = new List<Circle2D>();
            var ballsExternal = new List<Circle2D>();

            int b = 0;
            var rng = new Random(RngSeed);
            while (b < NumBalls)
            {
                var newCenter = new double[2];
                newCenter[0] = rng.NextDouble() * (maxCoordsExtended[0] - minCoordsExtended[0]) + minCoordsExtended[0];
                newCenter[1] = rng.NextDouble() * (maxCoordsExtended[1] - minCoordsExtended[1]) + minCoordsExtended[1];
                var newBallInternal = new Circle2D(newCenter[0], newCenter[1], RadiusEpoxyPhase);
                var newBallExternal = new Circle2D(newCenter[0], newCenter[1], RadiusEpoxyPhase + ThicknessSilverPhase);
                
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
                var lsmExternal = new SimpleLsm2D(phaseExternal.ID, physicalModel.XNodes, newBallExternal);
                var boundaryExternal = new ClosedLsmPhaseBoundary(phaseExternal.ID, lsmExternal, defaultPhase, phaseExternal);
                defaultPhase.ExternalBoundaries.Add(boundaryExternal);
                defaultPhase.Neighbors.Add(phaseExternal);
                phaseExternal.ExternalBoundaries.Add(boundaryExternal);
                phaseExternal.Neighbors.Add(defaultPhase);

                var lsmInternal = new SimpleLsm2D(phaseInternal.ID, physicalModel.XNodes, newBallInternal);
                var boundaryInternal = new ClosedLsmPhaseBoundary(phaseInternal.ID, lsmInternal, phaseExternal, phaseInternal);
                phaseExternal.InternalBoundaries.Add(boundaryInternal);
                phaseExternal.Neighbors.Add(phaseInternal);
                phaseExternal.InternalPhases.Add(phaseInternal);
                phaseInternal.ExternalBoundaries.Add(boundaryInternal);
                phaseInternal.Neighbors.Add(phaseExternal);

                ++b;
            }

        }

        private bool CollidesWithOtherBalls(Circle2D newBallInternal, Circle2D newBallExternal, 
            List<Circle2D> ballsInternal, List<Circle2D> ballsExternal)
        {
            for (int i = 0; i < ballsInternal.Count; ++i)
            {
                double centerDistance = XFEM.Geometry.Utilities.Distance2D(newBallInternal.Center, ballsInternal[i].Center);
                if (newBallExternal.Radius + ballsInternal[i].Radius >= centerDistance) return true;
                if (newBallInternal.Radius + ballsExternal[i].Radius >= centerDistance) return true;
            }
            return false;
        }
    }
}
