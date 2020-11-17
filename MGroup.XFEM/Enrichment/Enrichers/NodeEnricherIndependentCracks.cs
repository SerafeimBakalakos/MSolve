﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MGroup.XFEM.Cracks.Geometry;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Enrichment.SingularityResolution;
using MGroup.XFEM.Entities;
using MGroup.XFEM.Geometry.Primitives;

namespace MGroup.XFEM.Enrichment.Enrichers
{
    public class NodeEnricherIndependentCracks : INodeEnricher
    {
        private readonly IEnumerable<ICrack> cracks; //TODO: Read these from a component like PhaseGeometryModel
        private readonly double fixedTipEnrichmentRegionRadius;
        private readonly ISingularityResolver singularityResolver;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fixedTipEnrichmentRegionRadius">
        /// If a fixed enrichment area is applied, all nodes inside a circle around the tip are enriched with tip 
        /// functions. They can still be enriched with Heaviside functions, if they do not belong to the tip 
        /// element(s).
        /// </param>
        public NodeEnricherIndependentCracks(IEnumerable<ICrack> cracks,
            ISingularityResolver singularityResolver, double fixedTipEnrichmentRegionRadius = 0.0)
        {
            this.cracks = cracks;
            this.singularityResolver = singularityResolver;
            this.fixedTipEnrichmentRegionRadius = fixedTipEnrichmentRegionRadius;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fixedTipEnrichmentRegionRadius">
        /// If a fixed enrichment area is applied, all nodes inside a circle around the tip are enriched with tip 
        /// functions. They can still be enriched with Heaviside functions, if they do not belong to the tip 
        /// element(s).
        /// </param>
        public NodeEnricherIndependentCracks(IEnumerable<ICrack> cracks, double fixedTipEnrichmentRegionRadius = 0.0) :
            this(cracks, new RelativeAreaSingularityResolver(1E-4), fixedTipEnrichmentRegionRadius)
        {
        }

        public void ApplyEnrichments()
        {
            foreach (ICrack crack in cracks)
            {
                ClearNodalEnrichments(crack.CrackTipEnrichments);

                // Enrich nodes of the new crack tip elements
                var tipElementNodes = new HashSet<XNode>();
                foreach (IXCrackElement element in crack.TipElements)
                {
                    tipElementNodes.UnionWith(element.Nodes);
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
                var newHeavisideNodes = new HashSet<XNode>();
                foreach (XNode node in heavisideNodes)
                {
                    if (!node.Enrichments.Contains(crack.CrackBodyEnrichment))
                    {
                        newHeavisideNodes.Add(node);
                    }
                }
                EnrichNodesWith(newHeavisideNodes, crack.CrackBodyEnrichment);
            }
        }

        private void ClearNodalEnrichments(EnrichmentItem enrichment)
        {
            foreach (XNode node in enrichment.EnrichedNodes)
            {
                node.Enrichments.Remove(enrichment);
                foreach (IEnrichmentFunction enrichmentFunc in enrichment.EnrichmentFunctions)
                {
                    node.EnrichmentFuncs.Remove(enrichmentFunc);
                }
            }
            enrichment.EnrichedNodes.Clear();
        }

        private void EnrichNodesWith(IEnumerable<XNode> nodes, EnrichmentItem enrichment)
        {
            foreach (XNode node in nodes)
            {
                node.Enrichments.Add(enrichment);
                foreach (IEnrichmentFunction enrichmentFunc in enrichment.EnrichmentFunctions)
                {
                    node.EnrichmentFuncs[enrichmentFunc] = enrichmentFunc.EvaluateAt(node);
                }
                enrichment.EnrichedNodes.Add(node);
            }
        }

        ////TODO: Generalize this method for all INodeEnricher implementations
        ////TODO: For cases where a crack branches or 2 cracks merge, the enrichments will change. This method will need to be
        ////      called repeatedly (probably by the model). How do I determine which enrichments change and which remain the same?
        //private void IdentifyEnrichedDofs()
        //{
        //    //TODO: Create EnrichmentItems (and their dofs) and register them in the model 
        //    model.EnrichedDofs.Clear();
        //    foreach (ICrack crack in cracks)
        //    {
        //        var enrichments = new List<IEnrichmentFunction>();
        //        enrichments.Add(crack.CrackBodyEnrichment);
        //        enrichments.AddRange(crack.CrackTipEnrichments);
        //        if (dimension == 2)
        //        {
        //            foreach (IEnrichmentFunction enrichment in enrichments)
        //            {
        //                var dofs = new IDofType[]
        //                {
        //                    new EnrichedDof(enrichment, StructuralDof.TranslationX),
        //                    new EnrichedDof(enrichment, StructuralDof.TranslationY)
        //                };
        //                model.EnrichedDofs[enrichment] = dofs;
        //            }
        //        }
        //        else if (dimension == 3)
        //        {
        //            foreach (IEnrichmentFunction enrichment in enrichments)
        //            {
        //                var dofs = new IDofType[]
        //                {
        //                    new EnrichedDof(enrichment, StructuralDof.TranslationX),
        //                    new EnrichedDof(enrichment, StructuralDof.TranslationY),
        //                    new EnrichedDof(enrichment, StructuralDof.TranslationZ)
        //                };
        //                model.EnrichedDofs[enrichment] = dofs;
        //            }
        //        }
        //        else throw new NotImplementedException();
        //    }
        //}
    }
}
