using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ISAAR.MSolve.Discretization;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Discretization.Integration.Quadratures;
using ISAAR.MSolve.Discretization.Mesh.Generation;
using ISAAR.MSolve.Discretization.Mesh.Generation.Custom;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.Geometry.Shapes;
using ISAAR.MSolve.Materials;
using ISAAR.MSolve.XFEM.ThermalOLD.Curves.LevelSetMethod;
using ISAAR.MSolve.XFEM.ThermalOLD.Elements;
using ISAAR.MSolve.XFEM.ThermalOLD.Entities;
using ISAAR.MSolve.XFEM.ThermalOLD.Integration;
using ISAAR.MSolve.XFEM.ThermalOLD.MaterialInterface;
using ISAAR.MSolve.XFEM.ThermalOLD.Materials;
using ISAAR.MSolve.XFEM.ThermalOLD.Output.Enrichments;

namespace ISAAR.MSolve.XFEM.Tests.HeatOLD.Plotting
{
    public static class ThreewayJunction2D
    {
        private const string outputDirectory = @"C:\Users\Serafeim\Desktop\HEAT\Junction";
        private const string pathHeatFlux = @"C:\Users\Serafeim\Desktop\HEAT\Junction\heat_flux.vtk";
        private const string pathMesh = @"C:\Users\Serafeim\Desktop\HEAT\Junction\conforming_mesh.vtk";
        private const string pathTemperature = @"C:\Users\Serafeim\Desktop\HEAT\Junction\temperature.vtk";

        private const int numElementsX = 3, numElementsY = 3;
        private const double thickness = 1.0;
        private const double zeroLevelSetTolerance = 1E-6;
        private const int subdomainID = 0;

        private const double conductivityMatrix = 1.0, conductivityInclusion = 1000.0;
        private const double cntCntResistance = 1E1;
        private const double cntMatrixResistance = 1E10;

        public static void PlotLevelSetsAndEnrichments()
        {
            // Create model and LSM
            (XModel model, GeometricModel2D geometricModel) = CreateModel(numElementsX, numElementsY);
            ApplyEnrichments(model, geometricModel);

            // Plot mesh and level sets
            Utilities.PlotInclusionLevelSets(outputDirectory, "level_set", model, geometricModel);

            // Plot enrichments
            var enrichmentPlotter = new EnrichmentPlotter(model, geometricModel, outputDirectory);
            enrichmentPlotter.PlotEnrichedNodes();
        }

        private static void CreateGeometricModel(XModel model, GeometricModel2D geometricModel)
        {
            // Define geometry
            var junction = new CartesianPoint(0.0, 0.0);
            var point0 = new CartesianPoint(-1.0, 1.2);
            var point1 = new CartesianPoint(1.0, 1.2);
            var point2 = new CartesianPoint(0.0, -1.2);
            var curves = new List<DirectedSegment2D>();
            curves.Add(new DirectedSegment2D(point0, junction));
            curves.Add(new DirectedSegment2D(point1, junction));
            curves.Add(new DirectedSegment2D(point2, junction));

            // Create level set methods
            foreach (DirectedSegment2D curve in curves)
            {
                var lsm = new SimpleLsmClosedCurve2D(thickness, zeroLevelSetTolerance);
                lsm.InitializeGeometry(model.Nodes, curve);
                geometricModel.SingleCurves.Add(lsm);
            }

            #region these should be done by a preprocessor
            // Define material phases
            var phase0 = new MaterialPhase(0);
            phase0.AddBoundary(geometricModel.SingleCurves[0], -1);
            phase0.AddBoundary(geometricModel.SingleCurves[2], +1);
            geometricModel.Phases.Add(phase0);

            var phase1 = new MaterialPhase(1);
            phase1.AddBoundary(geometricModel.SingleCurves[1], -1);
            phase1.AddBoundary(geometricModel.SingleCurves[2], -1);
            geometricModel.Phases.Add(phase0);

            var phase2 = new MaterialPhase(2);
            phase2.AddBoundary(geometricModel.SingleCurves[0], +1);
            phase2.AddBoundary(geometricModel.SingleCurves[1], +1);
            geometricModel.Phases.Add(phase0);

            // Define junctions
            geometricModel.Junctions.Add(new PhaseJunction(junction, new MaterialPhase[] { phase0, phase1, phase2 }));
            #endregion
        }

        private static (XModel, GeometricModel2D) CreateModel(int numElementsX, int numElementsY)
        {
            var model = new XModel();
            model.Subdomains[subdomainID] = new XSubdomain(subdomainID);
            var geometricModel = new GeometricModel2D(thickness);

            // Materials
            double density = 1.0;
            double specificHeat = 1.0;
            var materialPos = new ThermalMaterial(density, specificHeat, conductivityMatrix);
            var materialNeg = new ThermalMaterial(density, specificHeat, conductivityInclusion);
            var materialField = new ThermalMultiMaterialField2D(materialPos, materialNeg, geometricModel);

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
            CreateGeometricModel(model, geometricModel);
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
        }

        private static void ApplyEnrichments(XModel model, GeometricModel2D geometricModel)
        {
            var resistances = new double[] { cntMatrixResistance, cntMatrixResistance, cntCntResistance };
            var enricher = new MultiphaseEnricher(geometricModel, model.Elements.Select(e => (XThermalElement2D)e), resistances);
            enricher.ApplyEnrichments();
        }
    }
}
