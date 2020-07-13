﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ISAAR.MSolve.Analyzers;
using ISAAR.MSolve.Discretization;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Discretization.Integration.Quadratures;
using ISAAR.MSolve.Discretization.Mesh;
using ISAAR.MSolve.Discretization.Mesh.Generation;
using ISAAR.MSolve.Discretization.Mesh.Generation.Custom;
using ISAAR.MSolve.Discretization.Providers;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.LinearAlgebra.Exceptions;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Problems;
using ISAAR.MSolve.Solvers.Direct;
using ISAAR.MSolve.XFEM_OLD.Multiphase.Elements;
using ISAAR.MSolve.XFEM_OLD.Multiphase.Enrichment;
using ISAAR.MSolve.XFEM_OLD.Multiphase.Enrichment.SingularityResolution;
using ISAAR.MSolve.XFEM_OLD.Multiphase.Entities;
using ISAAR.MSolve.XFEM_OLD.Multiphase.Geometry;
using ISAAR.MSolve.XFEM_OLD.Multiphase.Integration;
using ISAAR.MSolve.XFEM_OLD.Multiphase.Materials;
using ISAAR.MSolve.XFEM_OLD.Multiphase.Plotting;
using ISAAR.MSolve.XFEM_OLD.Multiphase.Plotting.Enrichments;
using ISAAR.MSolve.XFEM_OLD.Multiphase.Plotting.Fields;
using ISAAR.MSolve.XFEM_OLD.Multiphase.Plotting.Mesh;
using ISAAR.MSolve.XFEM_OLD.Multiphase.Plotting.Writers;
using ISAAR.MSolve.XFEM_OLD.Tests.Multiphase.Plotting;

namespace ISAAR.MSolve.XFEM_OLD.Tests.Multiphase.Singularity
{
    public static class TwoVerticalInclusions
    {
        private const int subdomainID = 0;
        private const double minX = 0.6, minY = -0.24, maxX = 0.76, maxY = -0.12;
        private const int numElementsX = 4, numElementsY = 3;
        //private const int numElementsX = 8, numElementsY = 6;
        private const double elementSize = (maxX - minX) / numElementsX;
        private const double thickness = 1.0;
        private const double specialHeatCoeff = 1.0;
        private const double conductivity0 = 1E0, conductivity1 = 1E5, conductivity2 = 1E5;
        private const double conductivityInterface01 = 1E10, conductivityInterface02 = 1E10;
        private const double singularityRelativeAreaTolerance = 1E-6;

        public static void RunTest()
        {
            // Parameters
            bool integrationWithSubtriangles = true;
            string outputDirectory = @"C:\Users\Serafeim\Desktop\HEAT\Paper\TwoVerticalInclusions";

            // Geometry
            GeometricModel geometricModel = CreatePhases();
            IPhase phase0 = geometricModel.Phases[0];
            IPhase phase1 = geometricModel.Phases[1];
            IPhase phase2 = geometricModel.Phases[2];

            // Materials
            var material0 = new ThermalMaterial(conductivity0, specialHeatCoeff);
            var material1 = new ThermalMaterial(conductivity1, specialHeatCoeff);
            var material2 = new ThermalMaterial(conductivity2, specialHeatCoeff);
            var materialField = new GeneralMultiphaseMaterial();
            materialField.RegisterPhaseMaterial(phase0, material0);
            materialField.RegisterPhaseMaterial(phase1, material1);
            materialField.RegisterPhaseMaterial(phase2, material2);
            materialField.RegisterBoundaryMaterial(phase0, phase1, conductivityInterface01);
            materialField.RegisterBoundaryMaterial(phase0, phase2, conductivityInterface02);

            // FE Model
            XModel physicalModel = CreatePhysicalModel(geometricModel, integrationWithSubtriangles, materialField);

            // Analysis
            PrepareForAnalysis(physicalModel, geometricModel);
            (SkylineMatrix matrix, Vector solution) = RunAnalysis(physicalModel);
            
            // Print matrix
            string path = @"C:\Users\Serafeim\Desktop\HEAT\debug\Kglob.txt";
            var writer = new LinearAlgebra.Output.FullMatrixWriter();
            writer.WriteToFile(matrix, path);

            // Print ordering
            DofTable dofs = physicalModel.Subdomains[subdomainID].FreeDofOrdering.FreeDofs;
            Console.WriteLine(dofs);

            //Plots
            var paths = new OutputPaths();
            paths.FillAllForDirectory(outputDirectory);
            PlotPhasesInteractions(geometricModel, physicalModel, paths, elementSize, solution);
        }

