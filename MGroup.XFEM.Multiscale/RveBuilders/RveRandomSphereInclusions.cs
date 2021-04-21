using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using ISAAR.MSolve.Analyzers.Multiscale;
using ISAAR.MSolve.Discretization.Mesh;
using ISAAR.MSolve.LinearAlgebra;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.Materials;
using ISAAR.MSolve.Materials.Interfaces;
using ISAAR.MSolve.Problems;
using ISAAR.MSolve.Solvers;
using ISAAR.MSolve.Solvers.Direct;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Enrichment.Enrichers;
using MGroup.XFEM.Entities;
using MGroup.XFEM.Geometry.LSM;
using MGroup.XFEM.Geometry.Mesh;
using MGroup.XFEM.Geometry.Primitives;
using MGroup.XFEM.Integration;
using MGroup.XFEM.Integration.Quadratures;
using MGroup.XFEM.Materials;
using MGroup.XFEM.Phases;

//TODO: Should initialization results be cached?
namespace MGroup.XFEM.Multiscale.RveBuilders
{
    public class RveRandomSphereInclusions : IMesoscale
    {
        #region input 
        public int NumRealizations { get; set; }

        public int Seed { get; set; }

        public double VolumeFraction { get; set; }

        public IContinuumMaterial MatrixMaterial { get; set; }

        public IContinuumMaterial InclusionMaterial { get; set; }

        public ISolverBuilder SolverBuilder { get; set; } = new SuiteSparseSolver.Builder();

        public int LsmMeshRefinementLevel { get; set; } = 5;

        public double[] MinStrains { get; set; }

        public double[] MaxStrains { get; set; }

        public double[] TotalStrain { get; set; }

        public int NumLoadingIncrements { get; set; } = 10;
        #endregion

        #region output
        public IList<double[]> Strains { get; set; }

        public IList<double[]> Stresses { get; set; }

        public IList<IMatrixView> ConstitutiveMatrices { get; set; }
        #endregion

        public void RunAnalysis()
        {
            throw new NotImplementedException();
            //XModel<IXMultiphaseElement> model = CreateModelPhysical();
            //PhaseGeometryModel geometryModel = CreatePhases(model);

            //// Run analysis
            //model.Initialize();
            //Debug.WriteLine("Starting homogenization analysis");
            //var solver = (new SuiteSparseSolver.Builder()).BuildSolver(model);
            //var provider = new ProblemStructural(model, solver);
            //var rve = new StructuralCubicRve(model, minCoords, maxCoords);
            //var homogenization = new HomogenizationAnalyzer(model, solver, provider, rve);

            //homogenization.Initialize();
            //homogenization.Solve();
            //Debug.WriteLine("Analysis finished");

            //Strains = new List<double[]>();
            //Stresses = new List<double[]>();
            //ElasticityTensors = new List<IMatrix>();
            //var rng = new Random(Seed);
            //for (int r = 0; r < NumRealizations; ++r)
            //{
            //    var strains = new double[6];
            //    for (int i = 0; i < strains.Length; ++i)
            //    {
            //        strains[i] = MinStrains[i] + rng.NextDouble() * (MaxStrains[i] - MinStrains[i]);
            //    }

            //    double[] stresses = homogenization.MacroscopicModulus.Multiply(strains);

            //    Strains.Add(strains);
            //    Stresses.Add(stresses);
            //    ElasticityTensors.Add(homogenization.MacroscopicModulus);
            //}
        }

        private const int dimension = 3;
        private const int defaultPhaseID = 0;
        private readonly double[] minCoords = { -1, -1, -1 };
        private readonly double[] maxCoords = { +1, +1, +1 };
        private readonly int[] numElements = { 19, 19, 19 };
        private readonly int bulkIntegrationOrder = 2, boundaryIntegrationOrder = 2;
        private readonly bool cohesiveInterfaces = false;

        public RveRandomSphereInclusions()
        {
        }

