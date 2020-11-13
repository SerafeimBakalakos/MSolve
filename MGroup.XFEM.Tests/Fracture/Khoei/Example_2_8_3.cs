using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Discretization.Mesh;
using ISAAR.MSolve.LinearAlgebra.Input;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using MGroup.XFEM.Cracks.Geometry;
using MGroup.XFEM.Cracks.Geometry.LSM;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Enrichment;
using MGroup.XFEM.Entities;
using MGroup.XFEM.Geometry;
using MGroup.XFEM.Geometry.LSM;
using MGroup.XFEM.Geometry.Primitives;
using MGroup.XFEM.Integration;
using MGroup.XFEM.Integration.Quadratures;
using MGroup.XFEM.Materials;
using Xunit;

namespace MGroup.XFEM.Tests.Fracture.Khoei
{
    public static class Example_2_8_3
    {
        private static readonly Func<double, double> round = x => 1E6 * Math.Round(x * 1E-6, 3);

        private const double E = 2E6;
        private const double v = 0.3;
        private const double thickness = 1.0;
        private const bool planeStress = false;

        [Fact]
        public static void TestElementStiffnesses()
        {
            (XNode[] nodes, XCrackElement2D[] elements) = CreateElements();
            Dictionary<IEnrichment, IDofType[]> enrichedDofs = EnrichNodesElements(nodes, elements);
            
            for (int e = 0; e < elements.Length; ++e)
            {
                elements[e].IdentifyDofs(enrichedDofs);
                elements[e].IdentifyIntegrationPointsAndMaterials();
                IMatrix computedK = elements[e].StiffnessMatrix(null);

                double tol = 1E-13;
                Matrix expectedK = GetExpectedStiffness(e);
                IMatrix computedRoundedK = computedK.DoToAllEntries(round);

                #region debug
                //var writer = new ISAAR.MSolve.LinearAlgebra.Output.FullMatrixWriter();
                //string path = @"C:\Users\Serafeim\Desktop\XFEM2020\Cracks\DebugOutput\K.txt";
                //writer.WriteToFile(expectedK.Subtract(computedRoundedK), path);
                #endregion

                Assert.True(expectedK.Equals(computedRoundedK, tol));
            }
        }

        //[Fact] //TODO: Figure why this does not seem to work correctly.
        public static void TestSolution()
        {
            (XNode[] nodes, XCrackElement2D[] elements) = CreateElements();
            Dictionary<IEnrichment, IDofType[]> enrichedDofs = EnrichNodesElements(nodes, elements);

            // FEM assembly
            var elementToGlobalMaps = new List<int[]>();
            elementToGlobalMaps.Add(new int[] { /*n0*/0, 1, /*n1*/2, 3, 4, 5, /*n2*/6, 7, 8, 9, /*n3*/10, 11});
            elementToGlobalMaps.Add(new int[] { /*n1*/2, 3, 4, 5, /*n4*/12, 13, 14, 15, /*n7*/20, 21, 22, 23, /*n2*/6, 7, 8, 9 });
            elementToGlobalMaps.Add(new int[] { /*n4*/12, 13, 14, 15, /*n5*/16, 17, /*n5*/18, 19, /*n7*/20, 21, 22, 23 });

            var globalK = Matrix.CreateZero(24, 24);
            for (int e = 0; e < elements.Length; ++e)
            {
                elements[e].IdentifyDofs(enrichedDofs);
                elements[e].IdentifyIntegrationPointsAndMaterials();
                IMatrix elementK = elements[e].StiffnessMatrix(null);
                AddSubmatrix(globalK, elementK, elementToGlobalMaps[e]);
            }


            // Solution
            int[] freeDofs =
            { 
                /*n1*/2, 3, 4, 5, /*n2*/6, 7, 8, 9, /*n4*/12, 13, 14, 15, /*n5*/17, /*n6*/19, /*n7*/20, 21, 22, 23
            };
            int[] constrainedDofs =
            {
                /*n0*/0, 1, /*n3*/10, 11, /*n5*/16, /*n6*/18
            };
            Matrix Kff = globalK.GetSubmatrix(freeDofs, freeDofs);
            Matrix Kfc = globalK.GetSubmatrix(freeDofs, constrainedDofs);

            Vector U = GetExpectedSolution();
            Vector Uf = U.GetSubvector(freeDofs);
            Vector Uc = U.GetSubvector(constrainedDofs);

            // The system is singular or very degenerate and cannot be solved with reasonable accuracy. 
            // Instead we check that the solution satisfies it.
            Vector Ff = Kff * Uf + Kfc * Uc;
            
            // Compare
            double tol = 1E-13;
            var expectedFf = Vector.CreateFromArray(new double[freeDofs.Length]);
            Assert.True(expectedFf.Equals(Ff, tol));
        }


