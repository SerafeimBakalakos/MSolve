using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ISAAR.MSolve.Discretization;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Discretization.Mesh;
using ISAAR.MSolve.Discretization.Mesh.Generation;
using ISAAR.MSolve.Discretization.Mesh.Generation.GMSH;
using ISAAR.MSolve.Solvers.Direct;
using ISAAR.MSolve.Solvers.Ordering;
using ISAAR.MSolve.Solvers.Ordering.Reordering;
using MGroup.XFEM.Cracks;
using MGroup.XFEM.Cracks.Geometry;
using MGroup.XFEM.Cracks.Geometry.LSM;
using MGroup.XFEM.Cracks.Jintegral;
using MGroup.XFEM.Cracks.PropagationCriteria;
using MGroup.XFEM.Cracks.PropagationTermination;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Enrichment.Enrichers;
using MGroup.XFEM.Enrichment.SingularityResolution;
using MGroup.XFEM.Entities;
using MGroup.XFEM.Geometry.Boundaries;
using MGroup.XFEM.Geometry.Primitives;
using MGroup.XFEM.Integration;
using MGroup.XFEM.Integration.Quadratures;
using MGroup.XFEM.Materials;
using Xunit;

namespace MGroup.XFEM.Tests.Fracture.Benchmarks
{
    public static class HolesBenchmark
    {
        public enum BoundaryConditions
        {
            BottomConstrainXY_TopDisplacementY, BottomConstrainXY_TopConstrainXDisplacementY,
            BottomConstrainY_TopDisplacementY, BottomDisplacementY_TopDisplacementY,
            BottomConstrainXDisplacementY_TopConstrainXDisplacementY
        }

        /// <summary>
        /// Thickness of the whole domain
        /// </summary>
        private const double t = 1.0; // mm

        /// <summary>
        /// Young's modulus
        /// </summary>
        private const double E = 2E5;  // N/mm^2

        /// <summary>
        /// Poisson's ratio
        /// </summary>
        private const double v = 0.3;

        private const bool planeStress = false;

        /// <summary>
        /// The maximum value that the effective SIF can reach before collapse occurs.
        /// </summary>
        private const double fractureToughness = double.MaxValue; // N.mm^(3/2) actually 1500

        // Boundary conditions
        private const double prescribedDisplacement = 0.1; //mm
        private const BoundaryConditions bcs = BoundaryConditions.BottomConstrainY_TopDisplacementY;

        // Geometry (m)
        private static readonly double[] minCoords = { 0.0, 0.0 };
        private static readonly double[] maxCoords = { 20.0, 10.0 }; //mm
        private const double holeRadius = 2.0, leftHoleX = 3.0, leftHoleY = 7.0, rightHoleX = 17.0, rightHoleY = 3.0; //mm
        private const double initialCrackLength = 1.0;
        private static readonly double[] leftCrackMouth = { 0.0, 2.85 };
        private static readonly double[] leftCrackTip = { minCoords[0] + initialCrackLength, 2.85 }; //mm
        private static readonly double[] rightCrackMouth = { 20.0, 7.15 }; //mm
        private static readonly double[] rightCrackTip = { maxCoords[0] - initialCrackLength, 7.15 }; //mm

        // Mesh
        private static readonly string meshPath = Path.Combine(new string[]
        {
            Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName, "Resources", "holes_4272dofs.msh"
        });

        private const double growthLength = 1.0; // mm. Must be sufficiently larger than the element size.

        // Usually should be in [1.5, 2.5). The J-integral radius must be large enough to at least include elements around
        // the element that contains the crack tip. However it must not be so large that an element intersected by the 
        // J-integral contour is containes the previous crack tip. Thus the J-integral radius must be sufficiently smaller
        // than the crack growth length.
        private const double jIntegralRadiusRatio = 2.0;

        private const double heavisideTol = 0.12;
        private const double tipEnrichmentArea = 0.5;
        private const int maxIterations = 12;
        private const int subdomainID = 0;

