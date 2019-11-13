using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using ISAAR.MSolve.Analyzers;
using ISAAR.MSolve.Discretization;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Discretization.Integration.Quadratures;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.Discretization.Mesh.Generation;
using ISAAR.MSolve.Discretization.Mesh.Generation.Custom;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.Geometry.Shapes;
using ISAAR.MSolve.LinearAlgebra.Output;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Materials;
using ISAAR.MSolve.Problems;
using ISAAR.MSolve.Solvers.Direct;
using ISAAR.MSolve.XFEM.Thermal.Elements;
using ISAAR.MSolve.XFEM.Thermal.Entities;
using ISAAR.MSolve.XFEM.Thermal.Integration;
using ISAAR.MSolve.XFEM.Thermal.LevelSetMethod;
using ISAAR.MSolve.XFEM.Thermal.MaterialInterface;
using ISAAR.MSolve.XFEM.Thermal.Materials;
using Xunit;

namespace ISAAR.MSolve.XFEM.Tests.HEAT
{
    public static class Yvonnet2011Example1_2D
    {
        private const double density = 1.0;
        private const double conductivity1 = 0.1, conductivity2 = 1.0; // W/m K
        private const double specificHeat = 1.0;
        private const double thickness = 1.0;
        private const int subdomainID = 0;
        private const double leftT = 0.0, rightT = 1.0;
        private const double interfaceResistance = 1.0;

        [Fact]
        public static void Run()
        {
            int numElementsX = 3;
            int numElementsY = 1;

            (XModel model, SimpleLsmClosedCurve2D interfaceLSM) = CreateModel(numElementsX, numElementsY);
            ApplyMaterialInterface(model, interfaceLSM);

            SkylineSolver solver = new SkylineSolver.Builder().BuildSolver(model);
            var problem = new ProblemThermalSteadyState(model, solver);
            var linearAnalyzer = new LinearAnalyzer(model, solver, problem);
            var staticAnalyzer = new StaticAnalyzer(model, solver, problem, linearAnalyzer);

            staticAnalyzer.Initialize();
            staticAnalyzer.Solve();

            // Write results
            XSubdomain subdomain = model.Subdomains[subdomainID];
            IVectorView temperatures = solver.LinearSystems[subdomainID].Solution;
            foreach ((INode node, IDofType dof, int idx) in subdomain.FreeDofOrdering.FreeDofs)
            {
                Debug.WriteLine($"Node {node}, dof {dof}: temperature = {temperatures[idx]}");
            }
        }

        private static (XModel, SimpleLsmClosedCurve2D) CreateModel(int numElementsX, int numElementsY)
        {
            var model = new XModel();
            model.Subdomains[subdomainID] = new XSubdomain(subdomainID);

            int nodeIdStart = model.Nodes.Count;
            int elementIdStart = model.Elements.Count;

            // Materials
            var interfaceLSM = new SimpleLsmClosedCurve2D(thickness);
            var materialPos = new ThermalMaterial(density, specificHeat, conductivity1);
            var materialNeg = new ThermalMaterial(density, specificHeat, conductivity2);
            var materialField = new ThermalBiMaterialField2D(materialPos, materialNeg, interfaceLSM);

            // Mesh generation
            var meshGen = new UniformMeshGenerator2D<XNode>(-1.0, -1.0, 1.0, 1.0, numElementsX, numElementsY);
            (IReadOnlyList<XNode> nodes, IReadOnlyList<CellConnectivity<XNode>> cells) =
                meshGen.CreateMesh((id, x, y, z) => new XNode(nodeIdStart + id, x, y));

            // Nodes
            foreach (XNode node in nodes) model.Nodes.Add(node);

            // Elements
            var integrationForConductivity = new RectangularSubgridIntegration2D<XThermalElement2D>(8,
                GaussLegendre2D.GetQuadratureWithOrder(2, 2));
            int numGaussPointsInterface = 2;
            var elementFactory = new XThermalElement2DFactory(materialField, thickness, integrationForConductivity,
                numGaussPointsInterface);
            for (int e = 0; e < cells.Count; ++e)
            {
                var element = elementFactory.CreateElement(elementIdStart + e, cells[e].CellType, cells[e].Vertices);
                model.Elements.Add(element);
                model.Subdomains[subdomainID].Elements.Add(element);
            }

            ApplyBoundaryConditions(model);

            return (model, interfaceLSM);
        }

        private static void ApplyBoundaryConditions(XModel model)
        {
            double meshTol = 1E-7;

            // Left side: T = 0
            double minX = model.Nodes.Select(n => n.X).Min();
            foreach (var node in model.Nodes.Where(n => Math.Abs(n.X - minX) <= meshTol))
            {
                node.Constraints.Add(new Constraint() { DOF = ThermalDof.Temperature, Amount = leftT});
            }

            // Right side: T = 1
            double maxX = model.Nodes.Select(n => n.X).Max();
            foreach (var node in model.Nodes.Where(n => Math.Abs(n.X - maxX) <= meshTol))
            {
                node.Constraints.Add(new Constraint() { DOF = ThermalDof.Temperature, Amount = rightT });
            }
        }

        private static void ApplyMaterialInterface(XModel model, SimpleLsmClosedCurve2D interfaceLSM)
        {
            // Mesh - geometry interactions
            var initialGeometry = new PolyLine2D(new CartesianPoint(0.0, -1.0), new CartesianPoint(0.0, 1.0));
            interfaceLSM.InitializeGeometry(model.Nodes, initialGeometry);

            // Enrichments
            var materialInterface = new SingleMaterialInterface(interfaceLSM, model.Elements.Select(e => (XThermalElement2D)e),
                interfaceResistance);
            materialInterface.ApplyEnrichments();
        }
    }
}