        private static Dictionary<IEnrichment, IDofType[]> EnrichNodesElements(XNode[] nodes, XCrackElement2D[] elements)
        {
            var crack = new Crack(new double[] { 30.0, +40.0 }, new double[] { 30.0, -40.0 });
            var stepEnrichment = new CrackStepEnrichment(0, crack);
            nodes[1].Enrichments[stepEnrichment] = stepEnrichment.EvaluateAt(nodes[1]);
            nodes[2].Enrichments[stepEnrichment] = stepEnrichment.EvaluateAt(nodes[2]);
            nodes[4].Enrichments[stepEnrichment] = stepEnrichment.EvaluateAt(nodes[4]);
            nodes[7].Enrichments[stepEnrichment] = stepEnrichment.EvaluateAt(nodes[7]);

            var enrichedDofs = new Dictionary<IEnrichment, IDofType[]>();
            enrichedDofs[stepEnrichment] = new IDofType[2] {
                new EnrichedDof(stepEnrichment, StructuralDof.TranslationX),
                new EnrichedDof(stepEnrichment, StructuralDof.TranslationY)
            };

            elements[1].InteractingCracks[crack] = new OpenLsmElementIntersection2D(crack.ID, elements[1].ID,
                RelativePositionCurveElement.Intersecting, false,
                new double[][] { new double[] { 30.0, 0.0 }, new double[] { 30.0, 20.0 } } );

            return enrichedDofs;
        }

        private static (XNode[] nodes, XCrackElement2D[] elements) CreateElements()
        {
            XNode[] nodes = new XNode[]
            {
                new XNode(0, 00.0, 00.0),
                new XNode(1, 20.0, 00.0),
                new XNode(2, 20.0, 20.0),
                new XNode(3, 00.0, 20.0),

                new XNode(4, 40.0, 00.0),
                new XNode(5, 60.0, 00.0),
                new XNode(6, 60.0, 20.0),
                new XNode(7, 40.0, 20.0)
            };

            var material = new HomogeneousFractureMaterialField2D(E, v, thickness, planeStress);
            var enrichedIntegration = new IntegrationWithNonconformingQuads2D(8, GaussLegendre2D.GetQuadratureWithOrder(2, 2));
            var bulkIntegration = new CrackElementIntegrationStrategy(
                enrichedIntegration, enrichedIntegration, enrichedIntegration);
            var factory = new XCrackElementFactory2D(material, thickness, bulkIntegration);
            var elements = new XCrackElement2D[3];
            elements[0] = factory.CreateElement(0, CellType.Quad4, new XNode[] { nodes[0], nodes[1], nodes[2], nodes[3] });
            elements[1] = factory.CreateElement(1, CellType.Quad4, new XNode[] { nodes[1], nodes[4], nodes[7], nodes[2] });
            elements[2] = factory.CreateElement(2, CellType.Quad4, new XNode[] { nodes[4], nodes[5], nodes[6], nodes[7] });

            return (nodes, elements);
        }

