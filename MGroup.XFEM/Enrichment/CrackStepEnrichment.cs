using System;
using System.Collections.Generic;
using System.Text;
using MGroup.XFEM.Cracks.Geometry;
using MGroup.XFEM.Entities;
using MGroup.XFEM.Geometry;
using MGroup.XFEM.Geometry.Primitives;

namespace MGroup.XFEM.Enrichment
{
    public class CrackStepEnrichment : IEnrichment
    {
        private readonly IXGeometryDescription crackGeometry;

        public CrackStepEnrichment(int id, IXGeometryDescription crackGeometry)
        {
            this.ID = id;
            this.crackGeometry = crackGeometry;
        }

        public int ID { get; }

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