        [Fact]
        public static void TestCrackPropagationPath()
        {
            XModel<IXCrackElement> model = CreateModel();
            #region debug
            //string[] outputDir = { @"C:\Users\Serafeim\Desktop\XFEM2020\Cracks\HolesBenchmark\left\",
            //    @"C:\Users\Serafeim\Desktop\XFEM2020\Cracks\HolesBenchmark\right\"};
            //var outputMesh = new XFEM.Plotting.Mesh.ContinuousOutputMesh(model.XNodes, model.Elements);
            //for (int i = 0; i < 2; ++i)
            //{
            //    var crackI = (ExteriorLsmCrack)model.Discontinuities[i];
            //    crackI.Observers.Add(new MGroup.XFEM.Plotting.Writers.CrackLevelSetPlotter(crackI, outputMesh, outputDir[i]));
            //}
            #endregion
            RunAnalysis(model);
            var leftCrack = (ExteriorLsmCrack)model.Discontinuities[0];
            var rightCrack = (ExteriorLsmCrack)model.Discontinuities[1];

            // Expected propagation paths
            var expectedPathLeft = new List<double[]>();
            expectedPathLeft.Add(new double[] { 0, 2.85});
            expectedPathLeft.Add(new double[] { 1, 2.85});
            expectedPathLeft.Add(new double[] { 1.9442660505904605, 3.179183270690195 });
            expectedPathLeft.Add(new double[] { 2.879042130851585, 3.534420768389478 });
            expectedPathLeft.Add(new double[] { 3.809959295997644, 3.8996511499795443 });
            expectedPathLeft.Add(new double[] { 4.7710602676437395, 3.62345389745713 });
            expectedPathLeft.Add(new double[] { 5.709721572298851, 3.2786134196370744 });
            expectedPathLeft.Add(new double[] { 6.709720923801637, 3.277474563189558 });
            expectedPathLeft.Add(new double[] { 7.703628761084884, 3.167260176710576 });
            expectedPathLeft.Add(new double[] { 8.702702093417999, 3.2103005835488636 });
            expectedPathLeft.Add(new double[] { 9.702701768642486, 3.209494636329524 });
            expectedPathLeft.Add(new double[] { 10.673024257844089, 3.451309166392556 });
            expectedPathLeft.Add(new double[] { 11.640463322588532, 3.704413208253598 });

            var expectedPathRight = new List<double[]>();
            expectedPathRight.Add(new double[] { 20, 7.15 });
            expectedPathRight.Add(new double[] { 19, 7.15 });
            expectedPathRight.Add(new double[] { 18.055227462611214, 6.822273204336361 });
            expectedPathRight.Add(new double[] { 17.108958187245708, 6.498893460847005 });
            expectedPathRight.Add(new double[] { 16.19004314187083, 6.1044379635045445 });
            expectedPathRight.Add(new double[] { 15.280751350828503, 6.520597114242617 });
            expectedPathRight.Add(new double[] { 14.288033326234983, 6.641058409467012 });
            expectedPathRight.Add(new double[] { 13.302316315137505, 6.809468545839664 });
            expectedPathRight.Add(new double[] { 12.303258240706835, 6.766075413723122 });
            expectedPathRight.Add(new double[] { 11.303267593766813, 6.761750366616408 });
            expectedPathRight.Add(new double[] { 10.308940838326691, 6.655381652156191 });
            expectedPathRight.Add(new double[] { 9.317703728430507, 6.5232870195451005 });
            expectedPathRight.Add(new double[] { 8.35139114501457, 6.265915709698763 });

            // Chack propagation paths
            int precision = 8;
            Assert.Equal(expectedPathLeft.Count, leftCrack.CrackPath.Count);
            for (int i = 0; i < expectedPathLeft.Count; ++i)
            {
                Assert.Equal(expectedPathLeft[i][0], leftCrack.CrackPath[i][0], precision);
                Assert.Equal(expectedPathLeft[i][1], leftCrack.CrackPath[i][1], precision);
            }

            Assert.Equal(expectedPathRight.Count, rightCrack.CrackPath.Count);
            for (int i = 0; i < expectedPathRight.Count; ++i)
            {
                Assert.Equal(expectedPathRight[i][0], rightCrack.CrackPath[i][0], precision);
                Assert.Equal(expectedPathRight[i][1], rightCrack.CrackPath[i][1], precision);
            }
        }

