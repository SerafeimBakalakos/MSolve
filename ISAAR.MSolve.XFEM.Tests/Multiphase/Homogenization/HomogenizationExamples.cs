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
using ISAAR.MSolve.XFEM_OLD.Multiphase.Elements;
using ISAAR.MSolve.XFEM_OLD.Multiphase.Enrichment;
using ISAAR.MSolve.XFEM_OLD.Multiphase.Enrichment.SingularityResolution;
using ISAAR.MSolve.XFEM_OLD.Multiphase.Entities;
using ISAAR.MSolve.XFEM_OLD.Multiphase.Integration;
using ISAAR.MSolve.XFEM_OLD.Multiphase.Materials;

namespace ISAAR.MSolve.XFEM_OLD.Tests.Multiphase.Homogenization
{
    public static class HomogenizationExamples
    {
        private const int numElementsX = 50, numElementsY = 50;
        private const int subdomainID = 0;
        private const double minX = -1.0, minY = -1.0, maxX = 1.0, maxY = 1.0;
        private const double elementSize = (maxX - minX) / numElementsX;
        private const double thickness = 1.0;
        private const bool integrationWithSubtriangles = true;
        private const double matrixConductivity = 1E0, inclusionConductivity = 1E4;
        private const double matrixInclusionInterfaceConductivity = 1E0, inclusionInclusionInterfaceConductivity = 1E10;
        private const double specialHeatCoeff = 1.0;
        private const double singularityRelativeAreaTolerance = 1E-4;

        public static void RunHomogenizationAnalysis()
        {
            var generator = new PhaseGenerator(minX, maxX, numElementsX);
            GeometricModel geometricModel = generator.CreatePercolatedTetrisPhases();
            XModel physicalModel = CreatePhysicalModel(geometricModel);
            PrepareForAnalysis(physicalModel, geometricModel);

            // Analysis
            double[] temperatureGradient = { 200, 0 };
            var solver = (new SkylineSolver.Builder()).BuildSolver(physicalModel);
            var provider = new ProblemThermalSteadyState(physicalModel, solver);
            var rve = new ThermalSquareRve(physicalModel, new double[] { minX, minY }, new double[] { maxX, maxY }, thickness);
            var homogenization = new HomogenizationAnalyzer(physicalModel, solver, provider, rve);
            //homogenization.MacroscopicStrains = temperatureGradient; // No need to calculate macroscopic flux

            homogenization.Initialize();
            homogenization.Solve();

            IMatrix conductivity = homogenization.MacroscopicModulus;
            Console.WriteLine($"C = [ {conductivity[0, 0]} {conductivity[0, 1]}; {conductivity[1, 0]} {conductivity[1, 1]} ]");
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
            var materialField = new MatrixInclusionsMaterialField(matrixMaterial, inclusionMaterial,
                matrixInclusionInterfaceConductivity, inclusionInclusionInterfaceConductivity, DefaultPhase.DefaultPhaseID);

            // Elements
            var factory = new XThermalElement2DFactory(materialField, thickness, volumeIntegration, boundaryIntegration);
            for (int e = 0; e < cells.Count; ++e)
            {
                XThermalElement2D element = factory.CreateElement(e, CellType.Quad4, cells[e].Vertices);
                physicalModel.Elements.Add(element);
                physicalModel.Subdomains[subdomainID].Elements.Add(element);
            }

            return physicalModel;
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
    }
}
