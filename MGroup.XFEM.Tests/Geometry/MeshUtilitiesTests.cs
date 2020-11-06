using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ISAAR.MSolve.Discretization.Mesh;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Entities;
using MGroup.XFEM.Geometry.Mesh;
using MGroup.XFEM.Geometry.Primitives;
using Xunit;

namespace MGroup.XFEM.Tests.Geometry
{
    public static class MeshUtilitiesTests
    {
        private const int subdomainID = 0;

        //TODO: make this a theory: large circle, small circle (1 layer outside start), very small (only start element)
        [Theory]
        [InlineData(0.25, new int[] { 54 })]
        [InlineData(1.00, new int[] { 43, 44, 45, 53, 55, 63, 64, 65 })]
        [InlineData(2.40, new int[] { 32, 33, 34, 35, 36, 42, 46, 52, 56, 62, 66, 72, 73, 74, 75, 76})]
        public static void TestElementsIntersectedByCircle(double radius, int[] expectedElements)
        {
            var model = new XModel<MockElement>();
            model.Subdomains[subdomainID] = new XSubdomain(subdomainID);

            double[] minCoords = { 0, 0 };
            double[] maxCoords = { 10, 10 };
            int[] numElements = { 10, 10 };
            var mesh = new UniformMesh2D(minCoords, maxCoords, numElements);

            // Nodes
            for (int nodeID = 0; nodeID < mesh.NumNodesTotal; ++nodeID)
            {
                int[] nodeIdx = mesh.GetNodeIdx(nodeID);
                double[] coords = mesh.GetNodeCoordinates(nodeIdx);
                model.Nodes.Add(new XNode(nodeID, coords));
            }

            // Elements
            for (int elemID = 0; elemID < mesh.NumElementsTotal; ++elemID)
            {
                var nodes = new List<XNode>();
                int[] elemIdx = mesh.GetElementIdx(elemID);
                foreach (int n in mesh.GetElementConnectivity(elemIdx))
                {
                    nodes.Add(model.Nodes[n]);
                }
                var element = new MockElement(elemID, CellType.Quad4, nodes);
                model.Elements.Add(element);
                model.Subdomains[subdomainID].Elements.Add(element);
            }

            model.ConnectDataStructures();

            var circle = new Circle2D(4.5, 5.5, radius);
            MockElement elementAroundCenter = model.Elements[54];
            var intersectedElements = MeshUtilities.FindElementsIntersectedByCircle(circle, elementAroundCenter);
            int[] intersectedElementIDs = intersectedElements.Select(e => e.ID).OrderBy(id => id).ToArray();
            Assert.True(AreEqual(expectedElements, intersectedElementIDs));
        }

        private static bool AreEqual(int[] expected, int[] computed)
        {
            if (expected.Length != computed.Length) return false;
            for (int i = 0; i < expected.Length; ++i)
            {
                if (expected[i] != computed[i]) return false;
            }
            return true;
        }
    }
}
