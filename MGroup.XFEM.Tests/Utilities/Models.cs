using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ISAAR.MSolve.Discretization;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Discretization.Mesh;
using ISAAR.MSolve.Discretization.Mesh.Generation;
using ISAAR.MSolve.Discretization.Mesh.Generation.Custom;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Entities;
using MGroup.XFEM.Integration;
using MGroup.XFEM.Integration.Quadratures;
using MGroup.XFEM.Materials;

namespace MGroup.XFEM.Tests.Utilities
{
    public static class Models
    {
        public static XModel<IXMultiphaseElement> CreateQuad4Model(double[] minCoords, double[] maxCoords, double thickness,
            int[] numElements, int bulkIntegrationOrder, int boundaryIntegrationOrder, IThermalMaterialField materialField)
        {
            var model = new XModel<IXMultiphaseElement>();
            model.Subdomains[0] = new XSubdomain(0);

            // Mesh generation
            var meshGen = new UniformMeshGenerator2D<XNode>(minCoords[0], minCoords[1], maxCoords[0], maxCoords[1],
                numElements[0], numElements[1]);
            (IReadOnlyList<XNode> nodes, IReadOnlyList<CellConnectivity<XNode>> cells) =
                meshGen.CreateMesh((id, x, y, z) => new XNode(id, new double[] { x, y, z }));

            // Nodes
            foreach (XNode node in nodes) model.Nodes.Add(node);

            // Integration
            var stdQuadrature = GaussLegendre2D.GetQuadratureWithOrder(bulkIntegrationOrder, bulkIntegrationOrder);
            var subcellQuadrature = TriangleQuadratureSymmetricGaussian.Order2Points3;
            var integrationBulk = new IntegrationWithConformingSubtriangles2D(stdQuadrature, subcellQuadrature);

            // Elements
            var elemFactory = new XThermalElement2DFactory(materialField, thickness, integrationBulk, boundaryIntegrationOrder);
            for (int e = 0; e < cells.Count; ++e)
            {
                XThermalElement2D element = elemFactory.CreateElement(e, CellType.Quad4, cells[e].Vertices);
                model.Elements.Add(element);
                model.Subdomains[0].Elements.Add(element);
            }
           
            // Boundary conditions
            double meshTol = 1E-7;

            // Left side: T = +100
            double minX = model.Nodes.Select(n => n.X).Min();
            foreach (var node in model.Nodes.Where(n => Math.Abs(n.X - minX) <= meshTol))
            {
                node.Constraints.Add(new Constraint() { DOF = ThermalDof.Temperature, Amount = +100 });
            }

            // Right side: T = 100
            double maxX = model.Nodes.Select(n => n.X).Max();
            foreach (var node in model.Nodes.Where(n => Math.Abs(n.X - maxX) <= meshTol))
            {
                node.Constraints.Add(new Constraint() { DOF = ThermalDof.Temperature, Amount = -100 });
            }

            model.ConnectDataStructures();
            return model;
        }

        public static XModel<IXMultiphaseElement> CreateHexa8Model(double[] minCoords, double[] maxCoords,int[] numElements,
            int bulkIntegrationOrder, int boundaryIntegrationOrder, IThermalMaterialField materialField)
        {
            var model = new XModel<IXMultiphaseElement>();
            model.Subdomains[0] = new XSubdomain(0);

            // Mesh generation
            var meshGen = new UniformMeshGenerator3D<XNode>(minCoords[0], minCoords[1], minCoords[2], maxCoords[0], maxCoords[1], 
                maxCoords[2], numElements[0], numElements[1], numElements[2]);
            (IReadOnlyList<XNode> nodes, IReadOnlyList<CellConnectivity<XNode>> cells) =
                meshGen.CreateMesh((id, x, y, z) => new XNode(id, new double[] { x, y, z }));

            // Nodes
            foreach (XNode node in nodes) model.Nodes.Add(node);

            // Integration
            var stdQuadrature = GaussLegendre3D.GetQuadratureWithOrder(bulkIntegrationOrder, bulkIntegrationOrder, bulkIntegrationOrder);
            var subcellQuadrature = TetrahedronQuadrature.Order2Points4;
            var integrationBulk = new IntegrationWithConformingSubtetrahedra3D(stdQuadrature, subcellQuadrature);

            // Elements
            var elemFactory = new XThermalElement3DFactory(materialField, integrationBulk, boundaryIntegrationOrder);
            for (int e = 0; e < cells.Count; ++e)
            {
                XThermalElement3D element = elemFactory.CreateElement(e, CellType.Hexa8, cells[e].Vertices);
                model.Elements.Add(element);
                model.Subdomains[0].Elements.Add(element);
            }
            
            // Boundary conditions
            double meshTol = 1E-7;

            // Left side: T = +100
            double minX = model.Nodes.Select(n => n.X).Min();
            foreach (var node in model.Nodes.Where(n => Math.Abs(n.X - minX) <= meshTol))
            {
                node.Constraints.Add(new Constraint() { DOF = ThermalDof.Temperature, Amount = +100 });
            }

            // Right side: T = 100
            double maxX = model.Nodes.Select(n => n.X).Max();
            foreach (var node in model.Nodes.Where(n => Math.Abs(n.X - maxX) <= meshTol))
            {
                node.Constraints.Add(new Constraint() { DOF = ThermalDof.Temperature, Amount = -100 });
            }

            model.ConnectDataStructures();
            return model;
        }

    }
}