        public XModel<IXMultiphaseElement> CreateModelPhysical()
        {
            // Materials
            double cohesiveness = 0;
            var interfaceMaterial = new CohesiveInterfaceMaterial(Matrix.CreateFromArray(new double[,]
            {
                { cohesiveness, 0, 0 },
                { 0, cohesiveness, 0 },
                { 0, 0, cohesiveness }
            }));
            var materialField = new MatrixInclusionsStructuralMaterialField(
                MatrixMaterial, InclusionMaterial, interfaceMaterial, 0);

            // Setup model
            var model = new XModel<IXMultiphaseElement>(3);
            model.Subdomains[0] = new XSubdomain(0);

            // Mesh generation
            var mesh = new UniformMesh3D(minCoords, maxCoords, numElements);
            
            // Nodes
            for (int n = 0; n < mesh.NumNodesTotal; ++n)
            {
                model.XNodes.Add(new XNode(n, mesh.GetNodeCoordinates(mesh.GetNodeIdx(n))));
            }

            // Integration
            model.FindConformingSubcells = true;
            var subcellQuadrature = TetrahedronQuadrature.Order2Points4;
            var integrationBulk = new IntegrationWithConformingSubtetrahedra3D(subcellQuadrature);

            // Elements
            var elemFactory = new XMultiphaseStructuralElementFactory3D(
                materialField, integrationBulk, boundaryIntegrationOrder, cohesiveInterfaces);
            for (int e = 0; e < mesh.NumElementsTotal; ++e)
            {
                var nodes = new List<XNode>();
                int[] connectivity = mesh.GetElementConnectivity(e);
                foreach (int n in connectivity) nodes.Add(model.XNodes[n]);
                IXStructuralMultiphaseElement element = elemFactory.CreateElement(e, CellType.Hexa8, nodes);
                model.Elements.Add(element);
                model.Subdomains[0].Elements.Add(element);
            }

            return model;
        }

        public PhaseGeometryModel CreatePhases(XModel<IXMultiphaseElement> model)
        {
            var numElementsFine = new int[dimension];
            for (int d = 0; d < dimension; ++d)
            {
                numElementsFine[d] = LsmMeshRefinementLevel * numElements[d];
            }
            var dualMesh = new DualMesh3D(minCoords, maxCoords, numElements, numElementsFine);

            IList<ISurface3D> inclusionGeometries = CreateInclusionGeometries();

            var geometryModel = new PhaseGeometryModel(model);
            model.GeometryModel = geometryModel;
            var defaultPhase = new DefaultPhase();
            geometryModel.Phases[defaultPhase.ID] = defaultPhase;
            for (int p = 0; p < inclusionGeometries.Count; ++p)
            {
                //var lsm = new SimpleLsm3D(p + 1, model.XNodes, inclusionGeometries[p]);
                var lsm = new DualMeshLsm3D(p + 1, dualMesh, inclusionGeometries[p]);
                var phase = new LsmPhase(p + 1, geometryModel, -1);
                geometryModel.Phases[phase.ID] = phase;

                var boundary = new ClosedPhaseBoundary(phase.ID, lsm, defaultPhase, phase);
                defaultPhase.ExternalBoundaries.Add(boundary);
                defaultPhase.Neighbors.Add(phase);
                phase.ExternalBoundaries.Add(boundary);
                phase.Neighbors.Add(defaultPhase);
                geometryModel.PhaseBoundaries[boundary.ID] = boundary;
            }
            if (cohesiveInterfaces)
            {
                geometryModel.Enricher = NodeEnricherMultiphaseNoJunctions.CreateStructuralStep(geometryModel, dimension);
            }
            else
            {
                geometryModel.Enricher = NodeEnricherMultiphaseNoJunctions.CreateStructuralRidge(geometryModel, dimension);
            }
            return geometryModel;
        }

        private IList<ISurface3D> CreateInclusionGeometries()
        {
            var inclusionGenerator = new InclusionGenerator3D();
            inclusionGenerator.CoordsMin = this.minCoords;
            inclusionGenerator.CoordsMax = this.maxCoords;
            inclusionGenerator.InclusionsMinDistanceOverDomainLength = 0.01;
            inclusionGenerator.RadiusMin = 0.05 * (maxCoords[0] - minCoords[0]);
            inclusionGenerator.RadiusMax = 0.20 * (maxCoords[0] - minCoords[0]);
            inclusionGenerator.Seed = this.Seed;
            inclusionGenerator.TargetVolumeFraction = this.VolumeFraction;
            inclusionGenerator.TargetVolumeFractionToleranceRatio = 0.05;
            return inclusionGenerator.CreateInclusions();
        }
    }
}
