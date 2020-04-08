using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ISAAR.MSolve.Analyzers;
using ISAAR.MSolve.Analyzers.Multiscale;
using ISAAR.MSolve.Discretization;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Discretization.Integration.Quadratures;
using ISAAR.MSolve.Discretization.Mesh;
using ISAAR.MSolve.Discretization.Mesh.Generation;
using ISAAR.MSolve.Discretization.Mesh.Generation.Custom;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Problems;
using ISAAR.MSolve.Solvers.Direct;
using ISAAR.MSolve.XFEM.Multiphase.Elements;
using ISAAR.MSolve.XFEM.Multiphase.Enrichment;
using ISAAR.MSolve.XFEM.Multiphase.Enrichment.SingularityResolution;
using ISAAR.MSolve.XFEM.Multiphase.Entities;
using ISAAR.MSolve.XFEM.Multiphase.Input;
using ISAAR.MSolve.XFEM.Multiphase.Integration;
using ISAAR.MSolve.XFEM.Multiphase.Materials;
using ISAAR.MSolve.XFEM.Multiphase.Plotting;
using ISAAR.MSolve.XFEM.Multiphase.Plotting.Enrichments;
using ISAAR.MSolve.XFEM.Multiphase.Plotting.Fields;
using ISAAR.MSolve.XFEM.Multiphase.Plotting.Mesh;
using ISAAR.MSolve.XFEM.Multiphase.Plotting.Writers;
using ISAAR.MSolve.XFEM.Tests.Multiphase.Plotting;

namespace ISAAR.MSolve.XFEM.Tests.Multiphase.Paper1
{
    public static class Paper1Example2
    {
        private const int numElementsX = 200, numElementsY = 200;
        private const int subdomainID = 0;
        private const double minX = 0, minY = 0, maxX = 2000, maxY = 2000;
        //private const double minX = -1.0, minY = -1.0, maxX = 1.0, maxY = 1.0;
        private const double elementSize = (maxX - minX) / numElementsX;
        private const double thickness = 1.0;
        private static readonly PhaseGenerator generator = new PhaseGenerator(minX, maxX, numElementsX);
        private const bool integrationWithSubtriangles = true;
        private const double matrixConductivity = 0.2, inclusionConductivity = 2000, layerConductivity = 0.3;
        private const double matrixLayerInterfaceConductivity = 1E5 /*0.25*/, 
            layerLayerInterfaceConductivity = 1E3, //1E3
            inclusionLayerInterfaceConductivity = 1E3; //1E3
        private const double specialHeatCoeff = 1.0;
        private const double singularityRelativeAreaTolerance = 1E-8;

        public static void Run()
        {
            var phaseReader = new PhaseReader(true, 0);
            string directory = @"C:\Users\Serafeim\Desktop\HEAT\Paper\Paper1Example2\";
            string matrixLayersFile = directory + "boundaries.txt";
            string inclusionsFile = directory + "CNTnodes.txt";
            GeometricModel geometricModel = phaseReader.ReadPhasesFromFile(matrixLayersFile, inclusionsFile);
            var paths = new OutputPaths();
            paths.FillAllForDirectory(@"C:\Users\Serafeim\Desktop\HEAT\Paper\Paper1Example2");
            PlotPhasesInteractions(() => geometricModel, paths);
        }

        public static void RunHomogenization()
        {
            string directory = @"C:\Users\Serafeim\Desktop\HEAT\Paper\Paper1Example2\";
            string matrixLayersFile = directory + "boundaries.txt";
            string inclusionsFile = directory + "CNTnodes.txt";
            var phaseReader = new PhaseReader(true, 0);
            GeometricModel geometricModel = phaseReader.ReadPhasesFromFile(matrixLayersFile, inclusionsFile);

            XModel physicalModel = CreatePhysicalModel(geometricModel);
            PrepareForAnalysis(physicalModel, geometricModel);

            // Analysis
            //Vector2 temperatureGradient = Vector2.Create(200, 0);
            Vector2 temperatureGradient = Vector2.Create(0, 0);
            //var solver = (new SkylineSolver.Builder()).BuildSolver(physicalModel);
            SuiteSparseSolver solver = new SuiteSparseSolver.Builder().BuildSolver(physicalModel);
            var provider = new ProblemThermalSteadyState(physicalModel, solver);
            var rve = new ThermalSquareRve(physicalModel, Vector2.Create(minX, minY), Vector2.Create(maxX, maxY), thickness,
                temperatureGradient);
            var homogenization = new HomogenizationAnalyzer(physicalModel, solver, provider, rve);

            homogenization.Initialize();
            homogenization.Solve();

            IMatrix conductivity = homogenization.EffectiveConstitutiveTensors[subdomainID];
            Console.WriteLine($"C = [ {conductivity[0, 0]} {conductivity[0, 1]}; {conductivity[1, 0]} {conductivity[1, 1]} ]");
        }

