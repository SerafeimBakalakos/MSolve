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
using ISAAR.MSolve.LinearAlgebra.Exceptions;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Problems;
using ISAAR.MSolve.Solvers.Direct;
using ISAAR.MSolve.XFEM_OLD.Multiphase.Elements;
using ISAAR.MSolve.XFEM_OLD.Multiphase.Enrichment;
using ISAAR.MSolve.XFEM_OLD.Multiphase.Enrichment.SingularityResolution;
using ISAAR.MSolve.XFEM_OLD.Multiphase.Entities;
using ISAAR.MSolve.XFEM_OLD.Multiphase.Geometry.Voronoi;
using ISAAR.MSolve.XFEM_OLD.Multiphase.Input;
using ISAAR.MSolve.XFEM_OLD.Multiphase.Integration;
using ISAAR.MSolve.XFEM_OLD.Multiphase.Materials;
using ISAAR.MSolve.XFEM_OLD.Multiphase.Plotting;
using ISAAR.MSolve.XFEM_OLD.Multiphase.Plotting.Enrichments;
using ISAAR.MSolve.XFEM_OLD.Multiphase.Plotting.Fields;
using ISAAR.MSolve.XFEM_OLD.Multiphase.Plotting.Mesh;
using ISAAR.MSolve.XFEM_OLD.Multiphase.Plotting.Writers;
using ISAAR.MSolve.XFEM_OLD.Tests.Multiphase.Plotting;

namespace ISAAR.MSolve.XFEM_OLD.Tests.Multiphase.Paper1
{
    public static class Paper1Example1
    {
        private const string ioPath = @"C:\Users\Serafeim\Desktop\HEAT\Paper\Paper1Example1\";
        private const int subdomainID = 0;
        private const bool integrationWithSubtriangles = true;

        private const double specialHeatCoeff = 1.0;
        private const double singularityRelativeAreaTolerance = 1E-8;
        private const bool fixedEnrichment = true;

