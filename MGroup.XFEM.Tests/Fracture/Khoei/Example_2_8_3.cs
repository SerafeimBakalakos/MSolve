using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Discretization.Mesh;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Enrichment;
using MGroup.XFEM.Entities;
using MGroup.XFEM.Geometry.Cracks;
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
            Dictionary<IEnrichment, IDofType[]> enrichedDofs = EnrichNodes(nodes);
            
            elements[1].IdentifyDofs(enrichedDofs);
            elements[1].IdentifyIntegrationPointsAndMaterials();
            IMatrix computedK = elements[1].StiffnessMatrix(null);

            double tol = 1E-13;
            Matrix expectedK = GetExpectedStiffness(1);
            IMatrix computedRoundedK = computedK.DoToAllEntries(round);
            Assert.True(expectedK.Equals(computedRoundedK, tol));
        }


        private static Dictionary<IEnrichment, IDofType[]> EnrichNodes(XNode[] nodes)
        {
            var crack = new InfiniteLineCrack2D(new double[] { 30.0, +40.0 }, new double[] { 30.0, -40.0 });
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

            var material = new HomogeneousMaterialField2D(E, v, planeStress);
            var integrationStrategy = new IntegrationWithNonconformingQuads2D(2, GaussLegendre2D.GetQuadratureWithOrder(2, 2));
            var factory = new XCrackElementFactory2D(material, thickness, integrationStrategy);
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
                throw new NotImplementedException();
                
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
                throw new NotImplementedException();
            }
            else throw new ArgumentException();
            
        }
    }
}
