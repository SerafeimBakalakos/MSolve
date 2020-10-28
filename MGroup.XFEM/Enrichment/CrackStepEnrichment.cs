using System;
using System.Collections.Generic;
using System.Text;
using MGroup.XFEM.Entities;
using MGroup.XFEM.Geometry.Cracks;
using MGroup.XFEM.Geometry.Primitives;

namespace MGroup.XFEM.Enrichment
{
    public class CrackStepEnrichment : IEnrichment
    {
        private readonly ICrack2D crack;

        public CrackStepEnrichment(int id, ICrack2D crack)
        {
            this.ID = id;
            this.crack = crack;
        }

        public int ID { get; }

        public IReadOnlyList<IPhase> Phases => throw new NotImplementedException();

        public EvaluatedFunction EvaluateAllAt(XPoint point)
        {
            double distance = crack.SignedDistanceFromBody(point);
            if (distance >= 0) return new EvaluatedFunction(+1, new double[2]);
            else return new EvaluatedFunction(-1, new double[2]);
        }

        public double EvaluateAt(XNode node)
        {
            double distance = crack.SignedDistanceFromBody(node);
            if (distance >= 0) return +1;
            else return -1;
        }

        public double EvaluateAt(XPoint point)
        {
            double distance = crack.SignedDistanceFromBody(point);
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
