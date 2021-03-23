using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ISAAR.MSolve.Discretization;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.Discretization.Mesh;
using ISAAR.MSolve.Discretization.Mesh.Generation;
using ISAAR.MSolve.Discretization.Mesh.Generation.Custom;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Entities;
using MGroup.XFEM.Geometry.Mesh;
using MGroup.XFEM.Integration;
using MGroup.XFEM.Integration.Quadratures;
using MGroup.XFEM.Materials;

namespace MGroup.XFEM.Tests.Utilities
{
    public static class Models
    {
        public static void AddNodesElements<TElement>(
            XModel<TElement> model, UniformMesh2D mesh, IXElementFactory<TElement> factory) 
            where TElement : class, IXFiniteElement
        {
            int subdomainID = model.Subdomains.First().Key;

            // Nodes
            for (int nodeID = 0; nodeID < mesh.NumNodesTotal; ++nodeID)
            {
                int[] nodeIdx = mesh.GetNodeIdx(nodeID);
                double[] coords = mesh.GetNodeCoordinates(nodeIdx);
                model.XNodes.Add(new XNode(nodeID, coords));
            }

            // Elements
            for (int elemID = 0; elemID < mesh.NumElementsTotal; ++elemID)
            {
                var nodes = new List<XNode>();
                int[] elemIdx = mesh.GetElementIdx(elemID);
                foreach (int n in mesh.GetElementConnectivity(elemIdx))
                {
                    nodes.Add(model.XNodes[n]);
                }
                var element = factory.CreateElement(elemID, CellType.Quad4, nodes);
                model.Elements.Add(element);
                model.Subdomains[subdomainID].Elements.Add(element);
            }
        }

        public static void ApplyBCsCantileverTension(XModel<IXMultiphaseElement> model)
        {
            // Boundary conditions
            double meshTol = 1E-7;

            // Left side: Ux=Uy=Uz=0
            double minX = model.XNodes.Select(n => n.X).Min();
            foreach (var node in model.XNodes.Where(n => Math.Abs(n.X - minX) <= meshTol))
            {
                node.Constraints.Add(new Constraint() { DOF = StructuralDof.TranslationX, Amount = 0 });
                node.Constraints.Add(new Constraint() { DOF = StructuralDof.TranslationY, Amount = 0 });
                if (model.Dimension == 3)
                {
                    node.Constraints.Add(new Constraint() { DOF = StructuralDof.TranslationZ, Amount = 0 });
                }
            }

            // Right side: Fx = 100
            double maxX = model.XNodes.Select(n => n.X).Max();
            XNode[] rightSideNodes = model.XNodes.Where(n => Math.Abs(n.X - maxX) <= meshTol).ToArray();
            double load = 1.0 / rightSideNodes.Length;
            foreach (var node in rightSideNodes)
            {
                model.NodalLoads.Add(new NodalLoad(node, StructuralDof.TranslationX, load));
            }
        }

        public static void ApplyBCsTemperatureDiffAlongX(IXModel model, double temperatureAtMinX, double temperatureAtMaxX)
        {
            // Boundary conditions
            double meshTol = 1E-7;

            // Left side
            double minX = model.XNodes.Select(n => n.X).Min();
            foreach (var node in model.XNodes.Where(n => Math.Abs(n.X - minX) <= meshTol))
            {
                node.Constraints.Add(new Constraint() { DOF = ThermalDof.Temperature, Amount = temperatureAtMinX });
            }

            // Right side
            double maxX = model.XNodes.Select(n => n.X).Max();
            foreach (var node in model.XNodes.Where(n => Math.Abs(n.X - maxX) <= meshTol))
            {
                node.Constraints.Add(new Constraint() { DOF = ThermalDof.Temperature, Amount = temperatureAtMaxX });
            }
        }

        public static XModel<IXMultiphaseElement> CreateQuad4Model(double[] minCoords, double[] maxCoords, double thickness,
            int[] numElements, int bulkIntegrationOrder, int boundaryIntegrationOrder, IThermalMaterialField materialField,
            bool cohesiveInterfaces)
        {
            var model = new XModel<IXMultiphaseElement>(2);
            model.Subdomains[0] = new XSubdomain(0);

            // Mesh generation
            var meshGen = new UniformMeshGenerator2D<XNode>(minCoords[0], minCoords[1], maxCoords[0], maxCoords[1],
                numElements[0], numElements[1]);
            (IReadOnlyList<XNode> nodes, IReadOnlyList<CellConnectivity<XNode>> cells) =
                meshGen.CreateMesh((id, x, y, z) => new XNode(id, new double[] { x, y, z }));

            // Nodes
            foreach (XNode node in nodes) model.XNodes.Add(node);

            // Integration
            var stdQuadrature = GaussLegendre2D.GetQuadratureWithOrder(bulkIntegrationOrder, bulkIntegrationOrder);
            var subcellQuadrature = TriangleQuadratureSymmetricGaussian.Order2Points3;
            var integrationBulk = new IntegrationWithConformingSubtriangles2D(subcellQuadrature);

            // Elements
            var elemFactory = new XThermalElement2DFactory(
                materialField, thickness, integrationBulk, boundaryIntegrationOrder, cohesiveInterfaces);
            for (int e = 0; e < cells.Count; ++e)
            {
                XThermalElement2D element = elemFactory.CreateElement(e, CellType.Quad4, cells[e].Vertices);
                model.Elements.Add(element);
                model.Subdomains[0].Elements.Add(element);
            }
           
            // Boundary conditions
            double meshTol = 1E-7;

            // Left side: T = +100
            double minX = model.XNodes.Select(n => n.X).Min();
            foreach (var node in model.XNodes.Where(n => Math.Abs(n.X - minX) <= meshTol))
            {
                node.Constraints.Add(new Constraint() { DOF = ThermalDof.Temperature, Amount = +100 });
            }

            // Right side: T = 100
            double maxX = model.XNodes.Select(n => n.X).Max();
            foreach (var node in model.XNodes.Where(n => Math.Abs(n.X - maxX) <= meshTol))
            {
                node.Constraints.Add(new Constraint() { DOF = ThermalDof.Temperature, Amount = -100 });
            }

            model.ConnectDataStructures();
            return model;
        }

