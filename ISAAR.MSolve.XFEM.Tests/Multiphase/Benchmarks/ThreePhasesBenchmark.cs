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
using ISAAR.MSolve.Geometry.Coordinates;
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

namespace ISAAR.MSolve.XFEM_OLD.Tests.Multiphase.Plotting
{
    public static class ThreePhasesBenchmark
    {
        private const int subdomainID = 0;
        private const double minX = 0.0, minY = 0.0, maxX = 2.0, maxY = 2.0;
        private const double thickness = 1.0;
        private const double specialHeatCoeff = 1.0;

        public static void RunTest()
        {
            // Parameters
            int numElementsX = 41, numElementsY = 41;
            double elementSize = (maxX - minX) / numElementsX;
            bool integrationWithSubtriangles = true;
            double conductivity0 = 100/*1E1*/ , conductivity1 = 1000/*1E2*/, conductivity2 = 1/*1E3*/;
            double interface01Conductivity = 100, interface02Conductivity = 10/*1E10*/, interface12Conductivity = 1000/*1E10*/;
            bool daux = false;
            int numJunctions = 2;
            double singularityRelativeAreaTolerance = 1E-4;
            string outputDirectory = @"C:\Users\Serafeim\Desktop\HEAT\Paper\ThreePhases";

            // Geometry
            GeometricModel geometricModel = CreatePhases(numElementsX);
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
            materialField.RegisterBoundaryMaterial(phase0, phase1, interface01Conductivity);
            materialField.RegisterBoundaryMaterial(phase0, phase2, interface02Conductivity);
            materialField.RegisterBoundaryMaterial(phase1, phase2, interface12Conductivity);

            // FE Model
            XModel physicalModel = CreatePhysicalModel(geometricModel, integrationWithSubtriangles, numElementsX, numElementsY,
                materialField);

            // Analysis
            PrepareForAnalysis(physicalModel, geometricModel, daux, numJunctions, singularityRelativeAreaTolerance);
            IVectorView solution = RunAnalysis(physicalModel);

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
                //double T = node.Y < 0 ? 0.0 : -100;
                double T = -100;
                node.Constraints.Add(new Constraint() { DOF = ThermalDof.Temperature, Amount = T });
            }
        }

        private static GeometricModel CreatePhases(int numElementsPerAxis)
        {
            // --------C--------
            // |       |       |
            // |       |   1   |
            // |       |       |
            // |   0   B-------D
            // |       |       |
            // |       |   2   |
            // |       |       |
            // --------A--------

            double middleX = 0.5 * (minX + maxX);
            double middleY = 0.5 * (minY + maxY);
            var A = new CartesianPoint(middleX, minY);
            var B = new CartesianPoint(middleX, middleY);
            var C = new CartesianPoint(middleX, maxY);
            var D = new CartesianPoint(maxX, middleY);

            // Define phases
            var phase0 = new ConvexPhase(0);
            var phase1 = new ConvexPhase(1);
            var phase2 = new ConvexPhase(2);

            // Create boundaries and associate them with their phases
            var AB = new PhaseBoundary(new XFEM_OLD.Multiphase.Geometry.LineSegment2D(A, B), phase0, phase2);
            var BC = new PhaseBoundary(new XFEM_OLD.Multiphase.Geometry.LineSegment2D(B, C), phase0, phase1);
            var BD = new PhaseBoundary(new XFEM_OLD.Multiphase.Geometry.LineSegment2D(B, D), phase1, phase2);

            // Initialize model
            var geometricModel = new GeometricModel();
            double elementSize = (maxX - minX) / numElementsPerAxis;
            geometricModel.MeshTolerance = new UserDefinedMeshTolerance(elementSize);
            geometricModel.Phases.Add(phase0);
            geometricModel.Phases.Add(phase1);
            geometricModel.Phases.Add(phase2);

            return geometricModel;
        }

        private static XModel CreatePhysicalModel(GeometricModel geometricModel, bool integrationWithSubtriangles,
            int numElementsX, int numElementsY, IThermalMaterialField materialField)
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

        private static void PrepareForAnalysis(XModel physicalModel, GeometricModel geometricModel, bool daux, int numJunctions,
            double singularityRelativeAreaTolerance)
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
            solver.PreventFromOverwrittingSystemMatrices();
            var problem = new ProblemThermalSteadyState(physicalModel, solver);
            var linearAnalyzer = new LinearAnalyzer(physicalModel, solver, problem);
            var staticAnalyzer = new StaticAnalyzer(physicalModel, solver, problem, linearAnalyzer);

            staticAnalyzer.Initialize();
            staticAnalyzer.Solve();

            #region debug
            //string path = @"C:\Users\Serafeim\Desktop\HEAT\debug\Kglob.txt";
            //var writer = new LinearAlgebra.Output.FullMatrixWriter();
            //writer.WriteToFile(solver.LinearSystems[0].Matrix, path);
            #endregion

            return solver.LinearSystems[subdomainID].Solution;
        }
    }
}
