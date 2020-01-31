using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.XFEM.Multiphase.Entities;

namespace ISAAR.MSolve.XFEM.Multiphase.Enrichment
{
    public interface IEnrichment
    {
        EnrichedDof Dof { get; }
        int ID { get; }

        double EvaluateAt(XNode node);
    }
}