        public static void RunHomogenization()
        {
            var input = new ProblemInput();

            #region sample input
            input.NumElements = new int[] { 200, 200 };
            input.MinCoords = new double[] { -1, -1 };
            input.MaxCoords = new double[] { 1, 1 };
            input.Thickness = 1.0;
            input.PhasesInputDirectoryPath = ioPath + @"input\sample\";
            input.GrainSize = double.NaN;
            input.GrainConductivity = 41;
            input.BoundaryConductivity = 2.46;
            #endregion

            #region grain size = 1nm
            //input.NumElements = new int[] { 200, 200 };
            //input.MinCoords = new double[] { 0, 0 };
            //input.MaxCoords = new double[] { 30, 30 };
            //input.Thickness = 1; //Paper 4.65E-1 nm, it should not matter
            //input.PhasesInputDirectoryPath = ioPath + @"input\grain_size1\";
            //input.GrainSize = 1;
            //input.GrainConductivity = 41E-9; // 41E-9 W/nm K // Paper: 41 W/mK 
            //input.BoundaryConductivity = 2.46E-9; // 2.46E-9 W/nm^2 K// Paper: 2.46E9 W/m^2K
            #endregion

            #region grain size = 2nm
            //input.NumElements = new int[] { 200, 200 };
            //input.MinCoords = new double[] { 0, 0 };
            //input.MaxCoords = new double[] { 60, 60 };
            //input.Thickness = 1; //Paper 4.65E-1 nm, it should not matter
            //input.PhasesInputDirectoryPath = ioPath + @"input\grain_size2\";
            //input.GrainSize = 2;
            //input.GrainConductivity = 41E-9; // 41E-9 W/nm K // Paper: 41 W/mK 
            //input.BoundaryConductivity = 2.46E-9; // 2.46E-9 W/nm^2 K// Paper: 2.46E9 W/m^2K
            #endregion

            #region grain size = 5nm
            //input.NumElements = new int[] { 200, 200 };
            //input.MinCoords = new double[] { 0, 0 };
            //input.MaxCoords = new double[] { 150, 150 };
            //input.Thickness = 1; //Paper 4.65E-1 nm, it should not matter
            //input.PhasesInputDirectoryPath = ioPath + @"input\grain_size5\";
            //input.GrainSize = 5;
            //input.GrainConductivity = 41E-9; // 41E-9 W/nm K // Paper: 41 W/mK 
            //input.BoundaryConductivity = 2.46E-9; // 2.46E-9 W/nm^2 K// Paper: 2.46E9 W/m^2K
            #endregion

            #region grain size = 10nm
            //input.NumElements = new int[] { 200, 200 };
            //input.MinCoords = new double[] { 0, 0 };
            //input.MaxCoords = new double[] { 300, 300 };
            //input.Thickness = 1; //Paper 4.65E-1 nm, it should not matter
            //input.PhasesInputDirectoryPath = ioPath + @"input\grain_size10\";
            //input.GrainSize = 10;
            //input.GrainConductivity = 41E-9; // 41E-9 W/nm K // Paper: 41 W/mK 
            //input.BoundaryConductivity = 2.46E-9; // 2.46E-9 W/nm^2 K// Paper: 2.46E9 W/m^2K
            #endregion

            #region grain size = 20nm
            //input.NumElements = new int[] { 200, 200 };
            //input.MinCoords = new double[] { 0, 0 };
            //input.MaxCoords = new double[] { 600, 600 };
            //input.Thickness = 1; //Paper 4.65E-1 nm, it should not matter
            //input.PhasesInputDirectoryPath = ioPath + @"input\grain_size20\";
            //input.GrainSize = 20;
            //input.GrainConductivity = 41E-9; // 41E-9 W/nm K // Paper: 41 W/mK 
            //input.BoundaryConductivity = 2.46E-9; // 2.46E-9 W/nm^2 K// Paper: 2.46E9 W/m^2K
            #endregion

            #region grain size = 50nm
            //input.NumElements = new int[] { 200, 200 };
            //input.MinCoords = new double[] { 0, 0 };
            //input.MaxCoords = new double[] { 1500, 1500 };
            //input.Thickness = 1; //Paper 4.65E-1 nm, it should not matter
            //input.PhasesInputDirectoryPath = ioPath + @"input\grain_size50\";
            //input.GrainSize = 10;
            //input.GrainConductivity = 41E-9; // 41E-9 W/nm K // Paper: 41 W/mK 
            //input.BoundaryConductivity = 2.46E-9; // 2.46E-9 W/nm^2 K// Paper: 2.46E9 W/m^2K
            #endregion

            #region grain size = 100nm
            //input.NumElements = new int[] { 200, 200 };
            //input.MinCoords = new double[] { 0, 0 };
            //input.MaxCoords = new double[] { 3000, 3000 };
            //input.Thickness = 1; //Paper 4.65E-1 nm, it should not matter
            //input.PhasesInputDirectoryPath = ioPath + @"input\grain_size100\";
            //input.GrainSize = 100;
            //input.GrainConductivity = 41E-9; // 41E-9 W/nm K // Paper: 41 W/mK 
            //input.BoundaryConductivity = 2.46E-9; // 2.46E-9 W/nm^2 K// Paper: 2.46E9 W/m^2K
            #endregion

            #region grain size = 200nm
            //input.NumElements = new int[] { 200, 200 };
            //input.MinCoords = new double[] { 0, 0 };
            //input.MaxCoords = new double[] { 6000, 6000 };
            //input.Thickness = 1; //Paper 4.65E-1 nm, it should not matter
            //input.PhasesInputDirectoryPath = ioPath + @"input\grain_size200\";
            //input.GrainSize = 20;
            //input.GrainConductivity = 41E-9; // 41E-9 W/nm K // Paper: 41 W/mK 
            //input.BoundaryConductivity = 2.46E-9; // 2.46E-9 W/nm^2 K// Paper: 2.46E9 W/m^2K
            #endregion

            #region grain size = 500nm
            //input.NumElements = new int[] { 200, 200 };
            //input.MinCoords = new double[] { 0, 0 };
            //input.MaxCoords = new double[] { 15000, 15000 };
            //input.Thickness = 1; //Paper 4.65E-1 nm, it should not matter
            //input.PhasesInputDirectoryPath = ioPath + @"input\grain_size500\";
            //input.GrainSize = 500;
            //input.GrainConductivity = 41E-9; // 41E-9 W/nm K // Paper: 41 W/mK 
            //input.BoundaryConductivity = 2.46E-9; // 2.46E-9 W/nm^2 K// Paper: 2.46E9 W/m^2K
            #endregion

            #region grain size = 1000nm
            //input.NumElements = new int[] { 200, 200 };
            //input.MinCoords = new double[] { 0, 0 };
            //input.MaxCoords = new double[] { 30000, 30000 };
            //input.Thickness = 1; //Paper 4.65E-1 nm, it should not matter
            //input.PhasesInputDirectoryPath = ioPath + @"input\grain_size1000\";
            //input.GrainSize = 1000;
            //input.GrainConductivity = 41E-9; // 41E-9 W/nm K // Paper: 41 W/mK 
            //input.BoundaryConductivity = 2.46E-9; // 2.46E-9 W/nm^2 K// Paper: 2.46E9 W/m^2K
            #endregion

            RunHomogenization(input);
        }

