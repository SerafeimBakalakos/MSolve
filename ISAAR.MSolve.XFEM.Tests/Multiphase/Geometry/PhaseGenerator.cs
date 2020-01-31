using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.Geometry.Shapes;
using ISAAR.MSolve.XFEM.Multiphase.Entities;
using ISAAR.MSolve.XFEM.Multiphase.Geometry;

namespace ISAAR.MSolve.XFEM.Tests.Multiphase.Geometry
{
    public class PhaseGenerator
    {
        private const double thickness = 1.0;

        private readonly double minX, minY, maxX, maxY;
        private readonly int numElementsPerAxis;

        public PhaseGenerator(double minX, double maxX, int numElementsPerAxis)
        {
            this.minX = minX;
            this.minY = minX;
            this.maxX = maxX;
            this.maxY = maxX;
            this.numElementsPerAxis = numElementsPerAxis;
        }

        public GeometricModel CreateScatterRectangularPhases(XModel physicalModel)
        {
            // Generate rectangles
            List<Rectangle2D> rectangles = ScatterDisjointRects();

            // Create phases out of them
            var geometricModel = new GeometricModel(physicalModel);
            double elementSize = (maxX - minX) / numElementsPerAxis;
            geometricModel.MeshTolerance = new UsedDefinedMeshTolerance(elementSize);
            var defaultPhase = new DefaultPhase();
            geometricModel.Phases.Add(defaultPhase);
            int phaseID = 1;
            foreach (Rectangle2D rect in rectangles)
            {
                var phase = new ConvexPhase(phaseID++);
                geometricModel.Phases.Add(phase);
                for (int i = 0; i < 4; ++i)
                {
                    CartesianPoint start = rect.Vertices[i];
                    CartesianPoint end = rect.Vertices[(i + 1) % 4];
                    var segment = new XFEM.Multiphase.Geometry.LineSegment2D(start, end);
                    var boundary = new PhaseBoundary(segment);
                    phase.Boundaries.Add(boundary);
                    boundary.PositivePhase = phase; // The vertices are in anti-clockwise order
                    boundary.NegativePhase = defaultPhase;
                }
            }
            geometricModel.AssossiatePhasesNodes();
            geometricModel.AssociatePhasesElements();
            return geometricModel;
        }

        public GeometricModel CreateSingleTetrisPhases(XModel physicalModel)
        {
            // Generate rectangles by rotating the following shapes
            //
            //  G-----------H
            //  |           |
            //  C-----D-----E-----F
            //        |           |
            //        A-----------B
            //
            double rectLength = 0.4, rectHeight = rectLength / 4.0;
            CartesianPoint A = Tranform(0.5 * rectLength, 0.0);
            CartesianPoint B = Tranform(1.5 * rectLength, 0.0);
            CartesianPoint C = Tranform(0.0, rectHeight);
            CartesianPoint D = Tranform(0.5 * rectLength, rectHeight);
            CartesianPoint E = Tranform(rectLength, rectHeight);
            CartesianPoint F = Tranform(1.5 * rectLength, rectHeight);
            CartesianPoint G = Tranform(0.0, 2.0 * rectHeight);
            CartesianPoint H = Tranform(rectLength, 2.0 * rectHeight);

            var phase0 = new DefaultPhase();
            var phase1 = new ConvexPhase(1);
            var phase2 = new ConvexPhase(2);

            // Create boundaries and associate them with their phases
            var AB = new PhaseBoundary(new XFEM.Multiphase.Geometry.LineSegment2D(A, B));
            AB.PositivePhase = phase1;
            AB.NegativePhase = phase0;
            var CD = new PhaseBoundary(new XFEM.Multiphase.Geometry.LineSegment2D(C, D));
            CD.PositivePhase = phase2;
            CD.NegativePhase = phase0;
            var DE = new PhaseBoundary(new XFEM.Multiphase.Geometry.LineSegment2D(D, E));
            DE.PositivePhase = phase2;
            DE.NegativePhase = phase1;
            var EF = new PhaseBoundary(new XFEM.Multiphase.Geometry.LineSegment2D(E, F));
            EF.PositivePhase = phase0;
            EF.NegativePhase = phase1; 
            var GH = new PhaseBoundary(new XFEM.Multiphase.Geometry.LineSegment2D(G, H));
            GH.PositivePhase = phase0;
            GH.NegativePhase = phase2; 
            var AD = new PhaseBoundary(new XFEM.Multiphase.Geometry.LineSegment2D(A, D));
            AD.PositivePhase = phase0;
            AD.NegativePhase = phase1;
            var BF = new PhaseBoundary(new XFEM.Multiphase.Geometry.LineSegment2D(B, F));
            BF.PositivePhase = phase1;
            BF.NegativePhase = phase0;
            var CG = new PhaseBoundary(new XFEM.Multiphase.Geometry.LineSegment2D(C, G));
            CG.PositivePhase = phase0;
            CG.NegativePhase = phase2;
            var EH = new PhaseBoundary(new XFEM.Multiphase.Geometry.LineSegment2D(E, H));
            EH.PositivePhase = phase2;
            EH.NegativePhase = phase0;

            // Associate each phase with its boundaries
            phase1.Boundaries.Add(AB);
            phase1.Boundaries.Add(DE);
            phase1.Boundaries.Add(EF);
            phase1.Boundaries.Add(AD);
            phase1.Boundaries.Add(BF);

            phase2.Boundaries.Add(CD);
            phase2.Boundaries.Add(DE);
            phase2.Boundaries.Add(GH);
            phase2.Boundaries.Add(CG);
            phase2.Boundaries.Add(EH);

            // Initialize model
            var geometricModel = new GeometricModel(physicalModel);
            double elementSize = (maxX - minX) / numElementsPerAxis;
            geometricModel.MeshTolerance = new UsedDefinedMeshTolerance(elementSize);
            geometricModel.Phases.Add(phase0);
            geometricModel.Phases.Add(phase1);
            geometricModel.Phases.Add(phase2);

            geometricModel.AssossiatePhasesNodes();
            geometricModel.AssociatePhasesElements();
            return geometricModel;
        }