        private static void ApplyBoundaryConditions(XModel<IXCrackElement> model)
        {
            double meshTol = 1E-6;
            XNode leftTopCorner = model.XNodes.Where(
                node => (Math.Abs(node.X - minCoords[0]) <= meshTol) && (Math.Abs(node.Y - maxCoords[1]) <= meshTol)).First();
            XNode rightBottomCorner = model.XNodes.Where(
                node => (Math.Abs(node.X - maxCoords[0]) <= meshTol) && (Math.Abs(node.Y - minCoords[1]) <= meshTol)).First();
            XNode[] bottomNodes = model.XNodes.Where(node => Math.Abs(node.Y - minCoords[1]) <= meshTol).ToArray();
            XNode[] topNodes = model.XNodes.Where(node => Math.Abs(node.Y - maxCoords[1]) <= meshTol).ToArray();

            if (bcs == BoundaryConditions.BottomConstrainXY_TopDisplacementY)
            {
                foreach (var node in bottomNodes)
                {
                    node.Constraints.Add(new Constraint() { DOF = StructuralDof.TranslationX, Amount = 0.0 });
                    node.Constraints.Add(new Constraint() { DOF = StructuralDof.TranslationY, Amount = 0.0 });
                }
                foreach (var node in topNodes)
                {
                    node.Constraints.Add(new Constraint() { DOF = StructuralDof.TranslationY, Amount = prescribedDisplacement });
                }
            }
            else if (bcs == BoundaryConditions.BottomConstrainXY_TopConstrainXDisplacementY)
            {
                foreach (var node in bottomNodes)
                {
                    node.Constraints.Add(new Constraint() { DOF = StructuralDof.TranslationX, Amount = 0.0 });
                    node.Constraints.Add(new Constraint() { DOF = StructuralDof.TranslationY, Amount = 0.0 });
                }
                foreach (var node in topNodes)
                {
                    node.Constraints.Add(new Constraint() { DOF = StructuralDof.TranslationX, Amount = 0.0 });
                    node.Constraints.Add(new Constraint() { DOF = StructuralDof.TranslationY, Amount = prescribedDisplacement });
                }
            }
            else if (bcs == BoundaryConditions.BottomConstrainY_TopDisplacementY)
            {
                foreach (var node in bottomNodes)
                {
                    node.Constraints.Add(new Constraint() { DOF = StructuralDof.TranslationY, Amount = 0.0 });
                }
                foreach (var node in topNodes)
                {
                    node.Constraints.Add(new Constraint() { DOF = StructuralDof.TranslationY, Amount = prescribedDisplacement });
                }
                leftTopCorner.Constraints.Add(new Constraint() { DOF = StructuralDof.TranslationX, Amount = 0.0 });
                rightBottomCorner.Constraints.Add(new Constraint() { DOF = StructuralDof.TranslationX, Amount = 0.0 });
            }
            else if (bcs == BoundaryConditions.BottomDisplacementY_TopDisplacementY)
            {
                foreach (var node in bottomNodes)
                {
                    node.Constraints.Add(new Constraint() { DOF = StructuralDof.TranslationY, Amount = -0.5 * prescribedDisplacement });
                }
                foreach (var node in topNodes)
                {
                    node.Constraints.Add(new Constraint() { DOF = StructuralDof.TranslationY, Amount = 0.5 * prescribedDisplacement });
                }
                leftTopCorner.Constraints.Add(new Constraint() { DOF = StructuralDof.TranslationX, Amount = 0.0 });
                rightBottomCorner.Constraints.Add(new Constraint() { DOF = StructuralDof.TranslationX, Amount = 0.0 });
            }
            else if (bcs == BoundaryConditions.BottomConstrainXDisplacementY_TopConstrainXDisplacementY)
            {
                foreach (var node in bottomNodes)
                {
                    node.Constraints.Add(new Constraint() { DOF = StructuralDof.TranslationX, Amount = 0.0 });
                    node.Constraints.Add(new Constraint() { DOF = StructuralDof.TranslationY, Amount = -0.5 * prescribedDisplacement });
                }
                foreach (var node in topNodes)
                {
                    node.Constraints.Add(new Constraint() { DOF = StructuralDof.TranslationX, Amount = 0.0 });
                    node.Constraints.Add(new Constraint() { DOF = StructuralDof.TranslationY, Amount = 0.5 * prescribedDisplacement });
                }
            }
            else throw new Exception("This code shouldn't have been reached.");
        }

