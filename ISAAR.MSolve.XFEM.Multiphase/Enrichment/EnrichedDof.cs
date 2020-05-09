using System.Text;
using ISAAR.MSolve.Discretization.FreedomDegrees;

namespace ISAAR.MSolve.XFEM_OLD.Multiphase.Enrichment
{
    public class EnrichedDof: IDofType
    {
        public IEnrichment Enrichment { get; }
        public IDofType StandardDof { get; }

        public EnrichedDof(IEnrichment enrichment, IDofType standardDof)
        {
            this.Enrichment = enrichment;
            this.StandardDof = standardDof;
        }
        
        public override string ToString()
        {
            var builder = new StringBuilder();
            builder.Append(Enrichment.ToString());
            builder.Append(" enriched ");
            builder.Append(StandardDof);
            return builder.ToString();
        }
    }
}
