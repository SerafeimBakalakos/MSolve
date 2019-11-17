//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using ISAAR.MSolve.Analyzers;
//using ISAAR.MSolve.Discretization;
//using ISAAR.MSolve.Discretization.FreedomDegrees;
//using ISAAR.MSolve.Discretization.Integration.Quadratures;
//using ISAAR.MSolve.Discretization.Mesh.Generation;
//using ISAAR.MSolve.Discretization.Mesh.Generation.Custom;
//using ISAAR.MSolve.Geometry.Coordinates;
//using ISAAR.MSolve.Geometry.Shapes;
//using ISAAR.MSolve.LinearAlgebra.Vectors;
//using ISAAR.MSolve.Materials;
//using ISAAR.MSolve.Problems;
//using ISAAR.MSolve.Solvers.Direct;
//using ISAAR.MSolve.XFEM.Thermal.Elements;
//using ISAAR.MSolve.XFEM.Thermal.Entities;
//using ISAAR.MSolve.XFEM.Thermal.Integration;
//using ISAAR.MSolve.XFEM.Thermal.LevelSetMethod;
//using ISAAR.MSolve.XFEM.Thermal.MaterialInterface;
//using ISAAR.MSolve.XFEM.Thermal.Materials;
//using ISAAR.MSolve.XFEM.Thermal.Output.Fields;
//using ISAAR.MSolve.XFEM.Thermal.Output.Mesh;
//using ISAAR.MSolve.XFEM.Thermal.Output.Writers;

//namespace ISAAR.MSolve.XFEM.Tests.HEAT.Plotting
//{
//    public static class ThermalInclusionMultipleBalls2D
//    {
//        private const string pathHeatFlux = @"C:\Users\Serafeim\Desktop\HEAT\MultipleBalls\heat_flux.vtk";
//        private const string pathLevelSets = @"C:\Users\Serafeim\Desktop\HEAT\MultipleBalls\level_sets.vtk";
//        private const string pathMesh = @"C:\Users\Serafeim\Desktop\HEAT\MultipleBalls\mesh.vtk";
//        private const string pathTemperature = @"C:\Users\Serafeim\Desktop\HEAT\MultipleBalls\temperature.vtk";

//        private const double xMin = -1.0, xMax = 1.0, yMin = -1, yMax = 1.0;
//        private const int numElementsX = 100, numElementsY = 100;
//        private const int numBallsX = 5, numBallsY = 5;
//        private const double ballRadius = 0.1;

//        private const double zeroLevelSetTolerance = 1E-6;
//        private const int subdomainID = 0;

//        private const double conductivityMatrix = 1.0, conductivityInclusion = 10000;
//        private const double interfaceResistance = 1E-10;

//        public static void PlotLevelSets()
//        {
//            // Create model and LSM
//            (XModel model, GeometricModel2D geometricModel) = CreateModel(numElementsX, numElementsY);
//            InitializeLSM(model, geometricModel);

//            // Plot mesh and level sets
//            using (var writer = new VtkFileWriter(pathLevelSets))
//            {
//                var levelSetField = new LevelSetField(model, geometricModel.SingleCurves[0]);
//                writer.WriteMesh(levelSetField.Mesh);
//                writer.WriteScalarField("inclusion_level_set", levelSetField.Mesh, levelSetField.CalcValuesAtVertices());
//            }
//        }

//        public static void PlotConformingMesh()
//        {
//            // Create model and LSM
//            (XModel model, GeometricModel2D geometricModel) = CreateModel(numElementsX, numElementsY);
//            InitializeLSM(model, geometricModel);

//            // Plot conforming mesh and level sets
//            using (var writer = new VtkFileWriter(pathMesh))
//            {
//                var mesh = new ConformingOutputMesh2D(geometricModel, model.Nodes, model.Elements);
//                writer.WriteMesh(mesh);
//            }

