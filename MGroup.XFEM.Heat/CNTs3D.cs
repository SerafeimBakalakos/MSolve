//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.IO;
//using System.Text;
//using MGroup.XFEM.Elements;
//using MGroup.XFEM.Enrichment.Enrichers;
//using MGroup.XFEM.Enrichment.SingularityResolution;
//using MGroup.XFEM.Entities;
//using MGroup.XFEM.Geometry.LSM;
//using MGroup.XFEM.Geometry.Mesh;
//using MGroup.XFEM.Geometry.Primitives;
//using MGroup.XFEM.Integration;
//using MGroup.XFEM.Integration.Quadratures;
//using MGroup.XFEM.Materials;
//using MGroup.XFEM.Output;
//using MGroup.XFEM.Output.Fields;
//using MGroup.XFEM.Output.Mesh;
//using MGroup.XFEM.Output.Vtk;
//using MGroup.XFEM.Output.Writers;
//using MGroup.XFEM.Phases;
//using Xunit;

//namespace MGroup.XFEM.HeatDualMeshLsm
//{
//    public static class CNTs3D
//    {
//        private static readonly string outputDirectory = @"C:\Users\Serafeim\Desktop\HEAT\Phase3\CntsMatrix";
//        private static readonly double[] minCoords = { -1.0, -1.0, -1.0 };
//        private static readonly double[] maxCoords = { +1.0, +1.0, +1.0 };
//        private static readonly int[] numElementsCoarse = { 15, 15, 15 };
//        private static readonly int[] numElementsFine = { 60, 60, 60 };
//        private const int defaultPhaseID = 0;
//        private const int bulkIntegrationOrder = 2, boundaryIntegrationOrder = 2;

        

//        [Fact]
//        public static void Run()
//        {
//            // Create model and LSM
//            var mesh = new DualMesh3D(minCoords, maxCoords, numElementsCoarse, numElementsFine);
//            XModel<IXMultiphaseElement> model = CreateModel(mesh.CoarseMesh);
//            model.FindConformingSubcells = true;
//            PhaseGeometryModel geometryModel = CreatePhases(model, mesh);

//            // Plot phases of nodes
//            geometryModel.InteractionObservers.Add(new NodalPhasesPlotter(outputDirectory, model));

//            // Plot element - phase boundaries interactions
//            geometryModel.InteractionObservers.Add(new LsmElementIntersectionsPlotter(outputDirectory, model));

//            // Plot element subcells
//            model.ModelObservers.Add(new ConformingMeshPlotter(outputDirectory, model));

//            // Plot phases of each element subcell
//            model.ModelObservers.Add(new ElementPhasePlotter(outputDirectory, model, geometryModel, defaultPhaseID));

//            // Write the size of each phase
//            model.ModelObservers.Add(new PhasesSizeWriter(outputDirectory, model, geometryModel));

//            // Plot bulk and boundary integration points of each element
//            model.ModelObservers.Add(new IntegrationPointsPlotter(outputDirectory, model));

//            // Plot enrichments
//            double elementSize = (maxCoords[0] - minCoords[0]) / numElementsCoarse[0];
//            model.RegisterEnrichmentObserver(new PhaseEnrichmentPlotter(outputDirectory, model, elementSize, 2));

//            // Initialize model state so that everything described above can be tracked
//            model.Initialize();

//        }

//        private static XModel<IXMultiphaseElement> CreateModel(IStructuredMesh mesh)
//        {
//            var model = new XModel<IXMultiphaseElement>(3);
//            model.Subdomains[0] = new XSubdomain(0);
//            for (int n = 0; n < mesh.NumNodesTotal; ++n)
//            {
//                model.XNodes.Add(new XNode(n, mesh.GetNodeCoordinates(mesh.GetNodeIdx(n))));
//            }

//            var matrixMaterial = new ThermalMaterial(1, 1);
//            var inclusionMaterial = new ThermalMaterial(1, 1);
//            var materialField = new MatrixInclusionsThermalMaterialField(matrixMaterial, inclusionMaterial,
//                1, 1, defaultPhaseID);

//            var subcellQuadrature = TetrahedronQuadrature.Order2Points4;
//            var integrationBulk = new IntegrationWithConformingSubtetrahedra3D(subcellQuadrature);

//            var elemFactory = new XThermalElement3DFactory(materialField, integrationBulk, boundaryIntegrationOrder, true);
//            for (int e = 0; e < mesh.NumElementsTotal; ++e)
//            {
//                var nodes = new List<XNode>();
//                int[] connectivity = mesh.GetElementConnectivity(mesh.GetElementIdx(e));
//                foreach (int n in connectivity)
//                {
//                    nodes.Add(model.XNodes[n]);
//                }
//                XThermalElement3D element = elemFactory.CreateElement(e, ISAAR.MSolve.Discretization.Mesh.CellType.Hexa8, nodes);
//                model.Elements.Add(element);
//                model.Subdomains[0].Elements.Add(element);
//            }

//            return model;
//        }

//        private static PhaseGeometryModel CreatePhases(XModel<IXMultiphaseElement> model, DualMesh3D mesh)
//        {
//            var geometricModel = new PhaseGeometryModel(model);
//            model.GeometryModel = geometricModel;
//            geometricModel.Enricher = NodeEnricherMultiphaseNoJunctions.CreateThermalStep(geometricModel);
            
//            var defaultPhase = new DefaultPhase();
//            geometricModel.Phases[defaultPhase.ID] = defaultPhase;
//            var phase = new LsmPhase(1, geometricModel, -1);
//            geometricModel.Phases[phase.ID] = phase;

//            var dualMeshLsm = new DualMeshLsm3D(0, mesh, initialSurface);
//            var boundary = new ClosedPhaseBoundary(phase.ID, dualMeshLsm, defaultPhase, phase);
//            defaultPhase.ExternalBoundaries.Add(boundary);
//            defaultPhase.Neighbors.Add(phase);
//            phase.ExternalBoundaries.Add(boundary);
//            phase.Neighbors.Add(defaultPhase);
//            geometricModel.PhaseBoundaries[boundary.ID] = boundary;

//            return geometricModel;
//        }

//        private static List<double[]> GeneratePointsPerElement(int numPointsPerAxis, double tolerance = 0.0)
//        {
//            var points = new List<double[]>();
//            double minCoord = -1 + tolerance;
//            double maxCoord = 1 - tolerance;
//            double space = (maxCoord - minCoord) / numPointsPerAxis;
//            for (int i = 0; i < numPointsPerAxis; ++i)
//            {
//                double xi = minCoord + 0.5 * space + i * space;
//                for (int j = 0; j < numPointsPerAxis; ++j)
//                {
//                    double eta = minCoord + 0.5 * space + j * space;
//                    for (int k = 0; k < numPointsPerAxis; ++k)
//                    {
//                        double zeta = minCoord + 0.5 * space + k * space;
//                        points.Add(new double[] { xi, eta, zeta });

//                    }
//                }
//            }
//            return points;
//        }
//    }
//}
