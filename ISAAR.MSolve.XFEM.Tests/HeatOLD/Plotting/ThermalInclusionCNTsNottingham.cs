using System;
using System.Collections.Generic;
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
using ISAAR.MSolve.XFEM_OLD.Thermal.Elements;
using ISAAR.MSolve.XFEM_OLD.Thermal.Entities;
using ISAAR.MSolve.XFEM_OLD.Thermal.Integration;
using ISAAR.MSolve.XFEM_OLD.Thermal.Curves;
using ISAAR.MSolve.XFEM_OLD.Thermal.MaterialInterface;
using ISAAR.MSolve.XFEM_OLD.Thermal.Materials;
using ISAAR.MSolve.XFEM_OLD.Thermal.Output.Enrichments;
using ISAAR.MSolve.XFEM_OLD.Thermal.Output.Fields;
using ISAAR.MSolve.XFEM_OLD.Thermal.Output.Mesh;
using ISAAR.MSolve.XFEM_OLD.Thermal.Output.Writers;
using ISAAR.MSolve.XFEM_OLD.Thermal.Curves.LevelSetMethod;
using System.Linq;
using System.Diagnostics;

namespace ISAAR.MSolve.XFEM_OLD.Tests.HeatOLD.Plotting
{
    public static class ThermalInclusionCNTsNottingham
    {
        private const string outputDirectory = @"C:\Users\Serafeim\Desktop\HEAT\CNTsNottingham";
        private const string pathHeatFlux = @"C:\Users\Serafeim\Desktop\HEAT\CNTsNottingham\heat_flux.vtk";
        private const string pathMesh = @"C:\Users\Serafeim\Desktop\HEAT\CNTsNottingham\conforming_mesh.vtk";
        private const string pathTemperature = @"C:\Users\Serafeim\Desktop\HEAT\CNTsNottingham\temperature.vtk";

        private const int numElementsX = 100, numElementsY = 100;
        private const double thickness = 1.0;
        private const double zeroLevelSetTolerance = 1E-6;
        private const int subdomainID = 0;

        private const double minX = -1.0, minY = -1.0, maxX = 1.0, maxY = 1.0;
        private const double cntLength = 0.4, cntHeight = cntLength / 10;
        private const int numCNTs = 25;
        private const bool cntsCannotInteract = true;

        private const double conductivityMatrix = 1.0, conductivityInclusion = 1000.0;
        private const double interfaceResistance = 1E-2;

        public static void PlotLevelSets()
        {
            double totalVolume = (maxX - minX) * (maxY - minY);
            double cntVolume = numCNTs * cntLength * cntHeight;
            Console.WriteLine($"Volume fraction = {cntVolume / totalVolume}");

            // Create model and LSM
            (XModel model, GeometricModel2D geometricModel) = CreateModel(numElementsX, numElementsY);
            CreateMultipleInclusions(model, geometricModel);

            // Plot mesh and level sets
            Utilities.PlotInclusionLevelSets(outputDirectory, "level_set", model, geometricModel);
        }

        public static void PlotConformingMesh()
        {
            // Create model and LSM
            (XModel model, GeometricModel2D geometricModel) = CreateModel(numElementsX, numElementsY);
            CreateMultipleInclusions(model, geometricModel);

            // Plot conforming mesh
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
            CreateMultipleInclusions(model, geometricModel);
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
            CreateMultipleInclusions(model, geometricModel);
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
            double density = 1.0;
            double specificHeat = 1.0;
            var geometricModel = new GeometricModel2D(thickness);
            var materialPos = new ThermalMaterial(density, specificHeat, conductivityMatrix);
            var materialNeg = new ThermalMaterial(density, specificHeat, conductivityInclusion);
            var materialField = new ThermalBiMaterialField2D(materialPos, materialNeg, geometricModel);

            // Mesh generation
            var meshGen = new UniformMeshGenerator2D<XNode>(minX, minY, maxX, maxY, numElementsX, numElementsY);
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
            return (model, geometricModel);
        }

        private static void ApplyBoundaryConditions(XModel model)
        {
            double meshTol = 1E-7;

            // Left side: T = -100
            double minX = model.Nodes.Select(n => n.X).Min();
            foreach (var node in model.Nodes.Where(n => Math.Abs(n.X - minX) <= meshTol))
            {
                node.Constraints.Add(new Constraint() { DOF = ThermalDof.Temperature, Amount = -100 });
            }

            // Right side: T = 100
            double maxX = model.Nodes.Select(n => n.X).Max();
            foreach (var node in model.Nodes.Where(n => Math.Abs(n.X - maxX) <= meshTol))
            {
                node.Constraints.Add(new Constraint() { DOF = ThermalDof.Temperature, Amount = 100 });
            }

            // Node inside circle
            //XNode internalNode = model.Nodes.Where(n => (Math.Abs(n.X + 0.4) <= meshTol) && (Math.Abs(n.Y) <= meshTol)).First();
            //System.Diagnostics.Debug.Assert(internalNode != null);
            //internalNode.Constraints.Add(new Constraint() { DOF = ThermalDof.Temperature, Amount = 0.1 });
        }

