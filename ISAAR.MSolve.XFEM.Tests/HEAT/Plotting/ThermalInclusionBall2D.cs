﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ISAAR.MSolve.Analyzers;
using ISAAR.MSolve.Discretization;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Discretization.Integration.Quadratures;
using ISAAR.MSolve.Discretization.Mesh.Generation;
using ISAAR.MSolve.Discretization.Mesh.Generation.Custom;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.Geometry.Shapes;
using ISAAR.MSolve.Materials;
using ISAAR.MSolve.Problems;
using ISAAR.MSolve.Solvers.Direct;
using ISAAR.MSolve.XFEM.Thermal.Elements;
using ISAAR.MSolve.XFEM.Thermal.Entities;
using ISAAR.MSolve.XFEM.Thermal.Integration;
using ISAAR.MSolve.XFEM.Thermal.LevelSetMethod;
using ISAAR.MSolve.XFEM.Thermal.MaterialInterface;
using ISAAR.MSolve.XFEM.Thermal.Materials;
using ISAAR.MSolve.XFEM.Thermal.Output.Fields;
using ISAAR.MSolve.XFEM.Thermal.Output.Mesh;
using ISAAR.MSolve.XFEM.Thermal.Output.Writers;

namespace ISAAR.MSolve.XFEM.Tests.HEAT.Plotting
{
    public static class ThermalInclusionBall2D
    {
        private const string pathHeatFlux = @"C:\Users\Serafeim\Desktop\HEAT\Ball\heat_flux.vtk";
        private const string pathLevelSets = @"C:\Users\Serafeim\Desktop\HEAT\Ball\level_sets.vtk";
        private const string pathMesh = @"C:\Users\Serafeim\Desktop\HEAT\Ball\mesh.vtk";
        private const string pathTemperature = @"C:\Users\Serafeim\Desktop\HEAT\Ball\temperature.vtk";

        private const int numElementsX = 20, numElementsY = 20;
        private const int subdomainID = 0;

        public static void PlotLevelSets()
        {
            // Create model and LSM
            (XModel model, SimpleLsmCurve2D interfaceLSM) = CreateModel(numElementsX, numElementsY);
            InitializeLSM(model, interfaceLSM);

            // Plot mesh and level sets
            using (var writer = new VtkFileWriter(pathLevelSets))
            {
                var levelSetField = new LevelSetField(model, interfaceLSM);
                writer.WriteMesh(levelSetField.Mesh);
                writer.WriteScalarField("inclusion_level_set", levelSetField.Mesh, levelSetField.CalcValuesAtVertices());
            }
        }

        public static void PlotConformingMesh()
        {
            // Create model and LSM
            (XModel model, SimpleLsmCurve2D interfaceLSM) = CreateModel(numElementsX, numElementsY);
            InitializeLSM(model, interfaceLSM);

            // Plot mesh and level sets
            using (var writer = new VtkFileWriter(pathMesh))
            {
                var mesh = new ConformingOutputMesh2D(model.Nodes, model.Elements, interfaceLSM);
                writer.WriteMesh(mesh);
            }
        }

        public static void PlotTemperature()
        {
            // Create model and LSM
            (XModel model, SimpleLsmCurve2D interfaceLSM) = CreateModel(numElementsX, numElementsY);
            InitializeLSM(model, interfaceLSM);
            ApplyEnrichments(model, interfaceLSM);

            // Run the analysis
            SkylineSolver solver = new SkylineSolver.Builder().BuildSolver(model);
            var problem = new ProblemThermalSteadyState(model, solver);
            var linearAnalyzer = new LinearAnalyzer(model, solver, problem);
            var staticAnalyzer = new StaticAnalyzer(model, solver, problem, linearAnalyzer);
            staticAnalyzer.Initialize();
            staticAnalyzer.Solve();

            // Plot mesh and level sets
            using (var writer = new VtkFileWriter(pathLevelSets))
            {
                var levelSetField = new LevelSetField(model, interfaceLSM);
                writer.WriteMesh(levelSetField.Mesh);
                writer.WriteScalarField("inclusion_level_set", levelSetField.Mesh, levelSetField.CalcValuesAtVertices());
            }
        }

        private static (XModel, SimpleLsmCurve2D) CreateModel(int numElementsX, int numElementsY)
        {
            var model = new XModel();
            model.Subdomains[subdomainID] = new XSubdomain(subdomainID);


            // Materials
            double thickness = 1.0;
            double density = 1.0;
            double specificHeat = 1.0, conductivity1 = 1.0, conductivity2 = 1000.0;
            var interfaceLSM = new SimpleLsmCurve2D(thickness);
            var materialPos = new ThermalMaterial(density, specificHeat, conductivity1);
            var materialNeg = new ThermalMaterial(density, specificHeat, conductivity2);
            var materialField = new ThermalMultiMaterialField2D(materialPos, materialNeg, interfaceLSM);

            // Mesh generation
            var meshGen = new UniformMeshGenerator2D<XNode>(-1.0, -1.0, 1.0, 1.0, numElementsX, numElementsY);
            (IReadOnlyList<XNode> nodes, IReadOnlyList<CellConnectivity<XNode>> cells) =
                meshGen.CreateMesh((id, x, y, z) => new XNode(id, x, y));

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
                var element = elementFactory.CreateElement(e, cells[e].CellType, cells[e].Vertices);
                model.Elements.Add(element);
                model.Subdomains[subdomainID].Elements.Add(element);
            }

            ApplyBoundaryConditions(model);

            return (model, interfaceLSM);
        }

        private static void ApplyBoundaryConditions(XModel model)
        {
            double meshTol = 1E-7;

            // Left side: T = 100
            double minX = model.Nodes.Select(n => n.X).Min();
            foreach (var node in model.Nodes.Where(n => Math.Abs(n.X - minX) <= meshTol))
            {
                node.Constraints.Add(new Constraint() { DOF = ThermalDof.Temperature, Amount = 100 });
            }

            // Right side: T = 100
            double maxX = model.Nodes.Select(n => n.X).Max();
            foreach (var node in model.Nodes.Where(n => Math.Abs(n.X - maxX) <= meshTol))
            {
                node.Constraints.Add(new Constraint() { DOF = ThermalDof.Temperature, Amount = 100 });
            }

            // Bottom side: T = 100
            double minY = model.Nodes.Select(n => n.Y).Min();
            foreach (var node in model.Nodes.Where(n => Math.Abs(n.Y - minY) <= meshTol))
            {
                node.Constraints.Add(new Constraint() { DOF = ThermalDof.Temperature, Amount = 100 });
            }

            // Top side: T = 100
            double maxY = model.Nodes.Select(n => n.Y).Max();
            foreach (var node in model.Nodes.Where(n => Math.Abs(n.Y - maxY) <= meshTol))
            {
                node.Constraints.Add(new Constraint() { DOF = ThermalDof.Temperature, Amount = 100 });
            }
        }

        private static void ApplyEnrichments(XModel model, SimpleLsmCurve2D interfaceLSM)
        {
            double interfaceResistance = 0.01;
            var materialInterface = new SingleMaterialInterface(interfaceLSM, model.Elements.Select(e => (XThermalElement2D)e),
                interfaceResistance);
            materialInterface.ApplyEnrichments();
        }

        private static void InitializeLSM(XModel model, SimpleLsmCurve2D interfaceLSM)
        {
            var initialGeometry = new Circle2D(new CartesianPoint(0.0, 0.0), 0.5);
            interfaceLSM.InitializeGeometry(model.Nodes, initialGeometry);
        }
    }
}