        /// <summary>
        /// The order of dofs in node major, enrichment medium, axis minor
        /// </summary>
        /// <param name="elementID"></param>
        /// <returns></returns>
        private static Matrix GetExpectedStiffness(int elementID)
        {
            if (elementID == 0)
            {
                return 1E6 * Matrix.CreateFromArray(new double[,]
                {
                    {  1.154,  0.481, -0.769,  0.096, 0.000, 0.000, -0.577, -0.481, 0.000, 0.000,  0.192, -0.096 },
                    {  0.481,  1.154, -0.096,  0.192, 0.000, 0.000, -0.481, -0.577, 0.000, 0.000,  0.096, -0.769 },
                    { -0.769, -0.096,  1.154, -0.481, 0.000, 0.000,  0.192,  0.096, 0.000, 0.000, -0.577,  0.481 },
                    {  0.096,  0.192, -0.481,  1.154, 0.000, 0.000, -0.096, -0.769, 0.000, 0.000,  0.481, -0.577 },
                    {  0.000,  0.000,  0.000,  0.000, 0.000, 0.000,  0.000,  0.000, 0.000, 0.000,  0.000,  0.000 },
                    {  0.000,  0.000,  0.000,  0.000, 0.000, 0.000,  0.000,  0.000, 0.000, 0.000,  0.000,  0.000 },
                    { -0.577, -0.481,  0.192, -0.096, 0.000, 0.000,  1.154,  0.481, 0.000, 0.000, -0.769,  0.096 },
                    { -0.481, -0.577,  0.096, -0.769, 0.000, 0.000,  0.481,  1.154, 0.000, 0.000, -0.096,  0.192 },
                    {  0.000,  0.000,  0.000,  0.000, 0.000, 0.000,  0.000,  0.000, 0.000, 0.000,  0.000,  0.000 },
                    {  0.000,  0.000,  0.000,  0.000, 0.000, 0.000,  0.000,  0.000, 0.000, 0.000,  0.000,  0.000 },
                    {  0.192,  0.096, -0.577,  0.481, 0.000, 0.000, -0.769, -0.096, 0.000, 0.000,  1.154, -0.481 },
                    { -0.096, -0.769,  0.481, -0.577, 0.000, 0.000,  0.096,  0.192, 0.000, 0.000, -0.481,  1.154 }
                });
            }
            else if (elementID == 1)
            {
                return 1E6 * Matrix.CreateFromArray(new double[,]
                {
                    {  1.154,  0.481,  0.962,  0.240, -0.769,  0.096,  0.769,  0.144, -0.577, -0.481,  0.577,  0.433,  0.192, -0.096,  0.385, -0.048 },
                    {  0.481,  1.154,  0.240,  0.481, -0.096,  0.192,  0.337, -0.192, -0.481, -0.577,  0.529,  0.577,  0.096, -0.769,  0.048, -0.096 },
                    {  0.962,  0.240,  1.923,  0.481, -0.769,  0.337,  0.000,  0.000, -0.577, -0.529,  0.000,  0.000,  0.385, -0.048,  0.769, -0.096 },
                    {  0.240,  0.481,  0.481,  0.962,  0.144,  0.192,  0.000,  0.000, -0.433, -0.577,  0.000,  0.000,  0.048, -0.096,  0.096, -0.192 },
                    { -0.769, -0.096, -0.769,  0.144,  1.154, -0.481, -0.962,  0.240,  0.192,  0.096, -0.385, -0.048, -0.577,  0.481, -0.577,  0.433 },
                    {  0.096,  0.192,  0.337,  0.192, -0.481,  1.154,  0.240, -0.481, -0.096, -0.769,  0.048,  0.096,  0.481, -0.577,  0.529, -0.577 },
                    {  0.769,  0.337,  0.000,  0.000, -0.962,  0.240,  1.923, -0.481, -0.385, -0.048,  0.769,  0.096,  0.577, -0.529,  0.000,  0.000 },
                    {  0.144, -0.192,  0.000,  0.000,  0.240, -0.481, -0.481,  0.962,  0.048,  0.096, -0.096, -0.192, -0.433,  0.577,  0.000,  0.000 },
                    { -0.577, -0.481, -0.577, -0.433,  0.192, -0.096, -0.385,  0.048,  1.154,  0.481, -0.962, -0.240, -0.769,  0.096, -0.769, -0.144 },
                    { -0.481, -0.577, -0.529, -0.577,  0.096, -0.769, -0.048,  0.096,  0.481,  1.154, -0.240, -0.481, -0.096,  0.192, -0.337,  0.192 },
                    {  0.577,  0.529,  0.000,  0.000, -0.385,  0.048,  0.769, -0.096, -0.962, -0.240,  1.923,  0.481,  0.769, -0.337,  0.000,  0.000 },
                    {  0.433,  0.577,  0.000,  0.000, -0.048,  0.096,  0.096, -0.192, -0.240, -0.481,  0.481,  0.962, -0.144, -0.192,  0.000,  0.000 },
                    {  0.192,  0.096,  0.385,  0.048, -0.577,  0.481,  0.577, -0.433, -0.769, -0.096,  0.769, -0.144,  1.154, -0.481,  0.962, -0.240 },
                    { -0.096, -0.769, -0.048, -0.096,  0.481, -0.577, -0.529,  0.577,  0.096,  0.192, -0.337, -0.192, -0.481,  1.154, -0.240,  0.481 },
                    {  0.385,  0.048,  0.769,  0.096, -0.577,  0.529,  0.000,  0.000, -0.769, -0.337,  0.000,  0.000,  0.962, -0.240,  1.923, -0.481 },
                    { -0.048, -0.096, -0.096, -0.192,  0.433, -0.577,  0.000,  0.000, -0.144,  0.192,  0.000,  0.000, -0.240,  0.481, -0.481,  0.962 }
                });
            }
            else if (elementID == 2)
            {
                return 1E6 * Matrix.CreateFromArray(new double[,]
                {
                    {  1.154,  0.481, 0.000, 0.000, -0.769,  0.096, -0.577, -0.481,  0.192, -0.096, 0.000, 0.000 },
                    {  0.481,  1.154, 0.000, 0.000, -0.096,  0.192, -0.481, -0.577,  0.096, -0.769, 0.000, 0.000 },
                    {  0.000,  0.000, 0.000, 0.000,  0.000,  0.000,  0.000,  0.000,  0.000,  0.000, 0.000, 0.000 },
                    {  0.000,  0.000, 0.000, 0.000,  0.000,  0.000,  0.000,  0.000,  0.000,  0.000, 0.000, 0.000 },
                    { -0.769, -0.096, 0.000, 0.000,  1.154, -0.481,  0.192,  0.096, -0.577,  0.481, 0.000, 0.000 },
                    {  0.096,  0.192, 0.000, 0.000, -0.481,  1.154, -0.096, -0.769,  0.481, -0.577, 0.000, 0.000 },
                    { -0.577, -0.481, 0.000, 0.000,  0.192, -0.096,  1.154,  0.481, -0.769,  0.096, 0.000, 0.000 },
                    { -0.481, -0.577, 0.000, 0.000,  0.096, -0.769,  0.481,  1.154, -0.096,  0.192, 0.000, 0.000 },
                    {  0.192,  0.096, 0.000, 0.000, -0.577,  0.481, -0.769, -0.096,  1.154, -0.481, 0.000, 0.000 },
                    { -0.096, -0.769, 0.000, 0.000,  0.481, -0.577,  0.096,  0.192, -0.481,  1.154, 0.000, 0.000 },
                    {  0.000,  0.000, 0.000, 0.000,  0.000,  0.000,  0.000,  0.000,  0.000,  0.000, 0.000, 0.000 },
                    {  0.000,  0.000, 0.000, 0.000,  0.000,  0.000,  0.000,  0.000,  0.000,  0.000, 0.000, 0.000 }
                });
            }
            else throw new ArgumentException();
            
        }

