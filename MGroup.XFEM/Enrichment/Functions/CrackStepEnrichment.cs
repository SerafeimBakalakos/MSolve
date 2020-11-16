using System;
using System.Collections.Generic;
using System.Text;
using MGroup.XFEM.Entities;
using MGroup.XFEM.Geometry;
using MGroup.XFEM.Geometry.Primitives;

namespace MGroup.XFEM.Enrichment.Functions
{
    public class CrackStepEnrichment : IEnrichmentFunction
    {
        private readonly IXGeometryDescription crackGeometry;

        public CrackStepEnrichment(IXGeometryDescription crackGeometry)
        {
            this.crackGeometry = crackGeometry;
        }

        public IReadOnlyList<IPhase> Phases => throw new NotImplementedException();

        public EvaluatedFunction EvaluateAllAt(XPoint point)
        {
            double distance = crackGeometry.SignedDistanceOf(point);
            if (distance >= 0) return new EvaluatedFunction(+1, new double[2]);
            else return new EvaluatedFunction(-1, new double[2]);
        }

        public double EvaluateAt(XNode node)
        {
            double distance = crackGeometry.SignedDistanceOf(node);
            if (distance >= 0) return +1;
            else return -1;
        }

        public double EvaluateAt(XPoint point)
        {
            double distance = crackGeometry.SignedDistanceOf(point);
            if (distance >= 0) return +1;
            else return -1;
        }

        public double GetJumpCoefficientBetween(PhaseBoundary phaseBoundary)
        {
            throw new NotImplementedException();
        }

        public bool IsAppliedDueTo(PhaseBoundary phaseBoundary)
        {
            throw new NotImplementedException();
        }
    }
}
