using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ISAAR.MSolve.Analyzers;
using ISAAR.MSolve.Analyzers.Multiscale;
using ISAAR.MSolve.Discretization;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Problems;
using ISAAR.MSolve.Solvers;
using ISAAR.MSolve.Solvers.Direct;
using MGroup.Geometry.Mesh;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Enrichment.Enrichers;
using MGroup.XFEM.Entities;
using MGroup.XFEM.Geometry.LSM;
using MGroup.XFEM.Geometry.LSM.DualMesh;
using MGroup.XFEM.Geometry.Mesh;
using MGroup.XFEM.Geometry.Primitives;
using MGroup.XFEM.Integration;
using MGroup.XFEM.Integration.Quadratures;
using MGroup.XFEM.Materials;
using MGroup.XFEM.Output;
using MGroup.XFEM.Output.Fields;
using MGroup.XFEM.Output.Mesh;
using MGroup.XFEM.Output.Vtk;
using MGroup.XFEM.Output.Writers;
using MGroup.XFEM.Phases;

namespace MGroup.XFEM.Heat
{
    public class CntsMatrixModel
    {
        private const int defaultPhaseID = 0;
        private const int lsmType = 1; // 0 = simple, 1 = dual global, 2 = dual local (saves a few MB, but is currently much slower)
        private const bool cartesianMesh = false;
        private const bool mergeIntersectingCNTs = false;
        private static readonly string outputDirectory = @"C:\Users\Serafeim\Desktop\HEAT\Phase3\CntsMatrix";
        private readonly int boundaryIntegrationOrder = 2;

        private XModel<IXMultiphaseElement> model;

        public CntsMatrixModel()
        {
        }

        public double[] CoordsMin { get; set; }

        public double[] CoordsMax { get; set; }

        public int[] NumHexaCoarse { get; set; } = { 10, 10, 10 };

        public int[] NumHexaFine { get; set; } = { 60, 60, 60 };

        public double ConductivityMatrix { get; set; }

        public double ConductivityCNT { get; set; }

        public double ConductivityInterface { get; set; }

        public ICntGeometryGenerator GeometryGenerator { get; set; }

        public void BuildModel()
        {
            IDualMesh dualMesh = new DualCartesianMesh3D.Builder(CoordsMin, CoordsMax, NumHexaCoarse, NumHexaFine).BuildMesh();
            if (cartesianMesh)
            {
                dualMesh = new DualCartesianMesh3D.Builder(CoordsMin, CoordsMax, NumHexaCoarse, NumHexaFine).BuildMesh();
            }
            else
            {
                int[] numNodesCoarse = { NumHexaCoarse[0] + 1, NumHexaCoarse[1] + 1, NumHexaCoarse[2] + 1 };
                int[] numNodesFine = { NumHexaFine[0] + 1, NumHexaFine[1] + 1, NumHexaFine[2] + 1 };
                dualMesh = new DualCartesianSimplicialMesh3D.Builder(
                    CoordsMin, CoordsMax, numNodesCoarse, numNodesFine).BuildMesh();
            }

            model = BuildPhysicalModel(dualMesh.CoarseMesh);
            PhaseGeometryModel geometryModel = CreatePhases(model, dualMesh);
            model.GeometryModel = geometryModel;
        }

        public void PlotModel()
        {
            var geometryModel = (PhaseGeometryModel)model.GeometryModel;

            // Plot level sets
            geometryModel.GeometryObservers.Add(new PhaseLevelSetPlotter(outputDirectory, model, geometryModel));

            // Plot phases of nodes
            geometryModel.InteractionObservers.Add(new NodalPhasesPlotter(outputDirectory, model));

            // Plot element - phase boundaries interactions
            geometryModel.InteractionObservers.Add(new LsmElementIntersectionsPlotter(outputDirectory, model));

            // Plot element subcells
            model.ModelObservers.Add(new ConformingMeshPlotter(outputDirectory, model));

            // Plot phases of each element subcell
            model.ModelObservers.Add(new ElementPhasePlotter(outputDirectory, model, geometryModel, defaultPhaseID));

            // Write the size of each phase
            model.ModelObservers.Add(new PhasesSizeWriter(outputDirectory, model, geometryModel));

            // Plot bulk and boundary integration points of each element
            model.ModelObservers.Add(new IntegrationPointsPlotter(outputDirectory, model));

            // Plot intersected elements
            model.ModelObservers.Add(new PhaseInteractingElementsPlotter(outputDirectory, model));

            // Plot enrichments
            double elementSize = (CoordsMax[0] - CoordsMin[0]) / NumHexaCoarse[0];
            model.RegisterEnrichmentObserver(new PhaseEnrichmentPlotter(outputDirectory, model, elementSize, 3));

            // Initialize model state so that everything described above can be tracked
            model.Initialize();
        }

