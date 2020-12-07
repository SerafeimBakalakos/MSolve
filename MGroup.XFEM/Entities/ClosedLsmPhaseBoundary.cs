﻿using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using MGroup.XFEM.Enrichment;
using MGroup.XFEM.Enrichment.Functions;
using MGroup.XFEM.Geometry.LSM;
using MGroup.XFEM.Geometry.Primitives;

namespace MGroup.XFEM.Entities
{
    public class ClosedLsmPhaseBoundary : IPhaseBoundary
    {
        public ClosedLsmPhaseBoundary(int id, IClosedGeometry geometry, IPhase positivePhase, IPhase negativePhase)
        {
            this.ID = id;
            this.Geometry = geometry;
            this.PositivePhase = positivePhase;
            this.NegativePhase = negativePhase;
        }

        public int ID { get; }

        //MODIFICATION NEEDED: Following the design of cracks, the enrichments functions and items should be defined by this 
        //  class, not the INodeEnricher. However if this is problematic if there are multiple 
        //  boundaries between the same 2 phases, because then there would be duplicate enrichments! This case does not affect
        //  closed geometries that are defined as a single boundary.
        public EnrichmentItem StepEnrichment { get; set; } 

        public IPhase NegativePhase { get; set; }
        public IPhase PositivePhase { get; set; }

        public IClosedGeometry Geometry { get; }

        public ILsmGeometry LsmGeometry { get; }

        public IList<EnrichmentItem> DefineEnrichments(int numCurrentEnrichments) //MODIFICATION NEEDED. Probably this should be moved to INodeEnricher
        {
            //throw new NotImplementedException();
            return new List<EnrichmentItem>();
        }

        public void InitializeGeometry() //MODIFICATION NEEDED. Right now the user creates the LSM. We want him to only provide the initial shape (e.g. Circle)
        {
            //TODO: In problems where the phase boundaries move, a new class should be used (and this renamed to ConstantPhaseBoundary)
        }

        public void InteractWithMesh() //MODIFICATION NEEDED. 
        {
        }

        public void UpdateGeometry(Dictionary<int, Vector> subdomainFreeDisplacements)
        {
            //TODO: In problems where the phase boundaries move, a new class should be used (and this renamed to ConstantPhaseBoundary)
        }
    }
}