        private static void RunHomogenization(ProblemInput input)
        {
            string pathVoronoiSeeds = input.PhasesInputDirectoryPath + "voronoi_seeds.txt";
            string pathVoronoiVertices = input.PhasesInputDirectoryPath + "voronoi_vertices.txt";
            string pathVoronoiCells = input.PhasesInputDirectoryPath + "voronoi_cells.txt";

            var voronoiReader = new VoronoiReader2D();
            VoronoiDiagram2D voronoi =
                voronoiReader.ReadMatlabVoronoiDiagram(pathVoronoiSeeds, pathVoronoiVertices, pathVoronoiCells);
            GeometricModel geometricModel = new MultigrainPhaseReader().CreatePhasesFromVoronoi(voronoi);

            XModel physicalModel = CreatePhysicalModel(geometricModel, input);
            PrepareForAnalysis(physicalModel, geometricModel);

            // Analysis
            Vector2 temperatureGradient = Vector2.Create(0, 0);
            //var solver = (new SkylineSolver.Builder()).BuildSolver(physicalModel);
            SuiteSparseSolver solver = new SuiteSparseSolver.Builder().BuildSolver(physicalModel);
            var provider = new ProblemThermalSteadyState(physicalModel, solver);
            var rve = new ThermalSquareRve(physicalModel, Vector2.CreateFromArray(input.MinCoords),
                Vector2.CreateFromArray(input.MaxCoords), input.Thickness, temperatureGradient);
            var homogenization = new HomogenizationAnalyzer(physicalModel, solver, provider, rve);

            homogenization.Initialize();
            homogenization.Solve();
            int numDofs = solver.LinearSystems[0].Matrix.NumRows;
            solver.Dispose();
            IMatrix Keff = homogenization.EffectiveConstitutiveTensors[subdomainID];

            Console.Write($"grain size = {input.GrainSize} - numDofs = {numDofs}");
            Console.WriteLine($" - Keff = [{Keff[0, 0]}, {Keff[0, 1]} ; {Keff[1, 0]}, {Keff[1, 1]}]");
        }

        public static void RunSingleAnalysisAndPlotting()
        {
            var input = new ProblemInput();
            input.NumElements = new int[] { 200, 200 };
            input.MinCoords = new double[] { -1, -1 };
            input.MaxCoords = new double[] { 1, 1 };
            input.Thickness = 1.0;
            input.PhasesInputDirectoryPath = ioPath + @"input\sample\";
            input.GrainConductivity = 41;
            input.BoundaryConductivity = 2.46E9;

            string pathVoronoiSeeds = input.PhasesInputDirectoryPath + "voronoi_seeds.txt";
            string pathVoronoiVertices = input.PhasesInputDirectoryPath + "voronoi_vertices.txt";
            string pathVoronoiCells = input.PhasesInputDirectoryPath + "voronoi_cells.txt";
            var voronoiReader = new VoronoiReader2D();
            VoronoiDiagram2D voronoi = 
                voronoiReader.ReadMatlabVoronoiDiagram(pathVoronoiSeeds, pathVoronoiVertices, pathVoronoiCells);
            GeometricModel geometricModel = new MultigrainPhaseReader().CreatePhasesFromVoronoi(voronoi);
            var paths = new OutputPaths();
            paths.FillAllForDirectory(ioPath + @"output");

            PlotPhasesInteractions(() => geometricModel, paths, input);
        }