        private static void PlotPhasesInteractions(Func<GeometricModel> genPhases, OutputPaths paths)
        {
            GeometricModel geometricModel = genPhases();
            XModel physicalModel = CreatePhysicalModel(geometricModel);
            PrepareForAnalysis(physicalModel, geometricModel);
            
            var feMesh = new ContinuousOutputMesh<XNode>(physicalModel.Nodes, physicalModel.Elements);
            using (var writer = new VtkFileWriter(paths.finiteElementMesh))
            {
                writer.WriteMesh(feMesh);
            }

            var phasePlotter = new PhasePlotter(physicalModel, geometricModel, -10);
            phasePlotter.PlotPhases(paths.phasesGeometry);

            var conformingMesh = new ConformingOutputMesh2D(geometricModel, physicalModel.Nodes, physicalModel.Elements);
            using (var writer = new VtkFileWriter(paths.conformingMesh))
            {
                writer.WriteMesh(conformingMesh);
            }

            phasePlotter.PlotNodes(paths.nodalPhases);
            phasePlotter.PlotElements(paths.elementPhases, conformingMesh);

            // Enrichment
            var enrichmentPlotter = new EnrichmentPlotter(physicalModel, elementSize);
            enrichmentPlotter.PlotStepEnrichedNodes(paths.stepEnrichedNodes);
            if (paths.junctionEnrichedNodes != null) enrichmentPlotter.PlotJunctionEnrichedNodes(paths.junctionEnrichedNodes);

            // Integration
            var integrationPlotter = new IntegrationMeshPlotter(physicalModel, geometricModel);
            integrationPlotter.PlotVolumeIntegrationMesh(paths.volumeIntegrationMesh);
            integrationPlotter.PlotVolumeIntegrationPoints(paths.volumeIntegrationPoints);
            integrationPlotter.PlotBoundaryIntegrationMesh(paths.boundaryIntegrationCells, paths.boundaryIntegrationVertices);
            integrationPlotter.PlotBoundaryIntegrationPoints(paths.boundaryIntegrationPoints);

            // Material
            var materialPlotter = new MaterialPlotter(physicalModel);
            materialPlotter.PlotVolumeMaterials(paths.volumeIntegrationMaterials);
            materialPlotter.PlotBoundaryMaterials(paths.boundaryIntegrationMaterials);
            //materialPlotter.PlotBoundaryPhaseJumpCoefficients(paths.boundaryIntegrationPhaseJumps);

            // Analysis
            IVectorView solution = RunAnalysis(physicalModel);

            // Plot temperature
            using (var writer = new Logging.VTK.VtkPointWriter(paths.temperatureAtNodes))
            {
                var temperatureField = new TemperatureAtNodesField(physicalModel);
                writer.WriteScalarField("temperature", temperatureField.CalcValuesAtVertices(solution));
            }
            using (var writer = new Logging.VTK.VtkPointWriter(paths.temperatureAtGaussPoints))
            {
                var temperatureField = new TemperatureAtGaussPointsField(physicalModel);
                writer.WriteScalarField("temperature", temperatureField.CalcValuesAtVertices(solution));
            }
            using (var writer = new VtkFileWriter(paths.temperatureField))
            {
                var temperatureField = new TemperatureField2D(physicalModel, conformingMesh);
                writer.WriteMesh(conformingMesh);
                writer.WriteScalarField("temperature", conformingMesh, temperatureField.CalcValuesAtVertices(solution));
            }
            using (var writer = new Logging.VTK.VtkPointWriter(paths.heatFluxAtGaussPoints))
            {
                var fluxField = new HeatFluxAtGaussPointsField(physicalModel);
                writer.WriteVector2DField("heat_flux", fluxField.CalcValuesAtVertices(solution));
            }
        }

