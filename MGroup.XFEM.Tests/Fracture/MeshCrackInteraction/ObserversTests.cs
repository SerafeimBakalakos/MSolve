using System;
using System.Collections.Generic;
using System.Text;
using MGroup.XFEM.Cracks;
using MGroup.XFEM.Cracks.Geometry;
using MGroup.XFEM.Cracks.Geometry.LSM;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Enrichment.Enrichers;
using MGroup.XFEM.Enrichment.Observers;
using MGroup.XFEM.Enrichment.SingularityResolution;
using MGroup.XFEM.Entities;
using MGroup.XFEM.Geometry.Mesh;
using MGroup.XFEM.Geometry.Primitives;
using MGroup.XFEM.Integration;
using MGroup.XFEM.Integration.Quadratures;
using MGroup.XFEM.Materials;
using MGroup.XFEM.Plotting.Mesh;
using MGroup.XFEM.Plotting.Writers;
using Xunit;

namespace MGroup.XFEM.Tests.Fracture.Observers
{
    public static class ObserversTests
    {
        private const int subdomainID = 0;

        //TODO: Error in tip enrichments when orientation is vertical
        [Fact]
        private static void RunPropagationAndPlot() 
        {
            XModel<IXCrackElement> model = CreateModel();
            var crack = (ExteriorLsmCrack)model.Discontinuities[0];

            // Plot the FE mesh
            string outputDir = @"C:\Users\Serafeim\Desktop\XFEM2020\Cracks\InteractionTests\";
            var outputMesh = new ContinuousOutputMesh(model.XNodes, model.Elements);
            string meshPath = outputDir + "mesh.vtk";
            using (var writer = new VtkFileWriter(meshPath))
            {
                writer.WriteMesh(outputMesh);
            }

            // Set up observers. //TODO: Each observer should define and link all its necessary observers in an optional constructor.
            crack.Observers.Add(new CrackLevelSetPlotter(crack, outputMesh, outputDir));
            crack.Observers.Add(new CrackInteractingElementsPlotter(crack, outputDir));

            var previousEnrichments = new PreviousEnrichmentsObserver();
            model.RegisterEnrichmentObserver(previousEnrichments);
            var newTipNodes = new NewCrackTipNodesObserver(crack);
            model.RegisterEnrichmentObserver(newTipNodes);
            var previousTipNodes = new PreviousCrackTipNodesObserver(crack, previousEnrichments);
            model.RegisterEnrichmentObserver(previousTipNodes);
            var allBodyNodes = new CrackBodyNodesObserver(crack);
            model.RegisterEnrichmentObserver(allBodyNodes);
            var newBodyNodes = new NewCrackBodyNodesObserver(crack, previousEnrichments, allBodyNodes);
            model.RegisterEnrichmentObserver(newBodyNodes);
            var rejectedBodyNodes = new RejectedCrackBodyNodesObserver(crack, newTipNodes, allBodyNodes);
            model.RegisterEnrichmentObserver(rejectedBodyNodes);
            var bodyNodesWithModifiedLevelSet = new CrackBodyNodesWithModifiedLevelSetObserver(
                crack, previousEnrichments, allBodyNodes);
            model.RegisterEnrichmentObserver(bodyNodesWithModifiedLevelSet);
            var modifiedNodes = new NodesWithModifiedEnrichmentsObserver(
                newTipNodes, previousTipNodes, newBodyNodes, bodyNodesWithModifiedLevelSet);
            model.RegisterEnrichmentObserver(modifiedNodes);
            var modifiedElements = new ElementsWithModifiedNodesObserver(modifiedNodes);
            model.RegisterEnrichmentObserver(modifiedElements);
            var nearModifiedNodes = new NodesNearModifiedNodesObserver(modifiedNodes, modifiedElements);
            model.RegisterEnrichmentObserver(nearModifiedNodes);

            var enrichmentPlotter = new CrackEnrichmentPlotter(crack, outputDir, newTipNodes, previousTipNodes, allBodyNodes,
                newBodyNodes, rejectedBodyNodes, nearModifiedNodes);
            model.RegisterEnrichmentObserver(enrichmentPlotter);

            // Propagate the crack. During this the observers will plot the data they pull from model. 
            model.Initialize();
            for (int rep = 0; rep < 3; ++rep)
            {
                model.Update(null);
            }
        }

        private static XModel<IXCrackElement> CreateModel()
        {
            var model = new XModel<IXCrackElement>(2);
            model.Subdomains[subdomainID] = new XSubdomain(subdomainID);

            // Materials, integration
            double E = 2E6, v = 0.3, thickness = 1.0;
            var material = new HomogeneousFractureMaterialField2D(E, v, thickness, true);
            var enrichedIntegration = new IntegrationWithNonconformingQuads2D(8, GaussLegendre2D.GetQuadratureWithOrder(2, 2));
            var bulkIntegration = new CrackElementIntegrationStrategy(
                enrichedIntegration, enrichedIntegration, enrichedIntegration);
            var factory = new XCrackElementFactory2D(material, thickness, bulkIntegration);

            // Mesh
            double[] minCoords = { 0.0, 0.0 };
            double[] maxCoords = { 20.0, 20.0 };
            int[] numElements = { 20, 20 };
            var mesh = new UniformMesh2D(minCoords, maxCoords, numElements);
            Utilities.Models.AddNodesElements(model, mesh, factory);

            // Fixed crack path
            double[] angles = { Math.PI/6, Math.PI / 6, -Math.PI / 6 };
            double[] lengths = { 1.0, 1.5, 4.0 };
            IPropagator propagator = new MockPropagator(angles, lengths);

            // Crack, enrichments
            double yCrack = 9.99; // avoid conforming case
            var initialFlaw = new PolyLine2D(new double[] { 0, yCrack }, new double[] { 4.93, yCrack } );
            var crack = new ExteriorLsmCrack(0, initialFlaw, model, propagator);
            var enricher = new NodeEnricherIndependentCracks(new ICrack[] { crack }, new RelativeAreaSingularityResolver(0.006));
            model.Discontinuities.Add(crack);
            model.NodeEnrichers.Add(enricher);
            model.FindConformingSubcells = true;

            return model;
        }
    }
}