        private static void PlotPhasesInteractions(Func<GeometricModel> genPhases, OutputPaths paths, ProblemInput input)
        {
            GeometricModel geometricModel = genPhases();
            XModel physicalModel = CreatePhysicalModel(geometricModel, input);
            PrepareForAnalysis(physicalModel, geometricModel);
            
            // Plot stuff
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

            double elementSize = (input.MaxCoords[0] - input.MinCoords[0]) / input.NumElements[0];
            var junctionPlotter = new JunctionPlotter(physicalModel, geometricModel, elementSize);
            junctionPlotter.PlotJunctionElements(paths.junctionElements);


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



        private static XModel CreatePhysicalModel(GeometricModel geometricModel, ProblemInput input)
        {
            var physicalModel = new XModel();
            physicalModel.Subdomains[subdomainID] = new XSubdomain(subdomainID);

            // Mesh generation
            var meshGen = new UniformMeshGenerator2D<XNode>(input.MinCoords[0], input.MinCoords[1], 
                input.MaxCoords[0], input.MaxCoords[1], input.NumElements[0], input.NumElements[1]);
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
            var materialField = new MultigrainMaterialField(input.GrainConductivity, input.BoundaryConductivity, specialHeatCoeff);

            // Elements
            var factory = new XThermalElement2DFactory(materialField, input.Thickness, volumeIntegration, boundaryIntegration);
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

            // Left side: T = +315
            double minX = physicalModel.Nodes.Select(n => n.X).Min();
            foreach (var node in physicalModel.Nodes.Where(n => Math.Abs(n.X - minX) <= meshTol))
            {
                node.Constraints.Add(new Constraint() { DOF = ThermalDof.Temperature, Amount = +315 });
            }

            // Right side: T = +285
            double maxX = physicalModel.Nodes.Select(n => n.X).Max();
            foreach (var node in physicalModel.Nodes.Where(n => Math.Abs(n.X - maxX) <= meshTol))
            {
                node.Constraints.Add(new Constraint() { DOF = ThermalDof.Temperature, Amount = +285 });
            }
        }

        private static void PrepareForAnalysis(XModel physicalModel, GeometricModel geometricModel)
        {
            physicalModel.ConnectDataStructures();

            geometricModel.AssossiatePhasesNodes(physicalModel);
            geometricModel.AssociatePhasesElements(physicalModel);
            geometricModel.FindJunctions(physicalModel);
            geometricModel.FindConformingMesh(physicalModel);

            ISingularityResolver singularityResolver = new RelativeAreaResolver(geometricModel, singularityRelativeAreaTolerance);
            //var nodeEnricher = new NodeEnricherOLD(geometricModel, singularityResolver);
            if (fixedEnrichment)
            {
                var nodeEnricher = new NodeEnricher_v3(physicalModel, geometricModel, singularityResolver);
                nodeEnricher.ApplyEnrichments();
            }
            else
            {
                var nodeEnricher = new NodeEnricher2Junctions(geometricModel, singularityResolver);
                nodeEnricher.ApplyEnrichments();
            }

            physicalModel.UpdateDofs();
            physicalModel.UpdateMaterials();
        }

        private static IVectorView RunAnalysis(XModel physicalModel)
        {
            Console.WriteLine("Starting analysis");
            SuiteSparseSolver solver = new SuiteSparseSolver.Builder().BuildSolver(physicalModel);
            //SkylineSolver solver = new SkylineSolver.Builder().BuildSolver(physicalModel);
            var problem = new ProblemThermalSteadyState(physicalModel, solver);
            var linearAnalyzer = new LinearAnalyzer(physicalModel, solver, problem);
            var staticAnalyzer = new StaticAnalyzer(physicalModel, solver, problem, linearAnalyzer);

            staticAnalyzer.Initialize();
            staticAnalyzer.Solve();

            Console.WriteLine("Analysis finished");


            return solver.LinearSystems[subdomainID].Solution;
        }

        private class ProblemInput
        {
            public double GrainConductivity { get; set; } = double.NaN; // Paper: 41 W/mK 

            public double BoundaryConductivity { get; set; } = double.NaN; // Paper: 2.46E9 W/m^2K

            public int[] NumElements { get; set; }

            public double[] MinCoords { get; set; }

            public double[] MaxCoords { get; set; }

            public double Thickness { get; set; } = double.NaN;

            public double GrainSize { get; set; } = double.NaN;

            public string PhasesInputDirectoryPath { get; set; }
        }
    }
}
