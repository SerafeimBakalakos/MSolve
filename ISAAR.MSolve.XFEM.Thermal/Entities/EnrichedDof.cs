using System.Text;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.XFEM.ThermalOLD.Enrichments.Functions;

namespace ISAAR.MSolve.XFEM.ThermalOLD.Entities
{
    public class EnrichedDof: IDofType
    {
        public IEnrichmentFunction Enrichment { get; }
        public IDofType StandardDof { get; }

        public EnrichedDof(IEnrichmentFunction enrichment, IDofType standardDof)
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
