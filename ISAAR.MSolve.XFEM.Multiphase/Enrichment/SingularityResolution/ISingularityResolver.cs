using System.Collections.Generic;
using ISAAR.MSolve.XFEM.Multiphase.Entities;

namespace ISAAR.MSolve.XFEM.Multiphase.Enrichment.SingularityResolution
{
    public interface ISingularityResolver
    {
        HashSet<XNode> FindStepEnrichedNodesToRemove(IEnumerable<XNode> stepNodes, StepEnrichment enrichment);
    }
}
