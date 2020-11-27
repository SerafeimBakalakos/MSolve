using System;
using System.Collections.Generic;
using System.Text;
using MGroup.XFEM.Entities;
using MGroup.XFEM.Geometry.Primitives;

//TODO: Some properties only apply to multiphase problems. Move these elsewhere
namespace MGroup.XFEM.Enrichment
{
    public interface IEnrichmentFunction
    {
        //MODIFICATION NEEDED. Remove this from IEnrichmentFunction. It is used only to check if a junction enrichment covers the case of a step enrichment. In previous versions of the code it had other uses, that were very trivial.
        //TODO: Not sure about this. This necessitates that the enrichment between phase0 and phase1 is different than the one 
        //      between phase0 and phase2. This does not allow step enrichments to be defined as in/out of a phase
        IReadOnlyList<IPhase> Phases { get; }

        double EvaluateAt(XNode node);

        //TODO: Perhaps the argument should be the phase itself. Also the same argument should be used for materials.
        double EvaluateAt(XPoint point);

        EvaluatedFunction EvaluateAllAt(XPoint point);

        /// <summary>
        /// Evaluates the jump of the enrichment function across a discontinuity. Every discontinuity has a positive and a 
        /// negative side. The jump is always considered as [[f(x)]] = f(x+) - f(x-).
        /// </summary>
        /// <param name="discontinuity"></param>
        /// <param name="point"></param>
        /// <returns></returns>
        double EvaluateJumpAcross(IXDiscontinuity discontinuity, XPoint point);
    }
}