        public void RunAnalysisAndPlot(ISolverBuilder solverBuilder = null)
        {
            if (solverBuilder == null)
            {
                solverBuilder = new SuiteSparseSolver.Builder();
            }

            ApplyBoundaryConditions();
            model.Initialize();
            IVectorView solution = RunSteadyStateAnalysis(solverBuilder);

            // Temperature field
            var conformingMesh = new ConformingOutputMesh(model);
            using (var writer = new VtkFileWriter(outputDirectory + "//temperature_field.vtk"))
            {
                var temperatureField = new TemperatureField(model, conformingMesh);
                writer.WriteMesh(conformingMesh);
                writer.WriteScalarField("temperature", conformingMesh, temperatureField.CalcValuesAtVertices(solution));
            }

            // Heat flux at Gauss Points
            using (var writer = new VtkPointWriter(outputDirectory + "//heat_flux_at_GPs.vtk"))
            {
                var fluxField = new HeatFluxAtGaussPointsField(model, true);
                writer.WriteVectorField("heat_flux", fluxField.CalcValuesAtVertices(solution));
            }
        }

        public double[,] RunHomogenization(ISolverBuilder solverBuilder = null)
        {
            if (solverBuilder == null)
            {
                solverBuilder = new SuiteSparseSolver.Builder();
            }

            // Run homogenization analysis
            model.Initialize();
            Console.WriteLine("Starting homogenization analysis");
            ISolver solver = solverBuilder.BuildSolver(model);
            var provider = new ProblemThermalSteadyState(model, solver);
            var rve = new ThermalCubicRve(model, CoordsMin, CoordsMax);
            var homogenization = new HomogenizationAnalyzer(model, solver, provider, rve);

            homogenization.Initialize();
            homogenization.Solve();
            IMatrix conductivity = homogenization.MacroscopicModulus;
            Console.WriteLine("Analysis finished");

            return conductivity.CopytoArray2D();
        }

        private void ApplyBoundaryConditions()
        {
            // Boundary conditions
            double meshTol = 1E-7;

            // Left side
            double minX = model.XNodes.Select(n => n.X).Min();
            foreach (var node in model.XNodes.Where(n => Math.Abs(n.X - minX) <= meshTol))
            {
                node.Constraints.Add(new Constraint() { DOF = ThermalDof.Temperature, Amount = 100 });
            }

            // Right side
            double maxX = model.XNodes.Select(n => n.X).Max();
            foreach (var node in model.XNodes.Where(n => Math.Abs(n.X - maxX) <= meshTol))
            {
                node.Constraints.Add(new Constraint() { DOF = ThermalDof.Temperature, Amount = -100 });
            }
        }

        private XModel<IXMultiphaseElement> BuildPhysicalModel(IStructuredMesh mesh)
        {
            var model = new XModel<IXMultiphaseElement>(3);
            model.Subdomains[0] = new XSubdomain(0);
            for (int n = 0; n < mesh.NumNodesTotal; ++n)
            {
                model.XNodes.Add(new XNode(n, mesh.GetNodeCoordinates(mesh.GetNodeIdx(n))));
            }

            var matrixMaterial = new ThermalMaterial(ConductivityMatrix, 1);
            var inclusionMaterial = new ThermalMaterial(ConductivityCNT, 1);
            var materialField = new MatrixInclusionsThermalMaterialField(matrixMaterial, inclusionMaterial,
                ConductivityInterface, 0, defaultPhaseID);

            var subcellQuadrature = TetrahedronQuadrature.Order2Points4;
            var integrationBulk = new IntegrationWithConformingSubtetrahedra3D(subcellQuadrature);

            var elemFactory = new XThermalElement3DFactory(materialField, integrationBulk, boundaryIntegrationOrder, true);
            for (int e = 0; e < mesh.NumElementsTotal; ++e)
            {
                var nodes = new List<XNode>();
                int[] connectivity = mesh.GetElementConnectivity(e);
                foreach (int n in connectivity)
                {
                    nodes.Add(model.XNodes[n]);
                }
                XThermalElement3D element = elemFactory.CreateElement(e, mesh.CellType, nodes);
                model.Elements.Add(element);
                model.Subdomains[0].Elements.Add(element);
            }

            model.FindConformingSubcells = true;
            return model;
        }