//            // Plot original mesh and level sets
//            using (var writer = new VtkFileWriter(pathLevelSets))
//            {
//                var levelSetField = new LevelSetField(model, geometricModel.SingleCurves[0]);
//                writer.WriteMesh(levelSetField.Mesh);
//                writer.WriteScalarField("inclusion_level_set", levelSetField.Mesh, levelSetField.CalcValuesAtVertices());
//            }
//        }

//        public static void PlotTemperature()
//        {
//            // Create model and LSM
//            (XModel model, GeometricModel2D geometricModel) = CreateModel(numElementsX, numElementsY);
//            InitializeLSM(model, geometricModel);
//            ApplyEnrichments(model, geometricModel);

//            // Run the analysis
//            SkylineSolver solver = new SkylineSolver.Builder().BuildSolver(model);
//            var problem = new ProblemThermalSteadyState(model, solver);
//            var linearAnalyzer = new LinearAnalyzer(model, solver, problem);
//            var staticAnalyzer = new StaticAnalyzer(model, solver, problem, linearAnalyzer);
//            staticAnalyzer.Initialize();
//            staticAnalyzer.Solve();

//            // Plot original mesh and level sets
//            using (var writer = new VtkFileWriter(pathLevelSets))
//            {
//                var levelSetField = new LevelSetField(model, geometricModel.SingleCurves[0]);
//                writer.WriteMesh(levelSetField.Mesh);
//                writer.WriteScalarField("inclusion_level_set", levelSetField.Mesh, levelSetField.CalcValuesAtVertices());
//            }

//            // Plot conforming mesh and temperature field
//            using (var writer = new VtkFileWriter(pathTemperature))
//            {
//                var mesh = new ConformingOutputMesh2D(geometricModel, model.Nodes, model.Elements);
//                var temperatureField = new TemperatureField2D(model, geometricModel.SingleCurves[0], mesh);
//                writer.WriteMesh(mesh);
//                IVectorView solution = solver.LinearSystems[subdomainID].Solution;
//                writer.WriteScalarField("temperature", mesh, temperatureField.CalcValuesAtVertices(solution));
//            }
//        }

//        public static void PlotTemperatureAndFlux()
//        {
//            // Create model and LSM
//            (XModel model, GeometricModel2D geometricModel) = CreateModel(numElementsX, numElementsY);
//            InitializeLSM(model, geometricModel);
//            ApplyEnrichments(model, geometricModel);

//            // Run the analysis
//            SkylineSolver solver = new SkylineSolver.Builder().BuildSolver(model);
//            var problem = new ProblemThermalSteadyState(model, solver);
//            var linearAnalyzer = new LinearAnalyzer(model, solver, problem);
//            var staticAnalyzer = new StaticAnalyzer(model, solver, problem, linearAnalyzer);
//            staticAnalyzer.Initialize();
//            staticAnalyzer.Solve();

//            // Plot original mesh and level sets
//            using (var writer = new VtkFileWriter(pathLevelSets))
//            {
//                var levelSetField = new LevelSetField(model, geometricModel.SingleCurves[0]);
//                writer.WriteMesh(levelSetField.Mesh);
//                writer.WriteScalarField("inclusion_level_set", levelSetField.Mesh, levelSetField.CalcValuesAtVertices());
//            }

//            // Plot conforming mesh, temperature field
//            using (var writer = new VtkFileWriter(pathTemperature))
//            {
//                var mesh = new ConformingOutputMesh2D(geometricModel, model.Nodes, model.Elements);
//                var temperatureField = new TemperatureField2D(model, geometricModel.SingleCurves[0], mesh);
//                var fluxField = new HeatFluxField2D(model, geometricModel.SingleCurves[0], mesh, zeroLevelSetTolerance);
//                writer.WriteMesh(mesh);
//                IVectorView solution = solver.LinearSystems[subdomainID].Solution;
//                writer.WriteScalarField("temperature", mesh, temperatureField.CalcValuesAtVertices(solution));
//                writer.WriteVector2DField("heat_flux", mesh, fluxField.CalcValuesAtVertices(solution));
//            }
//        }