        private static void ApplyBoundaryConditions(XModel physicalModel)
        {
            double meshTol = 1E-7;

            // Left side: T = +100
            double minX = physicalModel.Nodes.Select(n => n.X).Min();
            foreach (var node in physicalModel.Nodes.Where(n => Math.Abs(n.X - minX) <= meshTol))
            {
                double T = +100;
                node.Constraints.Add(new Constraint() { DOF = ThermalDof.Temperature, Amount = T });
            }

            // Right side: T = -100
            double maxX = physicalModel.Nodes.Select(n => n.X).Max();
            foreach (var node in physicalModel.Nodes.Where(n => Math.Abs(n.X - maxX) <= meshTol))
            {
                double T = -100;
                node.Constraints.Add(new Constraint() { DOF = ThermalDof.Temperature, Amount = T });
            }
        }

        private static GeometricModel CreatePhases()
        {
            // Phase 0
            var phase0 = new DefaultPhase();

            // Phase 1
            var phase1 = new ConvexPhase(1);
            var A1 = new CartesianPoint(0.646187096384893, -0.24903818695655);
            var A2 = new CartesianPoint(0.669487160909297, 0.150282620156176);
            var A3 = new CartesianPoint(0.629555080198024, 0.152612626608616);
            var A4 = new CartesianPoint(0.60625501567362, -0.24670818050411);
            var A12 = new PhaseBoundary(new LineSegment2D(A1, A2), phase1, phase0);
            var A23 = new PhaseBoundary(new LineSegment2D(A2, A3), phase1, phase0);
            var A34 = new PhaseBoundary(new LineSegment2D(A3, A4), phase1, phase0);
            var A41 = new PhaseBoundary(new LineSegment2D(A4, A1), phase1, phase0);


            // Phase 2
            var phase2 = new ConvexPhase(2);
            var B1 = new CartesianPoint(0.749600321096544, -0.210850234662684);
            var B2 = new CartesianPoint(0.865618032277769, 0.171955056173623);
            var B3 = new CartesianPoint(0.827337503194138, 0.183556827291746);
            var B4 = new CartesianPoint(0.711319792012913, -0.199248463544562);
            var B12 = new PhaseBoundary(new LineSegment2D(B1, B2), phase2, phase0);
            var B23 = new PhaseBoundary(new LineSegment2D(B2, B3), phase2, phase0);
            var B34 = new PhaseBoundary(new LineSegment2D(B3, B4), phase2, phase0);
            var B41 = new PhaseBoundary(new LineSegment2D(B4, B1), phase2, phase0);

            // Initialize model
            var geometricModel = new GeometricModel();
            geometricModel.MeshTolerance = new UserDefinedMeshTolerance(elementSize);
            geometricModel.Phases.Add(phase0);
            geometricModel.Phases.Add(phase1);
            geometricModel.Phases.Add(phase2);

            return geometricModel;
        }