        private PhaseGeometryModel CreatePhases(XModel<IXMultiphaseElement> model, IDualMesh mesh)
        {
            //IEnumerable<ISurface3D> inclusionGeometries = GenerateInclusionGeometries();
            IEnumerable<ISurface3D> inclusionGeometries = GeometryGenerator.GenerateInclusions();

            var geometricModel = new PhaseGeometryModel(model);
            geometricModel.Enricher = NodeEnricherMultiphaseNoJunctions.CreateThermalStep(geometricModel);
            geometricModel.MergeOverlappingPhases = mergeIntersectingCNTs;

            var defaultPhase = new DefaultPhase();
            geometricModel.Phases[defaultPhase.ID] = defaultPhase;

            foreach (ISurface3D surface in inclusionGeometries)
            {
                var phase = new LsmPhase(geometricModel.Phases.Count, geometricModel, 0);
                geometricModel.Phases[phase.ID] = phase;

                IClosedGeometry geometry;
                if (lsmType == 0) geometry = new SimpleLsm3D(phase.ID, model.XNodes, surface, !cartesianMesh);
                else if (lsmType == 1) geometry = new DualMeshLsm(phase.ID, surface, mesh, new LsmStorageGlobal(3));
                else if (lsmType == 2) geometry = new DualMeshLsm(phase.ID, surface, mesh, new LsmStorageLocal(3));
                else throw new NotImplementedException();
                var boundary = new ClosedPhaseBoundary(phase.ID, geometry, defaultPhase, phase);
                defaultPhase.ExternalBoundaries.Add(boundary);
                defaultPhase.Neighbors.Add(phase);
                phase.ExternalBoundaries.Add(boundary);
                phase.Neighbors.Add(defaultPhase);
                geometricModel.PhaseBoundaries[boundary.ID] = boundary;
            }

            return geometricModel;
        }

        private IEnumerable<ISurface3D> GenerateInclusionGeometries()
        {
            var inclusions = new List<ISurface3D>();

            #region debug Bug with this config and and 40x40x40 fine mesh
            //double theta = 0.5 * Math.PI; // 0 <= theta <= pi
            //double phi = 1.0 / 4.0 * Math.PI; // 0 <= phi < 2*pi
            //double length = 0.5;
            //double radius = 0.1; // breaks for 0.09
            #endregion

            #region debug Bug with this config and and 40x40x40 fine mesh
            //double theta = 0.5 * Math.PI; // 0 <= theta <= pi
            //double phi = 1.0 / 2.0 * Math.PI; // 0 <= phi < 2*pi
            //double length = 0.5;
            //double radius = 0.1; // breaks for 0.09
            #endregion

            #region debug Bug with this config and and 40x40x40 fine mesh
            //double theta = 0.5 * Math.PI; // 0 <= theta <= pi
            //double phi = 1.0 / 1.0 * Math.PI; // 0 <= phi < 2*pi
            //double length = 0.5;
            //double radius = 0.1; // breaks for 0.09
            #endregion

            #region debug Bug with this config and and 40x40x40 fine mesh
            //double theta = 1.0 /1.0 * Math.PI; // 0 <= theta <= pi
            //double phi = 1.0 / 6.0 * Math.PI; // 0 <= phi < 2*pi
            //double length = 0.5;
            //double radius = 0.1; // breaks for 0.09
            #endregion

            double[] centroid = { 0.0, 0.0, 0.0 };
            double theta = 1.0 / 2.0 * Math.PI; // 0 <= theta <= pi
            //double phi = 1.0 / 6.0 * Math.PI; // 0 <= phi < 2*pi
            double phi = 0.0 / 6.0 * Math.PI; // 0 <= phi < 2*pi

            double length = 0.9;
            double radius = 0.4; // breaks for 0.09

            double[] start = 
            {
                centroid[0] - 0.5 * length * Math.Cos(phi) * Math.Sin(theta),
                centroid[1] - 0.5 * length * Math.Sin(phi) * Math.Sin(theta),
                centroid[2] - 0.5 * length * Math.Cos(theta)
            };

            double[] end = 
            {
                centroid[0] + 0.5 * length * Math.Cos(phi) * Math.Sin(theta),
                centroid[1] + 0.5 * length * Math.Sin(phi) * Math.Sin(theta),
                centroid[2] + 0.5 * length * Math.Cos(theta)
            };

            inclusions.Add(new Cylinder3D(start, end, radius));
            //inclusions.Add(new Sphere(new double[] { 0, 0, 0 }, length));

            return inclusions;
        }

        private IVectorView RunSteadyStateAnalysis(ISolverBuilder solverBuilder)
        {
            // Run analysis
            Console.WriteLine("Starting analysis");
            if (solverBuilder == null) solverBuilder = new SkylineSolver.Builder();
            ISolver solver = solverBuilder.BuildSolver(model);
            var problem = new ProblemThermalSteadyState(model, solver);
            var linearAnalyzer = new LinearAnalyzer(model, solver, problem);
            var staticAnalyzer = new StaticAnalyzer(model, solver, problem, linearAnalyzer);

            staticAnalyzer.Initialize();
            staticAnalyzer.Solve();

            Console.WriteLine("Analysis finished");
            return solver.LinearSystems[0].Solution;
        }
    }
}