        private CartesianPoint Tranform(double x, double y)
        {
            double offsetX = 0.0555, offsetY = 0.0555, phi = Math.PI / 6.0;

            double r = Math.Sqrt(x * x + y * y);
            double theta = Math.Atan2(y, x);
            double newX = offsetX + r * Math.Cos(phi + theta);
            double newY = offsetY + r * Math.Sin(phi + theta);
            return new CartesianPoint(newX, newY);
        }

        private List<Rectangle2D> ScatterDisjointRects()
        {
            int numRects = 25;
            double rectLength = 0.4, rectHeight = rectLength / 10;
            bool rectsCannotInteract = true;

            int seed = 25;
            var rng = new Random(seed);
            var rectanges = new List<Rectangle2D>();
            rectanges.Add(GenerateRandomRectangle(rng, rectLength, rectHeight));
            for (int i = 1; i < numRects; ++i)
            {
                //Console.WriteLine("Trying new Rect");
                Rectangle2D newRect = null;
                do
                {
                    newRect = GenerateRandomRectangle(rng, rectLength, rectHeight);
                }
                while (rectsCannotInteract && InteractsWithOtherRects(newRect, rectanges));
                rectanges.Add(newRect);
            }
            return rectanges;
        }

        private Rectangle2D GenerateRandomRectangle(Random rng, double rectLength, double rectHeight)
        {
            //double lbX = minX + 0.5 * rectLength, ubX = maxX - 0.5 * rectLength;
            //double lbY = minY + 0.5 * rectLength, ubY = maxY - 0.5 * rectLength;
            double lbX = minX, ubX = maxX;
            double lbY = minY, ubY = maxY;

            double centroidX = lbX + (ubX - lbX) * rng.NextDouble();
            double centroidY = lbY + (ubY - lbY) * rng.NextDouble();
            double angle = Math.PI * rng.NextDouble();
            return new Rectangle2D(new CartesianPoint(centroidX, centroidY), rectLength, rectHeight, angle);
        }

        private static bool InteractsWithOtherRects(Rectangle2D newRect, List<Rectangle2D> currentRects)
        {
            var scaledRect = ScaleRectangle(newRect);
            foreach (Rectangle2D rect in currentRects)
            {
                if (!ScaleRectangle(rect).IsDisjointFrom(scaledRect))
                {
                    //Console.WriteLine("It interacts with an existing one");
                    return true;
                }
            }
            return false;
        }

        private static Rectangle2D ScaleRectangle(Rectangle2D rectangle)
        {
            double scaleFactor = 1.2;
            return new Rectangle2D(rectangle.Centroid,
                scaleFactor * rectangle.LengthAxis0, scaleFactor * rectangle.LengthAxis1, rectangle.Axis0Angle);
        }
    }
}
