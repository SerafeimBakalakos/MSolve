using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using MGroup.XFEM.Cracks.Geometry;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Enrichment.SingularityResolution;
using MGroup.XFEM.Entities;
using MGroup.XFEM.Geometry.Primitives;

namespace MGroup.XFEM.Enrichment.Enrichers
{
    public class NodeEnricherDisjointCracks : INodeEnricher
    {
        private readonly IEnumerable<ICrack> cracks; //TODO: Read these from a component like PhaseGeometryModel
        private readonly double fixedTipEnrichmentRegionRadius;
        private readonly int dimension;
        private readonly XModel<IXCrackElement> model;
        private readonly ISingularityResolver singularityResolver;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fixedTipEnrichmentRegionRadius">
        /// If a fixed enrichment area is applied, all nodes inside a circle around the tip are enriched with tip 
        /// functions. They can still be enriched with Heaviside functions, if they do not belong to the tip 
        /// element(s).
        /// </param>
        public NodeEnricherDisjointCracks(int dimension, XModel<IXCrackElement> model, IEnumerable<ICrack> cracks,
            ISingularityResolver singularityResolver, double fixedTipEnrichmentRegionRadius = 0.0)
        {
            this.dimension = dimension;
            this.model = model;
            this.cracks = cracks;
            this.singularityResolver = singularityResolver;
            this.fixedTipEnrichmentRegionRadius = fixedTipEnrichmentRegionRadius;
            IdentifyEnrichedDofs();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fixedTipEnrichmentRegionRadius">
        /// If a fixed enrichment area is applied, all nodes inside a circle around the tip are enriched with tip 
        /// functions. They can still be enriched with Heaviside functions, if they do not belong to the tip 
        /// element(s).
        /// </param>
        public NodeEnricherDisjointCracks(int dimension, XModel<IXCrackElement> model, IEnumerable<ICrack> cracks,
            double fixedTipEnrichmentRegionRadius = 0.0) :
            this(dimension, model, cracks, new RelativeAreaSingularityResolver(1E-4), fixedTipEnrichmentRegionRadius)
        {
        }

        public void ApplyEnrichments()
        {
            foreach (ICrack crack in cracks)
            {
                ClearNodalEnrichments(crack.CrackTipEnrichments);

                // Enrich nodes of the new crack tip elements
                var tipElementNodes = new HashSet<XNode>();
                foreach (IXCrackElement element in crack.CrackTipEnrichments)
                {
                    tipElementNodes.UnionWith(tipElementNodes);
                }
                EnrichNodesWith(tipElementNodes, crack.CrackTipEnrichments);

                // Extra tip nodes due to "fixed tip enrichment area"
                if (fixedTipEnrichmentRegionRadius > 0.0) 
                {
                    var circle = new Circle2D(crack.TipCoordinates, fixedTipEnrichmentRegionRadius); //TODO: This needs adapting for 3D
                    HashSet<XNode> extraTipNodes = MeshUtilities.FindNodesInsideCircle(circle, crack.TipElements.First());
                    extraTipNodes.ExceptWith(tipElementNodes);
                    EnrichNodesWith(extraTipNodes, crack.CrackTipEnrichments);
                }

                // Heaviside nodes
                var heavisideNodes = new HashSet<XNode>();
                foreach (IXCrackElement element in crack.IntersectedElements)
                {
                    heavisideNodes.UnionWith(element.Nodes);
                }
                foreach (IXCrackElement element in crack.ConformingElements)
                {
                    throw new NotImplementedException("Must find which of the element's nodes lie on the conforming edge");
                }

                // Do not enrich the nodes of the crack tip(s)
                heavisideNodes.ExceptWith(tipElementNodes);

                // Also do not enrich nodes that may cause singularities
                HashSet<XNode> rejectedNodes = 
                    singularityResolver.FindStepEnrichedNodesToRemove(heavisideNodes, crack.CrackGeometry);
                heavisideNodes.ExceptWith(rejectedNodes);

                // Only enrich the new Heaviside nodes, namely the ones not previously enriched. This optimization is not
                // necessary. It will cause problems if the crack tip turns sharply towards the crack body, which shouldn't 
                // happen normally.
                foreach (XNode node in heavisideNodes)
                {
                    if (!node.Enrichments.ContainsKey(crack.CrackBodyEnrichment))
                    {
                        node.Enrichments[crack.CrackBodyEnrichment] = crack.CrackBodyEnrichment.EvaluateAt(node);
                    }
                }
            }
        }

        private void ClearNodalEnrichments(IEnumerable<IEnrichment> enrichments)
        {
            foreach (IEnrichment enrichment in enrichments)
            {
                foreach (XNode node in model.EnrichedNodes[enrichment])
                {
                    node.Enrichments.Remove(enrichment);
                }
                model.EnrichedNodes[enrichment] = null;
            }
        }

        private void EnrichNodesWith(IEnumerable<XNode> nodes, IEnumerable<IEnrichment> enrichments)
        {
            foreach (XNode node in nodes)
            {
                foreach (IEnrichment enrichment in enrichments)
                {
                    node.Enrichments[enrichment] = enrichment.EvaluateAt(node);
                }
            }
        }

        //TODO: Generalize this method for all INodeEnricher implementations
        private void IdentifyEnrichedDofs()
        {
            model.EnrichedDofs.Clear();
            foreach (ICrack crack in cracks)
            {
                var enrichments = new List<IEnrichment>();
                enrichments.Add(crack.CrackBodyEnrichment);
                enrichments.AddRange(crack.CrackTipEnrichments);
                if (dimension == 2)
                {
                    foreach (IEnrichment enrichment in enrichments)
                    {
                        var dofs = new IDofType[]
                        {
                            new EnrichedDof(enrichment, StructuralDof.TranslationX),
                            new EnrichedDof(enrichment, StructuralDof.TranslationY)
                        };
                        model.EnrichedDofs[enrichment] = dofs;
                    }
                }
                else if (dimension == 3)
                {
                    foreach (IEnrichment enrichment in enrichments)
                    {
                        var dofs = new IDofType[]
                        {
                            new EnrichedDof(enrichment, StructuralDof.TranslationX),
                            new EnrichedDof(enrichment, StructuralDof.TranslationY),
                            new EnrichedDof(enrichment, StructuralDof.TranslationZ)
                        };
                        model.EnrichedDofs[enrichment] = dofs;
                    }
                }
                else throw new NotImplementedException();
            }
        }
    }
}
