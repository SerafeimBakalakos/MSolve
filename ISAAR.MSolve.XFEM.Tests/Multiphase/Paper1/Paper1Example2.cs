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
    public static class Paper1Example2
    {
        private const int numElements = 70;
        //private const int numElements = 400;
        private const int numElementsX = numElements, numElementsY = numElements;
        private const int subdomainID = 0;
        private const double minX = 0, minY = 0, maxX = 2000, maxY = 2000;
        //private const double minX = -1.0, minY = -1.0, maxX = 1.0, maxY = 1.0;
        private const double elementSize = (maxX - minX) / numElementsX;
        private const double thickness = 1.0;
        private static readonly PhaseGenerator generator = new PhaseGenerator(minX, maxX, numElementsX);
        private const bool integrationWithSubtriangles = true;
        
        private const double specialHeatCoeff = 1.0;
        private const double singularityRelativeAreaTolerance = 1E-8;
        private const bool fixedEnrichment = true;

        private static int[][] phasesToKeep =
        {
            new int[] { 2, 102, 30, 130, 36, 136 },
            new int[] { 30, 130, 11, 111, 25, 125, 16, 116 },
            new int[] { 38,138, 21,121, 15,115 },
            new int[] { 3,103, 7,107, 40,140 },
            new int[] { 7,107, 20,120, 33,133 },
            new int[] { 18,118, 12,112, 14,114 },
            new int[] { 6,106, 35,135, 37,137 },
            new int[] { 26,126, 8,108, 39,139 }
        };

        public static void RunSingleAnalysisAndPlotting()
        {
            var phaseReader = new PhaseReader(true, 0);
            string directory = @"C:\Users\Serafeim\Desktop\HEAT\Paper\Paper1Example2\";
            string matrixLayersFile = directory + "boundaries.txt";
            string inclusionsFile = directory + "CNTnodes.txt";
            GeometricModel geometricModel = phaseReader.ReadPhasesFromFile(matrixLayersFile, inclusionsFile);
            //KeepOnlyPhases(geometricModel, phasesToKeep[5]);
            var paths = new OutputPaths();
            paths.FillAllForDirectory(@"C:\Users\Serafeim\Desktop\HEAT\Paper\Paper1Example2");
            PlotPhasesInteractions(() => geometricModel, paths);
        }

        public static void RunParametricHomogenization()
        {
            double[] inclusionLayerInterfaceCondictivities =
            {
                1E-5,
                1E-4,
                1E-3,
                1E-2,
                //1E-1,
                0.099,
                1E0,
                //1E1,
                9.99,
                1E2,
                1E3,
                //1E4,
                //9999,
                //1E5,
            };
            double[] matrixLayerInterfaceCondictivities =
            {
                1E-5,
                1E-4,
                1E-3,
                //1E-2,
                0.009,
                1E-1,
                1E0,
                1E1,
                1E2,
                1E3,
                //1E4,
                //9999,
                //1E5,
                //99999
            };


            Console.WriteLine();
            Console.WriteLine("matrix-layer conductivity | inclusion-layer conductivity | effective conductivity XX");
            for (int i = 0; i < matrixLayerInterfaceCondictivities.Length; ++i)
            {
                for (int j = 0; j < inclusionLayerInterfaceCondictivities.Length; ++j)
                {
                    var conductivities = new Conductivities
                    {
                        Matrix = 0.2,
                        Inclusion = 2000,
                        Layer = 0.3,
                        MatrixLayerInterface = matrixLayerInterfaceCondictivities[i],
                        LayerLayerInterface = 1E3,
                        InclusionLayerInterface = inclusionLayerInterfaceCondictivities[j],
                    };

                    try
                    {
                        IMatrix effectiveConductivity = RunHomogenization(conductivities);
                        Console.WriteLine(matrixLayerInterfaceCondictivities[i] + "\t\t" +
                            inclusionLayerInterfaceCondictivities[j] + "\t\t" + effectiveConductivity[0, 0]);
                    }
                    catch (IndefiniteMatrixException ex)
                    {
                        Console.WriteLine(matrixLayerInterfaceCondictivities[i] + " " +
                            inclusionLayerInterfaceCondictivities[j] + " singular matrix exception");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(matrixLayerInterfaceCondictivities[i] + " " +
                            inclusionLayerInterfaceCondictivities[j] + " something else happenned");
                    }
                }
            }

            //Console.WriteLine();
            //Console.WriteLine("inclusion-layer conductivity | effective conductivity XX");
            //for (int i = 0; i < inclusionLayerInterfaceCondictivities.Length; ++i)
            //{
            //    var conductivities = new Conductivities
            //    {
            //        Matrix = 0.2,
            //        Inclusion = 2000,
            //        Layer = 0.3,
            //        MatrixLayerInterface = 0.25,
            //        LayerLayerInterface = 1E3,
            //        InclusionLayerInterface = inclusionLayerInterfaceCondictivities[i],
            //    };

            //    try
            //    {
            //        IMatrix effectiveConductivity = RunHomogenization(conductivities);
            //        Console.WriteLine(
            //            inclusionLayerInterfaceCondictivities[i] + " " + effectiveConductivity[0, 0]);
            //    }
            //    catch (IndefiniteMatrixException ex)
            //    {
            //        Console.WriteLine(
            //            inclusionLayerInterfaceCondictivities[i] + " singular matrix exception");
            //    }
            //    catch (Exception ex)
            //    {
            //        Console.WriteLine(
            //            inclusionLayerInterfaceCondictivities[i] + " something else happenned");
            //    }
            //}


            //Console.WriteLine();
            //Console.WriteLine("matrix-layer conductivity | effective conductivity XX");
            //for (int i = 0; i < matrixLayerInterfaceCondictivities.Length; ++i)
            //{
            //    var conductivities = new Conductivities
            //    {
            //        Matrix = 0.2, Inclusion = 2000, Layer = 0.3,
            //        MatrixLayerInterface = matrixLayerInterfaceCondictivities[i],
            //        LayerLayerInterface = 1E3, InclusionLayerInterface = 1E3,
            //    };

            //    try
            //    {
            //        IMatrix effectiveConductivity = RunHomogenization(conductivities);
            //        Console.WriteLine(matrixLayerInterfaceCondictivities[i] + " " + effectiveConductivity[0, 0]);
            //    }
            //    catch (IndefiniteMatrixException ex)
            //    {
            //        Console.WriteLine(matrixLayerInterfaceCondictivities[i] + " singular matrix exception");
            //    }
            //    catch (Exception ex)
            //    {
            //        Console.WriteLine(matrixLayerInterfaceCondictivities[i] + " something else happenned");
            //    }
            //}

            double[] layerLayerInterfaceCondictivities = 
            {
                //1E-5,
                //1E-4,
                //1E-3,
                //1E-2,
                //1E-1,
                //1E0,
                //1E1,
                //1E2,
                //1E3,
                //1E4,
                //1E5,
                //99999 
            };
            Console.WriteLine();
            Console.WriteLine("layer-layer conductivity | effective conductivity XX");
            for (int i = 0; i < layerLayerInterfaceCondictivities.Length; ++i)
            {
                var conductivities = new Conductivities
                {
                    Matrix = 0.2,
                    Inclusion = 2000,
                    Layer = 0.3,
                    MatrixLayerInterface = 0.25,
                    LayerLayerInterface = layerLayerInterfaceCondictivities[i],
                    InclusionLayerInterface = 1E3,
                };

                try
                {
                    IMatrix effectiveConductivity = RunHomogenization(conductivities);
                    Console.WriteLine(layerLayerInterfaceCondictivities[i] + " " + effectiveConductivity[0, 0]);
                }
                catch (IndefiniteMatrixException ex)
                {
                    Console.WriteLine(layerLayerInterfaceCondictivities[i] + " singular matrix exception");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(layerLayerInterfaceCondictivities[i] + " something else happenned");
                }
            }
        }

        private static IMatrix RunHomogenization(Conductivities conductivities)
        {
            string directory = @"C:\Users\Serafeim\Desktop\HEAT\Paper\Paper1Example2\";
            string matrixLayersFile = directory + "boundaries.txt";
            string inclusionsFile = directory + "CNTnodes.txt";
            var phaseReader = new PhaseReader(true, 0);
            GeometricModel geometricModel = phaseReader.ReadPhasesFromFile(matrixLayersFile, inclusionsFile);

            XModel physicalModel = CreatePhysicalModel(geometricModel, conductivities);
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

            solver.Dispose();
            return homogenization.EffectiveConstitutiveTensors[subdomainID];
        }

        private static void PlotPhasesInteractions(Func<GeometricModel> genPhases, OutputPaths paths)
        {
            var conductivities = new Conductivities
            {
                Matrix = 0.2, // Paper: 0.2
                Inclusion = 2000, // Paper: 2000
                Layer = 0.3, // 0.3
                MatrixLayerInterface = 0.25, // guess: 0.25
                LayerLayerInterface = 1E3, // guess: 1E3
                InclusionLayerInterface = 1E3, // paper: 1E-5
            };

            GeometricModel geometricModel = genPhases();
            XModel physicalModel = CreatePhysicalModel(geometricModel, conductivities);
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

        private static XModel CreatePhysicalModel(GeometricModel geometricModel, Conductivities conductivities)
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
            var matrixMaterial = new ThermalMaterial(conductivities.Matrix, specialHeatCoeff);
            var inclusionMaterial = new ThermalMaterial(conductivities.Inclusion, specialHeatCoeff);
            var layerMaterial = new ThermalMaterial(conductivities.Layer, specialHeatCoeff);
            var materialField = new MatrixInclusionsLayersMaterialField(matrixMaterial, inclusionMaterial, layerMaterial,
                conductivities.MatrixLayerInterface, conductivities.LayerLayerInterface, conductivities.InclusionLayerInterface, 
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
            geometricModel.FindJunctions(physicalModel);
            geometricModel.FindConformingMesh(physicalModel);

            ISingularityResolver singularityResolver = new RelativeAreaResolver(geometricModel, singularityRelativeAreaTolerance);
            //var nodeEnricher = new NodeEnricherOLD(geometricModel, singularityResolver);
            if (fixedEnrichment)
            {
                //var nodeEnricher = new NodeEnricher_v2(physicalModel, geometricModel, singularityResolver);
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

        private static void KeepOnlyPhases(GeometricModel geometricModel, int[] phasesToKeep)
        {
            IPhase defaultPhase = geometricModel.Phases[0];
            var phases = new HashSet<int>(phasesToKeep);
            phases.Add(0);
            foreach (IPhase phase in geometricModel.Phases.ToArray())
            {
                if (phases.Contains(phase.ID))
                {
                    foreach (IPhase neighbor in phase.Neighbors.ToArray())
                    {
                        if (!phases.Contains(neighbor.ID)) phase.Neighbors.Remove(neighbor);
                    }

                    foreach (PhaseBoundary boundary in phase.Boundaries.ToArray())
                    {
                        
                        if (boundary.PositivePhase == phase)
                        {
                            if (!phases.Contains(boundary.NegativePhase.ID))
                            {
                                phase.Boundaries.Remove(boundary);
                                if (phase.ID != 0)
                                {
                                    new PhaseBoundary(boundary.Segment, phase, defaultPhase);
                                }
                            }
                        }
                        else
                        {
                            if (!phases.Contains(boundary.PositivePhase.ID))
                            {
                                phase.Boundaries.Remove(boundary);
                                if (phase.ID != 0)
                                {
                                    new PhaseBoundary(boundary.Segment, defaultPhase, phase);
                                }
                            }
                        }

                        bool keep = phases.Contains(boundary.PositivePhase.ID);
                        keep &= phases.Contains(boundary.NegativePhase.ID);
                        if (!keep) phase.Boundaries.Remove(boundary);
                    }
                }
                else
                {
                    geometricModel.Phases.Remove(phase);
                }
            }
        }

        private class Conductivities
        {
            public double Matrix { get; set; }
            public double Inclusion { get; set; }
            public double Layer { get; set; }

            public double MatrixLayerInterface { get; set; }
            public double LayerLayerInterface { get; set; }
            public double InclusionLayerInterface { get; set; }

        }
    }
}
