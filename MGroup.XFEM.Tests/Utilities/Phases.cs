using System;
using System.Collections.Generic;
using System.Text;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Enrichment.Enrichers;
using MGroup.XFEM.Entities;
using MGroup.XFEM.Geometry.LSM;
using MGroup.XFEM.Geometry.Primitives;
using MGroup.XFEM.Phases;

namespace MGroup.XFEM.Tests.Utilities
{
    public static class Phases
    {
        public static List<ICurve2D> CreateBallsStructured2D(double[] minCoords, double[] maxCoords, int[] numBalls,
            double radius, double distanceFromSidesOverDistanceBetween = 1.0)
        {
            double xMin = minCoords[0], xMax = maxCoords[0], yMin = minCoords[1], yMax = maxCoords[1];
            var curves = new List<ICurve2D>(numBalls[0] * numBalls[1]);
            double dx = (xMax - xMin) / (numBalls[0] - 1 + 2 * distanceFromSidesOverDistanceBetween);
            double dy = (yMax - yMin) / (numBalls[1] - 1 + 2 * distanceFromSidesOverDistanceBetween);
            for (int i = 0; i < numBalls[0]; ++i)
            {
                double centerX = xMin + distanceFromSidesOverDistanceBetween * dx + i * dx;
                for (int j = 0; j < numBalls[1]; ++j)
                {
                    double centerY = yMin + distanceFromSidesOverDistanceBetween * dy + j * dy;
                    var circle = new Circle2D(centerX, centerY, radius);
                    curves.Add(circle);
                }
            }

            return curves;
        }

        public static List<ISurface3D> CreateBallsStructured3D(double[] minCoords, double[] maxCoords, int[] numBalls,
            double radius, double distanceFromSidesOverDistanceBetween = 1.0)
        {
            double xMin = minCoords[0], xMax = maxCoords[0];
            double yMin = minCoords[1], yMax = maxCoords[1];
            double zMin = minCoords[2], zMax = maxCoords[2];

            var balls = new List<ISurface3D>(numBalls[0] * numBalls[1] * numBalls[2]);
            double dx = (xMax - xMin) / (numBalls[0] - 1 + 2 * distanceFromSidesOverDistanceBetween);
            double dy = (yMax - yMin) / (numBalls[1] - 1 + 2 * distanceFromSidesOverDistanceBetween);
            double dz = (zMax - yMin) / (numBalls[2] - 1 + 2 * distanceFromSidesOverDistanceBetween);
            for (int i = 0; i < numBalls[0]; ++i)
            {
                double centerX = xMin + distanceFromSidesOverDistanceBetween * dx + i * dx;
                for (int j = 0; j < numBalls[1]; ++j)
                {
                    double centerY = yMin + distanceFromSidesOverDistanceBetween * dy + j * dy;
                    for (int k = 0; k < numBalls[2]; ++k)
                    {
                        double centerZ = zMin + distanceFromSidesOverDistanceBetween * dz + k * dz;
                        var sphere = new Sphere(centerX, centerY, centerZ, radius);
                        balls.Add(sphere);
                    }
                }
            }

            return balls;
        }

        public static PhaseGeometryModel CreateLsmPhases2D(XModel<IXMultiphaseElement> model,
            List<ICurve2D> inclusionGeometries, Func<PhaseGeometryModel, INodeEnricher> createNodeEnricher)
        {
            var geometricModel = new PhaseGeometryModel(model);
            model.GeometryModel = geometricModel;
            geometricModel.Enricher = createNodeEnricher(geometricModel);
            var defaultPhase = new DefaultPhase();
            geometricModel.Phases[defaultPhase.ID] = defaultPhase;
            for (int p = 0; p < inclusionGeometries.Count; ++p)
            {
                var lsm = new SimpleLsm2D(p + 1, model.XNodes, inclusionGeometries[p]);
                var phase = new LsmPhase(p + 1, geometricModel, -1);
                geometricModel.Phases[phase.ID] = phase;

                var boundary = new ClosedPhaseBoundary(phase.ID, lsm, defaultPhase, phase);
                defaultPhase.ExternalBoundaries.Add(boundary);
                defaultPhase.Neighbors.Add(phase);
                phase.ExternalBoundaries.Add(boundary);
                phase.Neighbors.Add(defaultPhase);
                geometricModel.PhaseBoundaries[boundary.ID] = boundary;
            }
            return geometricModel;
        }

        public static PhaseGeometryModel CreateLsmPhases3D(XModel<IXMultiphaseElement> model,
            List<ISurface3D> inclusionGeometries, Func<PhaseGeometryModel, INodeEnricher> createNodeEnricher)
        {
            var geometricModel = new PhaseGeometryModel(model);
            model.GeometryModel = geometricModel;
            geometricModel.Enricher = createNodeEnricher(geometricModel);
            var defaultPhase = new DefaultPhase();
            geometricModel.Phases[defaultPhase.ID] = defaultPhase;
            for (int p = 0; p < inclusionGeometries.Count; ++p)
            {
                var lsm = new SimpleLsm3D(p + 1, model.XNodes, inclusionGeometries[p]);
                var phase = new LsmPhase(p + 1, geometricModel, -1);
                geometricModel.Phases[phase.ID] = phase;

                var boundary = new ClosedPhaseBoundary(phase.ID, lsm, defaultPhase, phase);
                defaultPhase.ExternalBoundaries.Add(boundary);
                defaultPhase.Neighbors.Add(phase);
                phase.ExternalBoundaries.Add(boundary);
                phase.Neighbors.Add(defaultPhase);
                geometricModel.PhaseBoundaries[boundary.ID] = boundary;
            }
            return geometricModel;
        }
    }
}
