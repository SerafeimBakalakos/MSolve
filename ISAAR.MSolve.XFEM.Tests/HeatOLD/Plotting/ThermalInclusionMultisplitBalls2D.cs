using System;
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
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Materials;
using ISAAR.MSolve.Problems;
using ISAAR.MSolve.Solvers.Direct;
using ISAAR.MSolve.XFEM.ThermalOLD.Elements;
using ISAAR.MSolve.XFEM.ThermalOLD.Entities;
using ISAAR.MSolve.XFEM.ThermalOLD.Integration;
using ISAAR.MSolve.XFEM.ThermalOLD.Curves;
using ISAAR.MSolve.XFEM.ThermalOLD.MaterialInterface;
using ISAAR.MSolve.XFEM.ThermalOLD.Materials;
using ISAAR.MSolve.XFEM.ThermalOLD.Output.Enrichments;
using ISAAR.MSolve.XFEM.ThermalOLD.Output.Fields;
using ISAAR.MSolve.XFEM.ThermalOLD.Output.Mesh;
using ISAAR.MSolve.XFEM.ThermalOLD.Output.Writers;
using ISAAR.MSolve.XFEM.ThermalOLD.Curves.LevelSetMethod;

namespace ISAAR.MSolve.XFEM.Tests.HeatOLD.Plotting
{
    public static class ThermalInclusionMultisplitBalls2D
    {
        private const string outputDirectory = @"C:\Users\Serafeim\Desktop\HEAT\MultisplitBalls";
        private const string pathHeatFlux = @"C:\Users\Serafeim\Desktop\HEAT\MultisplitBalls\heat_flux.vtk";
        private const string pathMesh = @"C:\Users\Serafeim\Desktop\HEAT\MultisplitBalls\conforming_mesh.vtk";
        private const string pathTemperature = @"C:\Users\Serafeim\Desktop\HEAT\MultisplitBalls\temperature.vtk";

        private const double xMin = -1.0, xMax = 1.0, yMin = -1, yMax = 1.0;
        private const double thickness = 1.0;

        // There are 2 or more inclusions in the same element
        private const int numElementsX = 15, numElementsY = 15;
        private const int numBallsX = 2, numBallsY = 1;
        private const double ballRadius = 0.3;

        private const double zeroLevelSetTolerance = 1E-6;
        private const int subdomainID = 0;

        private const double conductivityMatrix = 1E0, conductivityInclusion = 1E4;
        private const double interfaceConductivityLeft = 1E0, interfaceConductivityRight = 1E0;

        public static void PlotLevelSets()
        {
            // Create model and LSM
            (XModel model, GeometricModel2D geometricModel) = CreateModel(numElementsX, numElementsY);
            InitializeLSM(model, geometricModel);

            // Plot original mesh and level sets
            Utilities.PlotInclusionLevelSets(outputDirectory, "level_set", model, geometricModel);
        }

        public static void PlotConformingMesh()
        {
            // Create model and LSM
            (XModel model, GeometricModel2D geometricModel) = CreateModel(numElementsX, numElementsY);
            InitializeLSM(model, geometricModel);

            // Plot conforming mesh and level sets
            using (var writer = new VtkFileWriter(pathMesh))
            {
                var mesh = new ConformingOutputMesh2D(geometricModel, model.Nodes, model.Elements);
                writer.WriteMesh(mesh);
            }

            // Plot original mesh and level sets
            Utilities.PlotInclusionLevelSets(outputDirectory, "level_set", model, geometricModel);
        }

        public static void PlotTemperature()
        {
            // Create model and LSM
            (XModel model, GeometricModel2D geometricModel) = CreateModel(numElementsX, numElementsY);
            InitializeLSM(model, geometricModel);
            ApplyEnrichments(model, geometricModel);

            // Run the analysis
            SkylineSolver solver = new SkylineSolver.Builder().BuildSolver(model);
            var problem = new ProblemThermalSteadyState(model, solver);
            var linearAnalyzer = new LinearAnalyzer(model, solver, problem);
            var staticAnalyzer = new StaticAnalyzer(model, solver, problem, linearAnalyzer);
            staticAnalyzer.Initialize();
            staticAnalyzer.Solve();

            // Plot original mesh and level sets
            Utilities.PlotInclusionLevelSets(outputDirectory, "level_set", model, geometricModel);

            // Plot enrichments
            var enrichmentPlotter = new EnrichmentPlotter(model, geometricModel, outputDirectory);
            enrichmentPlotter.PlotEnrichedNodes();

            // Plot conforming mesh and temperature field
            using (var writer = new VtkFileWriter(pathTemperature))
            {
                var mesh = new ConformingOutputMesh2D(geometricModel, model.Nodes, model.Elements);
                var temperatureField = new TemperatureField2D(model, mesh);
                writer.WriteMesh(mesh);
                IVectorView solution = solver.LinearSystems[subdomainID].Solution;
                writer.WriteScalarField("temperature", mesh, temperatureField.CalcValuesAtVertices(solution));
            }
        }

