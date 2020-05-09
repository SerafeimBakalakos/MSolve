using System.Collections.Generic;
using ISAAR.MSolve.XFEM_OLD.Multiphase.Entities;

namespace ISAAR.MSolve.XFEM_OLD.Multiphase.Enrichment.SingularityResolution
{
    public interface ISingularityResolver
    {
        HashSet<XNode> FindStepEnrichedNodesToRemove(IEnumerable<XNode> stepNodes, StepEnrichmentOLD enrichment);
    }
}
