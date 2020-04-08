using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.Geometry.Shapes;
using ISAAR.MSolve.XFEM.Multiphase.Entities;
using ISAAR.MSolve.XFEM.Multiphase.Geometry;

namespace ISAAR.MSolve.XFEM.Tests.Multiphase
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

        public GeometricModel Create2Phases()
        {
            // -------------
            // |     |     |
            // |  0  |  1  |
            // |     |     |
            // |     |     |
            // -------------

            double boundaryX = 0.5 * (minX + maxX);
            var start = new CartesianPoint(boundaryX, minY);
            var end = new CartesianPoint(boundaryX, maxY);

            // Define phases
            var phase0 = new DefaultPhase();
            var phase1 = new ConvexPhase(100000);

            // Create boundaries and associate them with their phases
            var boundary = new PhaseBoundary(new XFEM.Multiphase.Geometry.LineSegment2D(start, end), phase0, phase1);

            // Initialize model
            var geometricModel = new GeometricModel();
            double elementSize = (maxX - minX) / numElementsPerAxis;
            geometricModel.MeshTolerance = new UserDefinedMeshTolerance(elementSize);
            geometricModel.Phases.Add(phase0);
            geometricModel.Phases.Add(phase1);

            return geometricModel;
        }

        public GeometricModel Create3Phases()
        {
            // ------C------
            // |     |     |
            // |     |  1  |
            // |     |     |
            // |  0  B-----D
            // |     |     |
            // |     |  2  |
            // |     |     |
            // ------A------

            double middleX = 0.5 * (minX + maxX);
            double middleY = 0.5 * (minY + maxY);
            var A = new CartesianPoint(middleX, minY);
            var B = new CartesianPoint(middleX, middleY);
            var C = new CartesianPoint(middleX, maxY);
            var D = new CartesianPoint(maxX, middleY);

            // Define phases
            var phase0 = new DefaultPhase();
            var phase1 = new ConvexPhase(1);
            var phase2 = new ConvexPhase(2);

            // Create boundaries and associate them with their phases
            var AB = new PhaseBoundary(new XFEM.Multiphase.Geometry.LineSegment2D(A, B), phase0, phase2);
            var BC = new PhaseBoundary(new XFEM.Multiphase.Geometry.LineSegment2D(B, C), phase0, phase1);
            var BD = new PhaseBoundary(new XFEM.Multiphase.Geometry.LineSegment2D(B, D), phase1, phase2);

            // Initialize model
            var geometricModel = new GeometricModel();
            double elementSize = (maxX - minX) / numElementsPerAxis;
            geometricModel.MeshTolerance = new UserDefinedMeshTolerance(elementSize);
            geometricModel.Phases.Add(phase0);
            geometricModel.Phases.Add(phase1);
            geometricModel.Phases.Add(phase2);

            return geometricModel;
        }

        public GeometricModel CreateHollowTetrisPhases()
        {
            // Generate rectangles by rotating the following shapes
            //
            //  G------------------H
            //  |  C4----------C3  |
            //  |   | 4        |   | 
            //  |2 C1----------C2  |
            //  C---------D--------E--------F
            //            |  D4----------D3 |
            //            |   | 3        |  |
            //            |1 D1----------D2 |
            //            A-----------------B
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

            CartesianPoint C1 = TranformSingle(0.25 * rectLength, 1.25 * rectHeight);
            CartesianPoint C2 = TranformSingle(0.75 * rectLength, 1.25 * rectHeight);
            CartesianPoint C3 = TranformSingle(0.75 * rectLength, 1.75 * rectHeight);
            CartesianPoint C4 = TranformSingle(0.25 * rectLength, 1.75 * rectHeight);
            CartesianPoint D1 = TranformSingle(0.75 * rectLength, 0.25 * rectHeight);
            CartesianPoint D2 = TranformSingle(1.25 * rectLength, 0.25 * rectHeight);
            CartesianPoint D3 = TranformSingle(1.25 * rectLength, 0.75 * rectHeight);
            CartesianPoint D4 = TranformSingle(0.75 * rectLength, 0.75 * rectHeight);

            // Define phases
            var phase0 = new DefaultPhase();
            var phase1 = new HollowConvexPhase(1);
            var phase2 = new HollowConvexPhase(2);
            var phase3 = new ConvexPhase(3);
            var phase4 = new ConvexPhase(4);

            // Create boundaries and associate them with their phases
            var AB = new PhaseBoundary(new XFEM.Multiphase.Geometry.LineSegment2D(A, B), phase1, phase0);
            var CD = new PhaseBoundary(new XFEM.Multiphase.Geometry.LineSegment2D(C, D), phase2, phase0);
            var DE = new PhaseBoundary(new XFEM.Multiphase.Geometry.LineSegment2D(D, E), phase2, phase1);
            var EF = new PhaseBoundary(new XFEM.Multiphase.Geometry.LineSegment2D(E, F), phase0, phase1);
            var GH = new PhaseBoundary(new XFEM.Multiphase.Geometry.LineSegment2D(G, H), phase0, phase2);
            var AD = new PhaseBoundary(new XFEM.Multiphase.Geometry.LineSegment2D(A, D), phase0, phase1);
            var BF = new PhaseBoundary(new XFEM.Multiphase.Geometry.LineSegment2D(B, F), phase1, phase0);
            var CG = new PhaseBoundary(new XFEM.Multiphase.Geometry.LineSegment2D(C, G), phase0, phase2);
            var EH = new PhaseBoundary(new XFEM.Multiphase.Geometry.LineSegment2D(E, H), phase2, phase0);

            var C1C2 = new PhaseBoundary(new XFEM.Multiphase.Geometry.LineSegment2D(C1, C2), phase4, phase2);
            var C2C3 = new PhaseBoundary(new XFEM.Multiphase.Geometry.LineSegment2D(C2, C3), phase4, phase2);
            var C3C4 = new PhaseBoundary(new XFEM.Multiphase.Geometry.LineSegment2D(C3, C4), phase4, phase2);
            var C4C1 = new PhaseBoundary(new XFEM.Multiphase.Geometry.LineSegment2D(C4, C1), phase4, phase2);
            var D1D2 = new PhaseBoundary(new XFEM.Multiphase.Geometry.LineSegment2D(D1, D2), phase3, phase1);
            var D2D3 = new PhaseBoundary(new XFEM.Multiphase.Geometry.LineSegment2D(D2, D3), phase3, phase1);
            var D3D4 = new PhaseBoundary(new XFEM.Multiphase.Geometry.LineSegment2D(D3, D4), phase3, phase1);
            var D4D1 = new PhaseBoundary(new XFEM.Multiphase.Geometry.LineSegment2D(D4, D1), phase3, phase1);

            // Define internal phases
            phase1.AddInternalPhase(phase3);
            phase2.AddInternalPhase(phase4);

            // Initialize model
            var geometricModel = new GeometricModel();
            double elementSize = (maxX - minX) / numElementsPerAxis;
            geometricModel.MeshTolerance = new UserDefinedMeshTolerance(elementSize);
            geometricModel.Phases.Add(phase0);
            geometricModel.Phases.Add(phase1);
            geometricModel.Phases.Add(phase2);
            geometricModel.Phases.Add(phase3);
            geometricModel.Phases.Add(phase4);

            return geometricModel;
        }

        public GeometricModel CreatePercolatedTetrisPhases()
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

            // Define phases
            var phase0 = new DefaultPhase();
            var phase1 = new ConvexPhase(1);
            var phase2 = new ConvexPhase(2);
            var phase3 = new ConvexPhase(3);
            var phase4 = new ConvexPhase(4);

            // Create boundaries and associate them with their phases
            var L1_2 = new PhaseBoundary(new XFEM.Multiphase.Geometry.LineSegment2D(P1, P2), phase2, phase0);
            var L3_4 = new PhaseBoundary(new XFEM.Multiphase.Geometry.LineSegment2D(P3, P4), phase4, phase0);
            var L5_6 = new PhaseBoundary(new XFEM.Multiphase.Geometry.LineSegment2D(P5, P6), phase1, phase0);
            var L6_7 = new PhaseBoundary(new XFEM.Multiphase.Geometry.LineSegment2D(P6, P7), phase1, phase2);
            var L7_8 = new PhaseBoundary(new XFEM.Multiphase.Geometry.LineSegment2D(P7, P8), phase0, phase2);
            var L8_9 = new PhaseBoundary(new XFEM.Multiphase.Geometry.LineSegment2D(P8, P9), phase3, phase2);
            var L9_10 = new PhaseBoundary(new XFEM.Multiphase.Geometry.LineSegment2D(P9, P10), phase3, phase0);
            var L10_11 = new PhaseBoundary(new XFEM.Multiphase.Geometry.LineSegment2D(P10, P11), phase3, phase4);
            var L11_12 = new PhaseBoundary(new XFEM.Multiphase.Geometry.LineSegment2D(P11, P12), phase0, phase4);
            var L13_14 = new PhaseBoundary(new XFEM.Multiphase.Geometry.LineSegment2D(P13, P14), phase0, phase1);
            var L15_16 = new PhaseBoundary(new XFEM.Multiphase.Geometry.LineSegment2D(P15, P16), phase0, phase3);

            var L1_6 = new PhaseBoundary(new XFEM.Multiphase.Geometry.LineSegment2D(P1, P6), phase0, phase2);
            var L2_9 = new PhaseBoundary(new XFEM.Multiphase.Geometry.LineSegment2D(P2, P9), phase2, phase0);
            var L3_10 = new PhaseBoundary(new XFEM.Multiphase.Geometry.LineSegment2D(P3, P10), phase0, phase4);
            var L4_12 = new PhaseBoundary(new XFEM.Multiphase.Geometry.LineSegment2D(P4, P12), phase4, phase0);
            var L5_13 = new PhaseBoundary(new XFEM.Multiphase.Geometry.LineSegment2D(P5, P13), phase0, phase1);
            var L7_14 = new PhaseBoundary(new XFEM.Multiphase.Geometry.LineSegment2D(P7, P14), phase1, phase0);
            var L8_15 = new PhaseBoundary(new XFEM.Multiphase.Geometry.LineSegment2D(P8, P15), phase0, phase3);
            var L11_16 = new PhaseBoundary(new XFEM.Multiphase.Geometry.LineSegment2D(P11, P16), phase3, phase0);
   

            // Initialize model
            var geometricModel = new GeometricModel();
            double elementSize = (maxX - minX) / numElementsPerAxis;
            geometricModel.MeshTolerance = new UserDefinedMeshTolerance(elementSize);
            geometricModel.Phases.Add(phase0);
            geometricModel.Phases.Add(phase1);
            geometricModel.Phases.Add(phase2);
            geometricModel.Phases.Add(phase3);
            geometricModel.Phases.Add(phase4);
            
            return geometricModel;
        }

        public GeometricModel CreateScatterRectangularPhases()
        {
            // Generate rectangles
            List<Rectangle2D> rectangles = ScatterDisjointRects();

            // Create phases out of them
            var geometricModel = new GeometricModel();
            double elementSize = (maxX - minX) / numElementsPerAxis;
            geometricModel.MeshTolerance = new UserDefinedMeshTolerance(elementSize);
            var defaultPhase = new DefaultPhase();
            geometricModel.Phases.Add(defaultPhase);
            int phaseID = 1;
            foreach (Rectangle2D rect in rectangles)
            {
                var phase = new ConvexPhase(phaseID);
                ++phaseID;
                #region debug
                //if ((phaseID - 1 != 5) && (phaseID - 1 != 2)) continue;
                #endregion
                geometricModel.Phases.Add(phase);
                for (int i = 0; i < 4; ++i)
                {
                    CartesianPoint start = rect.Vertices[i];
                    CartesianPoint end = rect.Vertices[(i + 1) % 4];
                    var segment = new XFEM.Multiphase.Geometry.LineSegment2D(start, end);

                    // The vertices are in anti-clockwise order, therefore the positive phase is internal
                    var boundary = new PhaseBoundary(segment, phase, defaultPhase);
                }
            }
            return geometricModel;
        }

        public GeometricModel CreateSingleTetrisPhases()
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

            // Define phases
            var phase0 = new DefaultPhase();
            var phase1 = new ConvexPhase(1);
            var phase2 = new ConvexPhase(3);

            // Create boundaries and associate them with their phases
            var AB = new PhaseBoundary(new XFEM.Multiphase.Geometry.LineSegment2D(A, B), phase1, phase0);
            var CD = new PhaseBoundary(new XFEM.Multiphase.Geometry.LineSegment2D(C, D), phase2, phase0);
            var DE = new PhaseBoundary(new XFEM.Multiphase.Geometry.LineSegment2D(D, E), phase2, phase1);
            var EF = new PhaseBoundary(new XFEM.Multiphase.Geometry.LineSegment2D(E, F), phase0, phase1);
            var GH = new PhaseBoundary(new XFEM.Multiphase.Geometry.LineSegment2D(G, H), phase0, phase2);
            var AD = new PhaseBoundary(new XFEM.Multiphase.Geometry.LineSegment2D(A, D), phase0, phase1);
            var BF = new PhaseBoundary(new XFEM.Multiphase.Geometry.LineSegment2D(B, F), phase1, phase0);
            var CG = new PhaseBoundary(new XFEM.Multiphase.Geometry.LineSegment2D(C, G), phase0, phase2);
            var EH = new PhaseBoundary(new XFEM.Multiphase.Geometry.LineSegment2D(E, H), phase2, phase0);

            // Initialize model
            var geometricModel = new GeometricModel();
            double elementSize = (maxX - minX) / numElementsPerAxis;
            geometricModel.MeshTolerance = new UserDefinedMeshTolerance(elementSize);
            geometricModel.Phases.Add(phase0);
            geometricModel.Phases.Add(phase1);
            geometricModel.Phases.Add(phase2);

            return geometricModel;
        }

        private CartesianPoint TranformSingle(double x, double y)
        {
            double offsetX = 0.06, offsetY = 0.06, phi = Math.PI / 6.0;

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
