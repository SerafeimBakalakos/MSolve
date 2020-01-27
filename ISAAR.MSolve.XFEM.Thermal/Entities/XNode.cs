using System.Collections.Generic;
using ISAAR.MSolve.FEM.Entities;
using ISAAR.MSolve.XFEM.ThermalOLD.Elements;
using ISAAR.MSolve.XFEM.ThermalOLD.Enrichments.Items;

namespace ISAAR.MSolve.XFEM.ThermalOLD.Entities
{
    public class XNode : Node
    {
        public XNode(int id, double x, double y) : base(id, x, y)
        {
            this.EnrichmentItems = new Dictionary<IEnrichmentItem, double[]>();
        }

        public XNode(int id, double x, double y, double z) : base(id, x, y, z)
        {
            this.EnrichmentItems = new Dictionary<IEnrichmentItem, double[]>();
        }

        public new Dictionary<int, IXFiniteElement> ElementsDictionary { get; } = new Dictionary<int, IXFiniteElement>();

        public Dictionary<IEnrichmentItem, double[]> EnrichmentItems { get; }

        public int EnrichedDofsCount
        {
            get
            {
                int count = 0;
                foreach (IEnrichmentItem enrichment in EnrichmentItems.Keys) count += enrichment.Dofs.Count;
                return count;
            }
        }

        public IReadOnlyList<EnrichedDof> EnrichedDofs
        {
            get
            {
                var dofs = new List<EnrichedDof>();
                foreach (IEnrichmentItem enrichment in EnrichmentItems.Keys) dofs.AddRange(enrichment.Dofs);
                return dofs;
            }
        }

        public bool IsEnriched => EnrichmentItems.Count > 0;

        //public new Dictionary<int, XSubdomain> SubdomainsDictionary { get; } = new Dictionary<int, XSubdomain>();

        public void BuildXSubdomainDictionary()
        {
            foreach (IXFiniteElement element in ElementsDictionary.Values)
            {
                SubdomainsDictionary[element.Subdomain.ID] = element.Subdomain;
            }
        }
    }
}