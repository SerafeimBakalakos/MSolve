using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using MGroup.XFEM.Enrichment;
using MGroup.XFEM.Enrichment.Functions;
using MGroup.XFEM.Geometry.LSM;
using MGroup.XFEM.Geometry.Primitives;

namespace MGroup.XFEM.Entities
{
    public class PhaseBoundary : IXDiscontinuity
    {
        public PhaseBoundary(int id, IClosedGeometry geometry, IPhase positivePhase, IPhase negativePhase)
        {
            this.ID = id;
            this.Geometry = geometry;
            this.PositivePhase = positivePhase;
            this.NegativePhase = negativePhase;
        }

        public int ID { get; }


        public PhaseStepEnrichment StepEnrichment { get; set; }

        public IPhase NegativePhase { get; set; }
        public IPhase PositivePhase { get; set; }

        public IClosedGeometry Geometry { get; set; }

        public IList<EnrichmentItem> DefineEnrichments(int numCurrentEnrichments) //MODIFICATION NEEDED. Probably this should be moved to INodeEnricher
        {
            throw new NotImplementedException();
        }

        public void InitializeGeometry()
        {
            //TODO: In problems where the phase boundaries move, a new class should be used (and this renamed to ConstantPhaseBoundary)
        }

        public void InteractWithMesh() //MODIFICATION NEEDED. 
        {
            throw new NotImplementedException();
        }

        public void UpdateGeometry(Dictionary<int, Vector> subdomainFreeDisplacements)
        {
            //TODO: In problems where the phase boundaries move, a new class should be used (and this renamed to ConstantPhaseBoundary)
        }
    }
}
