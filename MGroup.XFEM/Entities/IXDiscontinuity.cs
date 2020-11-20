using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using MGroup.XFEM.Enrichment;

namespace MGroup.XFEM.Entities
{
    /// <summary>
    /// Parent interface for cracks, phases or any geometric entity that interacts with the mesh and introduces enrichments.
    /// </summary>
    public interface IXDiscontinuity
    {
        int ID { get; }

        //TODO: Perhaps everything enrichment related should be calculated, stored and exposed by INodeEnricher.
        //      Also cracks are geometric components and do not necessarily have to define their enrichments. E.g. the exact same
        //      crack class should be usable for brittle and cohesive cracks, although the tip enrichment functions and the SIF
        //      calculation are different. For that matter the NodeEnricher component should be the same as well, as it just 
        //      locates which nodes to enrich with the crack's enrichments funcs
        IList<EnrichmentItem> DefineEnrichments(int numCurrentEnrichments);

        void InitializeGeometry();

        void InteractWithMesh(); //TODO: Should this be included in Initialize/UpdateGeometry()?

        /// <summary>
        /// 
        /// </summary>
        /// <param name="subdomainFreeDisplacements">Total displacements of all dofs of each subdomain.</param>
        void UpdateGeometry(Dictionary<int, Vector> subdomainFreeDisplacements);
    }
}