        private static XModel CreatePhysicalModel(GeometricModel geometricModel, bool integrationWithSubtriangles,
            IThermalMaterialField materialField)
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
                    GaussLegendre2D.GetQuadratureWithOrder(2, 2), 2, GaussLegendre2D.GetQuadratureWithOrder(2, 2));
            }
            IBoundaryIntegration boundaryIntegration = new LinearBoundaryIntegration(GaussLegendre1D.GetQuadratureWithOrder(2));

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

        private static void EnrichByHand(XModel physicalModel, GeometricModel geometricModel)
        {
            ////             B               D
            ////     p0      |       p1      |      p2
            ////             |               |    
            //// 6-------7---|---8-------9---|---10------11
            //// |       |   |   |       |   |   |       |
            //// |   e0  |   |e1 |  e2   |   |e3 |  e4   |
            //// |       |   |   |       |   |   |       |
            //// |       |   |   |       |   |   |       |
            //// 0-------1---|---2-------3---|---4-------5
            ////             |               |    
            ////             A               C

            //var leftEnrichment = new StepEnrichment(0, geometricModel.Phases[0], geometricModel.Phases[1]);
            //var rightEnrichment = new StepEnrichment(1, geometricModel.Phases[1], geometricModel.Phases[2]);
            //List<XNode> nodes = physicalModel.Nodes;

            //var leftBoundaryNodes = new HashSet<XNode>();
            //leftBoundaryNodes.Add(nodes[leftBoundaryInElement]);
            //leftBoundaryNodes.Add(nodes[leftBoundaryInElement + 1]);
            //leftBoundaryNodes.Add(nodes[leftBoundaryInElement + numElementsX + 1]);
            //leftBoundaryNodes.Add(nodes[leftBoundaryInElement + numElementsX + 2]);
            //foreach (XNode node in leftBoundaryNodes) node.Enrichments[leftEnrichment] = leftEnrichment.EvaluateAt(node);

            //var rightBoundaryNodes = new HashSet<XNode>();
            //rightBoundaryNodes.Add(nodes[rightBoundaryInElement]);
            //rightBoundaryNodes.Add(nodes[rightBoundaryInElement + 1]);
            //rightBoundaryNodes.Add(nodes[rightBoundaryInElement + numElementsX + 1]);
            //rightBoundaryNodes.Add(nodes[rightBoundaryInElement + numElementsX + 2]);
            //foreach (XNode node in rightBoundaryNodes) node.Enrichments[rightEnrichment] = rightEnrichment.EvaluateAt(node);
        }

        private static void EnrichAutomatically(XModel physicalModel, GeometricModel geometricModel)
        {
            ISingularityResolver singularityResolver = new RelativeAreaResolver(geometricModel, singularityRelativeAreaTolerance);
            var nodeEnricher = new NodeEnricherOLD(geometricModel, singularityResolver);
            nodeEnricher.ApplyEnrichments();
        }


        private static void PrepareForAnalysis(XModel physicalModel, GeometricModel geometricModel)
        {
            physicalModel.ConnectDataStructures();

            geometricModel.AssossiatePhasesNodes(physicalModel);
            geometricModel.AssociatePhasesElements(physicalModel);
            geometricModel.FindConformingMesh(physicalModel);

            //EnrichByHand(physicalModel, geometricModel);
            EnrichAutomatically(physicalModel, geometricModel);

            physicalModel.UpdateDofs();
            physicalModel.UpdateMaterials();
        }

        private static void PlotPhasesInteractions(GeometricModel geometricModel, XModel physicalModel, OutputPaths paths,
            double elementSize, IVectorView solution)
        {

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

        private static (SkylineMatrix matrix, Vector solution) RunAnalysis(XModel physicalModel)
        {
            SkylineSolver solver = new SkylineSolver.Builder().BuildSolver(physicalModel);
            solver.PreventFromOverwrittingSystemMatrices();
            var problem = new ProblemThermalSteadyState(physicalModel, solver);
            var linearAnalyzer = new LinearAnalyzer(physicalModel, solver, problem);
            var staticAnalyzer = new StaticAnalyzer(physicalModel, solver, problem, linearAnalyzer);
            staticAnalyzer.Initialize();

            var sys = solver.LinearSystems[subdomainID];
            try
            {
                staticAnalyzer.Solve();
            }
            catch (Exception)
            {
                Console.WriteLine("Matrix is singular");
                ((Vector)sys.Solution).SetAll(double.NaN);
            }

            return ((SkylineMatrix)sys.Matrix, (Vector)sys.Solution);
        }
    }
}