//        private static (XModel, GeometricModel2D) CreateModel(int numElementsX, int numElementsY)
//        {
//            var model = new XModel();
//            model.Subdomains[subdomainID] = new XSubdomain(subdomainID);


//            // Materials
//            double thickness = 1.0;
//            double density = 1.0;
//            double specificHeat = 1.0;
//            var geometricModel = new GeometricModel2D(thickness, zeroLevelSetTolerance);
//            var materialPos = new ThermalMaterial(density, specificHeat, conductivityMatrix);
//            var materialNeg = new ThermalMaterial(density, specificHeat, conductivityInclusion);
//            var materialField = new ThermalMultiMaterialField2D(materialPos, materialNeg, geometricModel);

//            // Mesh generation
//            var meshGen = new UniformMeshGenerator2D<XNode>(xMin, yMin, xMax, yMax, numElementsX, numElementsY);
//            (IReadOnlyList<XNode> nodes, IReadOnlyList<CellConnectivity<XNode>> cells) =
//                meshGen.CreateMesh((id, x, y, z) => new XNode(id, x, y));

//            // Nodes
//            foreach (XNode node in nodes) model.Nodes.Add(node);

//            // Elements
//            var integrationForConductivity = new RectangularSubgridIntegration2D<XThermalElement2D>(8,
//                GaussLegendre2D.GetQuadratureWithOrder(2, 2));
//            int numGaussPointsInterface = 2;
//            var elementFactory = new XThermalElement2DFactory(materialField, thickness, integrationForConductivity,
//                numGaussPointsInterface);
//            for (int e = 0; e < cells.Count; ++e)
//            {
//                var element = elementFactory.CreateElement(e, cells[e].CellType, cells[e].Vertices);
//                model.Elements.Add(element);
//                model.Subdomains[subdomainID].Elements.Add(element);
//            }

//            ApplyBoundaryConditions(model);

//            model.ConnectDataStructures();
//            return (model, geometricModel);
//        }

//        private static void ApplyBoundaryConditions(XModel model)
//        {
//            double meshTol = 1E-7;

//            // Left side: T = -100
//            double minX = model.Nodes.Select(n => n.X).Min();
//            foreach (var node in model.Nodes.Where(n => Math.Abs(n.X - minX) <= meshTol))
//            {
//                node.Constraints.Add(new Constraint() { DOF = ThermalDof.Temperature, Amount = -100.0 });
//            }

//            // Right side: T = 100
//            double maxX = model.Nodes.Select(n => n.X).Max();
//            foreach (var node in model.Nodes.Where(n => Math.Abs(n.X - maxX) <= meshTol))
//            {
//                node.Constraints.Add(new Constraint() { DOF = ThermalDof.Temperature, Amount = 100 });
//            }
//        }

//        private static void ApplyEnrichments(XModel model, GeometricModel2D geometricModel)
//        {
//            var materialInterface = new SingleMaterialInterface(geometricModel, model.Elements.Select(e => (XThermalElement2D)e),
//                interfaceResistance);
//            materialInterface.ApplyEnrichments();
//        }

//        private static void InitializeLSM(XModel model, GeometricModel2D geometricModel)
//        {
//            var inclusions = new List<Circle2D>();
//            double dx = (xMax - xMin) / (numBallsX + 1);
//            double dy = (yMax - yMin) / (numBallsY + 1);
//            for (int i = 0; i < numBallsX; ++i)
//            {
//                double centreX = xMin + (i + 1) * dx;
//                for (int j = 0; j < numBallsY; ++j)
//                {
//                    double centreY = yMin + (j + 1) * dy;
//                    inclusions.Add(new Circle2D(new CartesianPoint(centreX, centreY), ballRadius));
//                }
//            }
//            geometricModel.InitializeGeometry(model.Nodes, inclusions);
//        }
//    }
//}