        private static Vector GetExpectedSolution()
        {
            return Vector.CreateFromArray(new double[]
            {
                /*n0*/0, 0, /*n1*/0, 0, 0.5, 0, /*n2*/0, 0, 0.5, 0, /*n3*/ 0, 0,
                /*n4*/1, 0, 0.5, 0, /*n5*/1, 0, /*n6*/1, 0, /*n7*/1, 0, 0.5, 0
            });
        }

        private static void AddSubmatrix(Matrix globalMatrix, IMatrix submatrix, int[] subToGlobalMatrix)
        {
            for (int j = 0; j < submatrix.NumColumns; ++j)
            {
                for (int i = 0; i < submatrix.NumRows; ++i)
                {
                    globalMatrix[subToGlobalMatrix[i], subToGlobalMatrix[j]] = submatrix[i, j];
                }
            }
        }

        private class Crack : ICrack, IXGeometryDescription
        {
            private readonly Line2D line;

            public Crack(double[] point0, double[] point1)
            {
                this.line = new Line2D(point0, point1);
                IntersectedElementIDs = new HashSet<int>();
                IntersectedElementIDs.Add(1);
            }

            public TipCoordinateSystem TipSystem => null;

            public HashSet<int> IntersectedElementIDs { get; }

            public HashSet<int> TipElementIDs => new HashSet<int>();

            public int ID => 0;

            public HashSet<IXCrackElement> ConformingElements => throw new NotImplementedException();

            public CrackStepEnrichment CrackBodyEnrichment => throw new NotImplementedException();

            public IXGeometryDescription CrackGeometry => throw new NotImplementedException();

            public IReadOnlyList<ICrackTipEnrichment> CrackTipEnrichments => throw new NotImplementedException();

            public HashSet<IXCrackElement> IntersectedElements => throw new NotImplementedException();

            public double[] TipCoordinates => throw new NotImplementedException();

            public HashSet<IXCrackElement> TipElements => throw new NotImplementedException();

            public void InteractWithMesh()
            {
                throw new NotImplementedException();
            }

            public IElementCrackInteraction Intersect(IXFiniteElement element)
            {
                throw new NotImplementedException();
            }

            public void Propagate(Dictionary<int, Vector> subdomainFreeDisplacements)
            {
                throw new NotImplementedException();
            }

            public double SignedDistanceOf(XNode node)
            {
                return line.SignedDistanceOf(node.Coordinates);
            }

            public double SignedDistanceOf(XPoint point)
            {
                bool hasCartesian = point.Coordinates.TryGetValue(CoordinateSystem.GlobalCartesian, out double[] coords);
                if (!hasCartesian)
                {
                    coords = point.MapCoordinates(point.ShapeFunctions, point.Element.Nodes);
                    point.Coordinates[CoordinateSystem.GlobalCartesian] = coords;
                }
                return line.SignedDistanceOf(coords);
            }
        }
    }
}
