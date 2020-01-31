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

        public GeometricModel CreatePercolatedTetrisPhases(XModel physicalModel)
        {
            // Generate rectangles by rotating the following shapes
            // 13----------14  15----------16   
            //  |    (1)    |   |   (3)     |
            //  5-------6---7---8---9--10--11------12
            //          |    (2)    |   |    (4)    |
            //  0       1-----------2   3-----------4
            double rectLength = 0.7, rectHeight = rectLength / 5.0;
            CartesianPoint P1  = TranformPerc(2 * rectLength / 3, 0.0);
            CartesianPoint P2  = TranformPerc(5 * rectLength / 3, 0.0);
            CartesianPoint P3  = TranformPerc(2 * rectLength, 0.0);
            CartesianPoint P4  = TranformPerc(3 * rectLength, 0.0);
            CartesianPoint P5  = TranformPerc(0.0, rectHeight);
            CartesianPoint P6  = TranformPerc(2 * rectLength / 3, rectHeight);
            CartesianPoint P7  = TranformPerc(1 * rectLength, rectHeight);
            CartesianPoint P8  = TranformPerc(4 * rectLength / 3, rectHeight);
            CartesianPoint P9  = TranformPerc(5 * rectLength / 3, rectHeight);
            CartesianPoint P10 = TranformPerc(2 * rectLength, rectHeight);
            CartesianPoint P11 = TranformPerc(7 * rectLength / 3, rectHeight);
            CartesianPoint P12 = TranformPerc(3 * rectLength, rectHeight);
            CartesianPoint P13 = TranformPerc(0.0, 2 * rectHeight);
            CartesianPoint P14 = TranformPerc(1 * rectLength, 2 * rectHeight);
            CartesianPoint P15 = TranformPerc(4 * rectLength / 3, 2 * rectHeight);
            CartesianPoint P16 = TranformPerc(7 * rectLength / 3, 2 * rectHeight);

            var phase0 = new DefaultPhase();
            var phase1 = new ConvexPhase(1);
            var phase2 = new ConvexPhase(2);
            var phase3 = new ConvexPhase(3);
            var phase4 = new ConvexPhase(4);

            // Create boundaries and associate them with their phases
            var L1_2 = new PhaseBoundary(new XFEM.Multiphase.Geometry.LineSegment2D(P1, P2));
            L1_2.PositivePhase = phase2;
            L1_2.NegativePhase = phase0;
            var L3_4 = new PhaseBoundary(new XFEM.Multiphase.Geometry.LineSegment2D(P3, P4));
            L3_4.PositivePhase = phase4;
            L3_4.NegativePhase = phase0;
            var L5_6 = new PhaseBoundary(new XFEM.Multiphase.Geometry.LineSegment2D(P5, P6));
            L5_6.PositivePhase = phase1;
            L5_6.NegativePhase = phase0;
            var L6_7 = new PhaseBoundary(new XFEM.Multiphase.Geometry.LineSegment2D(P6, P7));
            L6_7.PositivePhase = phase1;
            L6_7.NegativePhase = phase2;
            var L7_8 = new PhaseBoundary(new XFEM.Multiphase.Geometry.LineSegment2D(P7, P8));
            L7_8.PositivePhase = phase0;
            L7_8.NegativePhase = phase2;
            var L8_9 = new PhaseBoundary(new XFEM.Multiphase.Geometry.LineSegment2D(P8, P9));
            L8_9.PositivePhase = phase3;
            L8_9.NegativePhase = phase2;
            var L9_10 = new PhaseBoundary(new XFEM.Multiphase.Geometry.LineSegment2D(P9, P10));
            L9_10.PositivePhase = phase3;
            L9_10.NegativePhase = phase0;
            var L10_11 = new PhaseBoundary(new XFEM.Multiphase.Geometry.LineSegment2D(P10, P11));
            L10_11.PositivePhase = phase3;
            L10_11.NegativePhase = phase4;
            var L11_12 = new PhaseBoundary(new XFEM.Multiphase.Geometry.LineSegment2D(P11, P12));
            L11_12.PositivePhase = phase0;
            L11_12.NegativePhase = phase4;
            var L13_14 = new PhaseBoundary(new XFEM.Multiphase.Geometry.LineSegment2D(P13, P14));
            L13_14.PositivePhase = phase0;
            L13_14.NegativePhase = phase1;
            var L15_16 = new PhaseBoundary(new XFEM.Multiphase.Geometry.LineSegment2D(P15, P16));
            L15_16.PositivePhase = phase0;
            L15_16.NegativePhase = phase3;

            var L1_6 = new PhaseBoundary(new XFEM.Multiphase.Geometry.LineSegment2D(P1, P6));
            L1_6.PositivePhase = phase0;
            L1_6.NegativePhase = phase2;
            var L2_9 = new PhaseBoundary(new XFEM.Multiphase.Geometry.LineSegment2D(P2, P9));
            L2_9.PositivePhase = phase2;
            L2_9.NegativePhase = phase0;
            var L3_10 = new PhaseBoundary(new XFEM.Multiphase.Geometry.LineSegment2D(P3, P10));
            L3_10.PositivePhase = phase0;
            L3_10.NegativePhase = phase4;
            var L4_12 = new PhaseBoundary(new XFEM.Multiphase.Geometry.LineSegment2D(P4, P12));
            L4_12.PositivePhase = phase4;
            L4_12.NegativePhase = phase0;
            var L5_13 = new PhaseBoundary(new XFEM.Multiphase.Geometry.LineSegment2D(P5, P13));
            L5_13.PositivePhase = phase0;
            L5_13.NegativePhase = phase1;
            var L7_14 = new PhaseBoundary(new XFEM.Multiphase.Geometry.LineSegment2D(P7, P14));
            L7_14.PositivePhase = phase1;
            L7_14.NegativePhase = phase0;
            var L8_15 = new PhaseBoundary(new XFEM.Multiphase.Geometry.LineSegment2D(P8, P15));
            L8_15.PositivePhase = phase0;
            L8_15.NegativePhase = phase3;
            var L11_16 = new PhaseBoundary(new XFEM.Multiphase.Geometry.LineSegment2D(P11, P16));
            L11_16.PositivePhase = phase3;
            L11_16.NegativePhase = phase0;

            // Associate each phase with its boundaries
            phase1.Boundaries.Add(L5_6);
            phase1.Boundaries.Add(L6_7);
            phase1.Boundaries.Add(L13_14);
            phase1.Boundaries.Add(L5_13);
            phase1.Boundaries.Add(L7_14);

            phase2.Boundaries.Add(L1_2);
            phase2.Boundaries.Add(L6_7);
            phase2.Boundaries.Add(L7_8);
            phase2.Boundaries.Add(L8_9);
            phase2.Boundaries.Add(L1_6);
            phase2.Boundaries.Add(L2_9);

            phase3.Boundaries.Add(L8_9);
            phase3.Boundaries.Add(L9_10);
            phase3.Boundaries.Add(L10_11);
            phase3.Boundaries.Add(L15_16);
            phase3.Boundaries.Add(L8_15);
            phase3.Boundaries.Add(L11_16);

            phase4.Boundaries.Add(L3_4);
            phase4.Boundaries.Add(L10_11);
            phase4.Boundaries.Add(L11_12);
            phase4.Boundaries.Add(L3_10);
            phase4.Boundaries.Add(L4_12);

            // Initialize model
            var geometricModel = new GeometricModel(physicalModel);
            double elementSize = (maxX - minX) / numElementsPerAxis;
            geometricModel.MeshTolerance = new UsedDefinedMeshTolerance(elementSize);
            geometricModel.Phases.Add(phase0);
            geometricModel.Phases.Add(phase1);
            geometricModel.Phases.Add(phase2);
            geometricModel.Phases.Add(phase3);
            geometricModel.Phases.Add(phase4);

            geometricModel.AssossiatePhasesNodes();
            geometricModel.AssociatePhasesElements();
            return geometricModel;
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
            CartesianPoint A = TranformSingle(0.5 * rectLength, 0.0);
            CartesianPoint B = TranformSingle(1.5 * rectLength, 0.0);
            CartesianPoint C = TranformSingle(0.0, rectHeight);
            CartesianPoint D = TranformSingle(0.5 * rectLength, rectHeight);
            CartesianPoint E = TranformSingle(rectLength, rectHeight);
            CartesianPoint F = TranformSingle(1.5 * rectLength, rectHeight);
            CartesianPoint G = TranformSingle(0.0, 2.0 * rectHeight);
            CartesianPoint H = TranformSingle(rectLength, 2.0 * rectHeight);

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

        private CartesianPoint TranformSingle(double x, double y)
        {
            double offsetX = 0.0555, offsetY = 0.0555, phi = Math.PI / 6.0;

            double r = Math.Sqrt(x * x + y * y);
            double theta = Math.Atan2(y, x);
            double newX = offsetX + r * Math.Cos(phi + theta);
            double newY = offsetY + r * Math.Sin(phi + theta);
            return new CartesianPoint(newX, newY);
        }

        private CartesianPoint TranformPerc(double x, double y)
        {
            double offsetX = -1.055, offsetY = -0.0555, phi = 0.0;

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