        public static XModel<IXMultiphaseElement> CreateQuad4Model(double[] minCoords, double[] maxCoords, double thickness,
            int[] numElements, int bulkIntegrationOrder, int boundaryIntegrationOrder, IStructuralMaterialField materialField,
            bool cohesiveInterfaces)
        {
            var model = new XModel<IXMultiphaseElement>(2);
            model.Subdomains[0] = new XSubdomain(0);

            // Mesh generation
            var meshGen = new UniformMeshGenerator2D<XNode>(minCoords[0], minCoords[1], maxCoords[0], maxCoords[1],
                numElements[0], numElements[1]);
            (IReadOnlyList<XNode> nodes, IReadOnlyList<CellConnectivity<XNode>> cells) =
                meshGen.CreateMesh((id, x, y, z) => new XNode(id, new double[] { x, y }));

            // Nodes
            foreach (XNode node in nodes) model.XNodes.Add(node);

            // Integration
            //var stdQuadrature = GaussLegendre2D.GetQuadratureWithOrder(bulkIntegrationOrder, bulkIntegrationOrder);
            var subcellQuadrature = TriangleQuadratureSymmetricGaussian.Order2Points3;
            var integrationBulk = new IntegrationWithConformingSubtriangles2D(subcellQuadrature);

            // Elements
            var elemFactory = new XMultiphaseStructuralElementFactory2D(
                materialField, thickness, integrationBulk, boundaryIntegrationOrder, cohesiveInterfaces);
            for (int e = 0; e < cells.Count; ++e)
            {
                IXStructuralMultiphaseElement element = elemFactory.CreateElement(e, CellType.Quad4, cells[e].Vertices);
                model.Elements.Add(element);
                model.Subdomains[0].Elements.Add(element);
            }

            return model;
        }

        public static XModel<IXMultiphaseElement> CreateHexa8Model(double[] minCoords, double[] maxCoords,int[] numElements,
            int bulkIntegrationOrder, int boundaryIntegrationOrder, IThermalMaterialField materialField, bool cohesiveInterfaces)
        {
            var model = new XModel<IXMultiphaseElement>(3);
            model.Subdomains[0] = new XSubdomain(0);

            // Mesh generation
            var meshGen = new UniformMeshGenerator3D<XNode>(minCoords[0], minCoords[1], minCoords[2], maxCoords[0], maxCoords[1], 
                maxCoords[2], numElements[0], numElements[1], numElements[2]);
            (IReadOnlyList<XNode> nodes, IReadOnlyList<CellConnectivity<XNode>> cells) =
                meshGen.CreateMesh((id, x, y, z) => new XNode(id, new double[] { x, y, z }));

            // Nodes
            foreach (XNode node in nodes) model.XNodes.Add(node);

            // Integration
            var subcellQuadrature = TetrahedronQuadrature.Order2Points4;
            var integrationBulk = new IntegrationWithConformingSubtetrahedra3D(subcellQuadrature);

            // Elements
            var elemFactory = new XThermalElement3DFactory(
                materialField, integrationBulk, boundaryIntegrationOrder, cohesiveInterfaces);
            for (int e = 0; e < cells.Count; ++e)
            {
                XThermalElement3D element = elemFactory.CreateElement(e, CellType.Hexa8, cells[e].Vertices);
                model.Elements.Add(element);
                model.Subdomains[0].Elements.Add(element);
            }

            return model;
        }

        public static XModel<IXMultiphaseElement> CreateHexa8Model(double[] minCoords, double[] maxCoords,
            int[] numElements, int bulkIntegrationOrder, int boundaryIntegrationOrder, IStructuralMaterialField materialField,
            bool cohesiveInterfaces)
        {
            var model = new XModel<IXMultiphaseElement>(3);
            model.Subdomains[0] = new XSubdomain(0);

            // Mesh generation
            var meshGen = new UniformMeshGenerator3D<XNode>(minCoords[0], minCoords[1], minCoords[2], maxCoords[0], maxCoords[1],
                 maxCoords[2], numElements[0], numElements[1], numElements[2]);
            (IReadOnlyList<XNode> nodes, IReadOnlyList<CellConnectivity<XNode>> cells) =
                meshGen.CreateMesh((id, x, y, z) => new XNode(id, new double[] { x, y, z }));

            // Nodes
            foreach (XNode node in nodes) model.XNodes.Add(node);

            // Integration
            var subcellQuadrature = TetrahedronQuadrature.Order2Points4;
            var integrationBulk = new IntegrationWithConformingSubtetrahedra3D(subcellQuadrature);

            // Elements
            var elemFactory = new XMultiphaseStructuralElementFactory3D(
                materialField, integrationBulk, boundaryIntegrationOrder, cohesiveInterfaces);
            for (int e = 0; e < cells.Count; ++e)
            {
                IXStructuralMultiphaseElement element = elemFactory.CreateElement(e, CellType.Hexa8, cells[e].Vertices);
                model.Elements.Add(element);
                model.Subdomains[0].Elements.Add(element);
            }

            return model;
        }

    }
}