        public static void PlotTemperatureAndFlux()
        {
            // Create model and LSM
            (XModel model, GeometricModel2D geometricModel) = CreateModel(numElementsX, numElementsY);
            InitializeLSM(model, geometricModel);
            ApplyEnrichments(model, geometricModel);

            // Run the analysis
            SkylineSolver solver = new SkylineSolver.Builder().BuildSolver(model);
            var problem = new ProblemThermalSteadyState(model, solver);
            var linearAnalyzer = new LinearAnalyzer(model, solver, problem);
            var staticAnalyzer = new StaticAnalyzer(model, solver, problem, linearAnalyzer);
            staticAnalyzer.Initialize();
            staticAnalyzer.Solve();

            // Plot original mesh and level sets
            Utilities.PlotInclusionLevelSets(outputDirectory, "level_set", model, geometricModel);

            // Plot conforming mesh, temperature field
            using (var writer = new VtkFileWriter(pathTemperature))
            {
                var mesh = new ConformingOutputMesh2D(geometricModel, model.Nodes, model.Elements);
                var temperatureField = new TemperatureField2D(model, mesh);
                var fluxField = new HeatFluxField2D(model, mesh, zeroLevelSetTolerance);
                writer.WriteMesh(mesh);
                IVectorView solution = solver.LinearSystems[subdomainID].Solution;
                writer.WriteScalarField("temperature", mesh, temperatureField.CalcValuesAtVertices(solution));
                writer.WriteVector2DField("heat_flux", mesh, fluxField.CalcValuesAtVertices(solution));

            }
        }

        private static (XModel, GeometricModel2D) CreateModel(int numElementsX, int numElementsY)
        {
            var model = new XModel();
            model.Subdomains[subdomainID] = new XSubdomain(subdomainID);


            // Materials
            double thickness = 1.0;
            double density = 1.0;
            double specificHeat = 1.0;
            var interfaceLSM = new GeometricModel2D(thickness);
            var materialPos = new ThermalMaterial(density, specificHeat, conductivityMatrix);
            var materialNeg = new ThermalMaterial(density, specificHeat, conductivityInclusion);
            var materialField = new ThermalMultiMaterialField2D(materialPos, materialNeg, interfaceLSM);

            // Mesh generation
            var meshGen = new UniformMeshGenerator2D<XNode>(xMin, yMin, xMax, yMax, numElementsX, numElementsY);
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

            model.ConnectDataStructures();
            return (model, interfaceLSM);
        }

        private static void ApplyBoundaryConditions(XModel model)
        {
            double meshTol = 1E-7;

            // Left side: T = -100
            double minX = model.Nodes.Select(n => n.X).Min();
            foreach (var node in model.Nodes.Where(n => Math.Abs(n.X - minX) <= meshTol))
            {
                node.Constraints.Add(new Constraint() { DOF = ThermalDof.Temperature, Amount = -100.0 });
            }

            // Right side: T = 100
            double maxX = model.Nodes.Select(n => n.X).Max();
            foreach (var node in model.Nodes.Where(n => Math.Abs(n.X - maxX) <= meshTol))
            {
                node.Constraints.Add(new Constraint() { DOF = ThermalDof.Temperature, Amount = 100 });
            }
        }

        private static void ApplyEnrichments(XModel model, GeometricModel2D geometricModel)
        {
            int numCurves = geometricModel.SingleCurves.Count;
            var interfaceResistances = new double[numCurves];
            interfaceResistances[0] = 1 / interfaceConductivityLeft;
            interfaceResistances[1] = 1 / interfaceConductivityRight;
            var materialInterface = new MultiMaterialInterfaceEnricher(geometricModel, model.Elements.Select(e => (XThermalElement2D)e),
                interfaceResistances);
            materialInterface.ApplyEnrichments();
        }

        private static void InitializeLSM(XModel model, GeometricModel2D geometricModel)
        {
            double dx = (xMax - xMin) / (numBallsX + 1);
            double dy = (yMax - yMin) / (numBallsY + 1);
            for (int i = 0; i < numBallsX; ++i)
            {
                double centreX = xMin + (i + 1) * dx;
                for (int j = 0; j < numBallsY; ++j)
                {
                    double centreY = yMin + (j + 1) * dy;
                    var circle = new Circle2D(new CartesianPoint(centreX, centreY), ballRadius);
                    var lsm = new SimpleLsmClosedCurve2D(thickness, zeroLevelSetTolerance);
                    lsm.InitializeGeometry(model.Nodes, circle);
                    geometricModel.SingleCurves.Add(lsm);
                }
            }
        }
    }
}
