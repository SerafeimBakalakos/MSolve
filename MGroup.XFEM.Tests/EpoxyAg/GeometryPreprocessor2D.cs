using System;
using System.Collections.Generic;
using System.Text;
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

        public GeometricModel GeometricModel { get; set; }

        public List<int> EpoxyPhases { get; set; } = new List<int>();

        public List<int> SilverPhases { get; set; } = new List<int>();

        public void GeneratePhases(XModel physicalModel)
        {
            GeometricModel = new GeometricModel(2, physicalModel);
            var defaultPhase = new DefaultPhase(0);
            GeometricModel.Phases.Add(defaultPhase);
            EpoxyPhases.Add(0);

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
                EpoxyPhases.Add(phaseInternal.ID);
                var phaseExternal = new HollowLsmPhase(GeometricModel.Phases.Count, GeometricModel, 0);
                GeometricModel.Phases.Add(phaseExternal);
                SilverPhases.Add(phaseExternal.ID);

                // Create phase boundaries
                var lsmExternal = new SimpleLsm2D(physicalModel, newBallExternal);
                var boundaryExternal = new PhaseBoundary(lsmExternal, defaultPhase, phaseExternal);
                defaultPhase.ExternalBoundaries.Add(boundaryExternal);
                defaultPhase.Neighbors.Add(phaseExternal);
                phaseExternal.ExternalBoundaries.Add(boundaryExternal);
                phaseExternal.Neighbors.Add(defaultPhase);

                var lsmInternal = new SimpleLsm2D(physicalModel, newBallInternal);
                var boundaryInternal = new PhaseBoundary(lsmInternal, phaseExternal, phaseInternal);
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
                double centerDistance = newBallInternal.Center.Distance2D(ballsInternal[i].Center);
                if (newBallExternal.Radius + ballsInternal[i].Radius >= centerDistance) return true;
                if (newBallInternal.Radius + ballsExternal[i].Radius >= centerDistance) return true;
            }
            return false;
        }
    }
}