        private static XModel CreatePhysicalModel(GeometricModel geometricModel)
        {
            var physicalModel = new XModel();
            physicalModel.Subdomains[subdomainID] = new XSubdomain(subdomainID);

            // Mesh generation
            var meshGen = new UniformMeshGenerator2D<XNode>(minX, minY, maxX, maxY, numElementsX, numElementsY);
            (IReadOnlyList<XNode> nodes, IReadOnlyList<CellConnectivity<XNode>> cells) =
                meshGen.CreateMesh((id, x, y, z) => new XNode(id, x, y));

            // Nodes
            foreach (XNode node in nodes) physicalModel.Nodes.Add(node);

            // Integration
            IIntegrationStrategy volumeIntegration;
            if (integrationWithSubtriangles)
            {
                volumeIntegration = new IntegrationWithConformingSubtriangles2D(GaussLegendre2D.GetQuadratureWithOrder(2, 2),
                    TriangleQuadratureSymmetricGaussian.Order2Points3,
                    element => geometricModel.GetConformingTriangulationOf(element));
            }
            else
            {
                volumeIntegration = new IntegrationWithNonConformingSubsquares2D(
                    GaussLegendre2D.GetQuadratureWithOrder(2, 2), 8, GaussLegendre2D.GetQuadratureWithOrder(2, 2));
            }
            IBoundaryIntegration boundaryIntegration = new LinearBoundaryIntegration(GaussLegendre1D.GetQuadratureWithOrder(3));

            // Materials
            var matrixMaterial = new ThermalMaterial(matrixConductivity, specialHeatCoeff);
            var inclusionMaterial = new ThermalMaterial(inclusionConductivity, specialHeatCoeff);
            var layerMaterial = new ThermalMaterial(layerConductivity, specialHeatCoeff);
            var materialField = new MatrixInclusionsLayersMaterialField(matrixMaterial, inclusionMaterial, layerMaterial,
                matrixLayerInterfaceConductivity, layerLayerInterfaceConductivity, inclusionLayerInterfaceConductivity, 
                DefaultPhase.DefaultPhaseID);

            // Elements
            var factory = new XThermalElement2DFactory(materialField, thickness, volumeIntegration, boundaryIntegration);
            for (int e = 0; e < cells.Count; ++e)
            {
                XThermalElement2D element = factory.CreateElement(e, CellType.Quad4, cells[e].Vertices);
                physicalModel.Elements.Add(element);
                physicalModel.Subdomains[subdomainID].Elements.Add(element);
            }

            // Boundary conditions
            ApplyBoundaryConditions(physicalModel);

            return physicalModel;
        }

        private static void ApplyBoundaryConditions(XModel physicalModel)
        {
            double meshTol = 1E-7;

            // Left side: T = +100
            double minX = physicalModel.Nodes.Select(n => n.X).Min();
            foreach (var node in physicalModel.Nodes.Where(n => Math.Abs(n.X - minX) <= meshTol))
            {
                node.Constraints.Add(new Constraint() { DOF = ThermalDof.Temperature, Amount = +100 });
            }

            // Right side: T = 100
            double maxX = physicalModel.Nodes.Select(n => n.X).Max();
            foreach (var node in physicalModel.Nodes.Where(n => Math.Abs(n.X - maxX) <= meshTol))
            {
                node.Constraints.Add(new Constraint() { DOF = ThermalDof.Temperature, Amount = -100 });
            }
        }


        private static void PrepareForAnalysis(XModel physicalModel, GeometricModel geometricModel)
        {
            physicalModel.ConnectDataStructures();

            geometricModel.AssossiatePhasesNodes(physicalModel);
            geometricModel.AssociatePhasesElements(physicalModel);
            geometricModel.FindConformingMesh(physicalModel);

            ISingularityResolver singularityResolver = new RelativeAreaResolver(geometricModel, singularityRelativeAreaTolerance);
            //var nodeEnricher = new NodeEnricherOLD(geometricModel, singularityResolver);
            var nodeEnricher = new NodeEnricher2Junctions(geometricModel, singularityResolver);
            nodeEnricher.ApplyEnrichments();

            physicalModel.UpdateDofs();
            physicalModel.UpdateMaterials();
        }

        private static IVectorView RunAnalysis(XModel physicalModel)
        {
            SkylineSolver solver = new SkylineSolver.Builder().BuildSolver(physicalModel);
            var problem = new ProblemThermalSteadyState(physicalModel, solver);
            var linearAnalyzer = new LinearAnalyzer(physicalModel, solver, problem);
            var staticAnalyzer = new StaticAnalyzer(physicalModel, solver, problem, linearAnalyzer);

            staticAnalyzer.Initialize();
            staticAnalyzer.Solve();

            return solver.LinearSystems[subdomainID].Solution;
        }
    }
}
