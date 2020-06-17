using System.Collections.Generic;
using MGroup.XFEM.Entities;

namespace MGroup.XFEM.Enrichment.SingularityResolution
{
    public interface ISingularityResolver
    {
        HashSet<XNode> FindStepEnrichedNodesToRemove(IEnumerable<XNode> stepNodes, StepEnrichment enrichment);
    }
}
