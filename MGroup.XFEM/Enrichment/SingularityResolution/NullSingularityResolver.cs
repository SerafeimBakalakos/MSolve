using System;
using System.Collections.Generic;
using System.Text;
using MGroup.XFEM.Entities;

namespace MGroup.XFEM.Enrichment.SingularityResolution
{
    public class NullSingularityResolver : ISingularityResolver
    {
        public HashSet<XNode> FindStepEnrichedNodesToRemove(IEnumerable<XNode> stepNodes, PhaseStepEnrichment enrichment)
        {
            return new HashSet<XNode>();
        }
    }
}
