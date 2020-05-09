using System.Collections.Generic;
using ISAAR.MSolve.FEM.Entities;
using ISAAR.MSolve.XFEM_OLD.Multiphase.Elements;
using ISAAR.MSolve.XFEM_OLD.Multiphase.Enrichment;

namespace ISAAR.MSolve.XFEM_OLD.Multiphase.Entities
{
    public class XNode : Node
    {
        public XNode(int id, double x, double y) : base(id, x, y)
        {
        }

        public XNode(int id, double x, double y, double z) : base(id, x, y, z)
        {
        }

        public new Dictionary<int, IXFiniteElement> ElementsDictionary { get; } = new Dictionary<int, IXFiniteElement>();

        public Dictionary<IEnrichment, double> Enrichments { get; } = new Dictionary<IEnrichment, double>();

        public int EnrichedDofsCount => Enrichments.Count;

        public IReadOnlyList<EnrichedDof> EnrichedDofs
        {
            get
            {
                var dofs = new List<EnrichedDof>();
                foreach (IEnrichment enrichment in Enrichments.Keys) dofs.Add(enrichment.Dof);
                return dofs;
            }
        }

        public bool IsEnriched => Enrichments.Count > 0;

        public IPhase SurroundingPhase { get; set; }

        //public new Dictionary<int, XSubdomain> SubdomainsDictionary { get; } = new Dictionary<int, XSubdomain>();
    }
}