        private static XModel<IXCrackElement> CreateModel()
        {
            var model = new XModel<IXCrackElement>(2);
            model.Subdomains[subdomainID] = new XSubdomain(subdomainID);
            model.FindConformingSubcells = true;

            // Mesh generation
            var reader = new GmshReader<XNode>(meshPath);
            (IReadOnlyList<XNode> nodes, IReadOnlyList<CellConnectivity<XNode>> elementConnectivities) = reader.CreateMesh(
                (id, x, y, z) => new XNode(id, x, y));

            // Nodes
            foreach (XNode node in nodes) model.XNodes.Add(node);

            // Elements
            var material = new HomogeneousFractureMaterialField2D(E, v, t, planeStress);
            var cutIntegration = new IntegrationWithConformingSubtriangles2D(GaussLegendre2D.GetQuadratureWithOrder(2, 2),
                TriangleQuadratureSymmetricGaussian.Order2Points3);
            var tipIntegration = new IntegrationWithNonconformingQuads2D(8, GaussLegendre2D.GetQuadratureWithOrder(2, 2));
            var bulkIntegration = new CrackElementIntegrationStrategy(
                cutIntegration, tipIntegration, tipIntegration);
            var factory = new XCrackElementFactory2D(material, t, bulkIntegration);

            // Elements
            for (int e = 0; e < elementConnectivities.Count; ++e)
            {
                IXCrackElement element = factory.CreateElement(e, CellType.Quad4, elementConnectivities[e].Vertices);
                model.Elements.Add(element);
                model.Subdomains[subdomainID].Elements.Add(element);
            }

            ApplyBoundaryConditions(model);

            // Cracks, enrichments
            var jIntegrationRule = new IntegrationWithNonconformingQuads2D(8, GaussLegendre2D.GetQuadratureWithOrder(4, 4));
            var leftPropagator = new JintegralPropagator2D(jIntegralRadiusRatio, jIntegrationRule, material,
                new MaximumCircumferentialTensileStressCriterion(), new ConstantIncrement2D(growthLength));
            var leftCrack = new ExteriorLsmCrack(0, new PolyLine2D(leftCrackMouth, leftCrackTip), model, leftPropagator);
            var rightPropagator = new JintegralPropagator2D(jIntegralRadiusRatio, jIntegrationRule, material,
                new MaximumCircumferentialTensileStressCriterion(), new ConstantIncrement2D(growthLength));
            var rightCrack = new ExteriorLsmCrack(1, new PolyLine2D(rightCrackMouth, rightCrackTip), model, rightPropagator);
            var enricher = new NodeEnricherIndependentCracks(
                new ICrack[] { leftCrack, rightCrack }, new RelativeAreaSingularityResolver(heavisideTol), tipEnrichmentArea);
            model.Discontinuities.Add(leftCrack);
            model.Discontinuities.Add(rightCrack);
            model.NodeEnrichers.Add(enricher);

            return model;
        }

        private static void RunAnalysis(XModel<IXCrackElement> model)
        {
            var solverBuilder = new SkylineSolver.Builder();
            solverBuilder.DofOrderer = new DofOrderer(new NodeMajorDofOrderingStrategy(), AmdReordering.CreateWithCSparseAmd());
            SkylineSolver solver = solverBuilder.BuildSolver(model);

            var domainBoundary = new DomainBoundary();
            var termination = new TerminationLogic.Or(
                new FractureToughnessTermination(fractureToughness),
                new CrackExitsDomainTermination(domainBoundary));
            var analyzer = new QuasiStaticLefmAnalyzer(model, solver, maxIterations, termination);

            analyzer.Analyze();
        }

        private class DomainBoundary : IDomainBoundary
        {
            public DomainBoundary()
            {
            }

            public bool SurroundsPoint(double[] point)
            {
                // Shapes
                var rectHull = new RectangularDomainBoundary(minCoords, maxCoords);
                var leftCircle = new Circle2D(leftHoleX, leftHoleY, holeRadius);
                var rightCircle = new Circle2D(rightHoleX, rightHoleY, holeRadius);

                // Internal points lie inside the rectangle, but outside the circular holes.
                if (rectHull.SurroundsPoint(point))
                {
                    if (leftCircle.SignedDistanceOf(point) > 0) return true;
                    if (rightCircle.SignedDistanceOf(point) > 0) return true;
                }
                return false;
            }
        }
    }
}