        private static void ApplyEnrichments(XModel model, GeometricModel2D geometricModel)
        {
            var materialInterface = new SingleMaterialInterfaceEnricher(geometricModel, geometricModel.SingleCurves[0],
                model.Elements.Select(e => (XThermalElement2D)e), interfaceResistance);
            materialInterface.ApplyEnrichments();
        }

        private static void Create1Inclusion(XModel model, GeometricModel2D geometricModel)
        {
            var rectangle = new Rectangle2D(new CartesianPoint(0.0, 0.0), 0.8, 0.1, Math.PI / 3);
            var lsm = new SimpleLsmClosedCurve2D(thickness, zeroLevelSetTolerance);
            lsm.InitializeGeometry(model.Nodes, rectangle);
            geometricModel.SingleCurves.Add(lsm);
        }

        private static void Create2Inclusions(XModel model, GeometricModel2D geometricModel)
        {
            var rectangles = new Rectangle2D[2];
            rectangles[0] = new Rectangle2D(new CartesianPoint(-0.4, 0.0), 0.4, 0.05, Math.PI / 3);
            rectangles[1] = new Rectangle2D(new CartesianPoint(-0.2, 0.0), 0.4, 0.05, 5 * Math.PI / 6);
            //Debug.Assert(rectangles[0].IsDisjointFrom(rectangles[1]));

            var lsm = new MultiLsmClosedCurve2D(thickness, zeroLevelSetTolerance);
            lsm.InitializeGeometry(model.Nodes, rectangles);
            geometricModel.SingleCurves.Add(lsm);
        }

        private static void CreateMultipleInclusions(XModel model, GeometricModel2D geometricModel)
        {
            List<Rectangle2D> cnts = ScatterDisjointCNTs();
            var lsm = new MultiLsmClosedCurve2D(thickness, zeroLevelSetTolerance);
            lsm.InitializeGeometry(model.Nodes, cnts);
            geometricModel.SingleCurves.Add(lsm);
        }

        private static List<Rectangle2D> ScatterDisjointCNTs()
        {
            int seed = 25;
            var rng = new Random(seed);
            var cnts = new List<Rectangle2D>();
            cnts.Add(GenerateCNT(rng));
            for (int i = 1; i < numCNTs; ++i)
            {
                Console.WriteLine("Trying new CNT");
                Rectangle2D newCNT = null;
                do
                {
                    newCNT = GenerateCNT(rng);
                }
                while (cntsCannotInteract && InteractsWithOtherCNTs(newCNT, cnts));
                cnts.Add(newCNT);
            }
            return cnts;
        }

        private static Rectangle2D GenerateCNT(Random rng)
        {
            //double lbX = minX + 0.5 * cntLength, ubX = maxX - 0.5 * cntLength;
            //double lbY = minY + 0.5 * cntLength, ubY = maxY - 0.5 * cntLength;
            double lbX = minX, ubX = maxX;
            double lbY = minY, ubY = maxY;

            double centroidX = lbX + (ubX - lbX) * rng.NextDouble();
            double centroidY = lbY + (ubY - lbY) * rng.NextDouble();
            double angle = Math.PI * rng.NextDouble();
            return new Rectangle2D(new CartesianPoint(centroidX, centroidY), cntLength, cntHeight, angle);
        }

        private static bool InteractsWithOtherCNTs(Rectangle2D newCNT, List<Rectangle2D> currentCNTs)
        {
            var scaledCNT = newCNT.ScaleRectangle();
            foreach (Rectangle2D cnt in currentCNTs)
            {
                if (!cnt.ScaleRectangle().IsDisjointFrom(scaledCNT))
                {
                    Console.WriteLine("It interacts with an existing one");
                    return true;
                }
            }
            return false;
        }

        private static Rectangle2D ScaleRectangle(this Rectangle2D rectangle)
        {
            double scaleFactor = 1.2;
            return new Rectangle2D(rectangle.Centroid,
                scaleFactor * rectangle.LengthAxis0, scaleFactor * rectangle.LengthAxis1, rectangle.Axis0Angle);
        }
    }
}
