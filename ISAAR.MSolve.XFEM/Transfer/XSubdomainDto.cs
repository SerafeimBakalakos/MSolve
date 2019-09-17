using ISAAR.MSolve.Discretization;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.Discretization.Transfer;
using ISAAR.MSolve.XFEM.Elements;
using ISAAR.MSolve.XFEM.Entities;
using ISAAR.MSolve.XFEM.Materials;
using ISAAR.MSolve.XFEM.Transfer.Elements;
using ISAAR.MSolve.XFEM.Transfer.Materials;
using System;
using System.Collections.Generic;
using System.Text;

namespace ISAAR.MSolve.XFEM.Transfer
{
    [Serializable]
    public class XSubdomainDto //TODO: dofs, enrichments
    {
        public int id;
        public XNodeDto[] nodes;
        public IXElementDto[] elements;
        public IXMaterialFieldDto[] materials;
        public XNodalDisplacementDto[] nodalDisplacements;
        public XNodalLoadDto[] nodalLoads;

        public static XSubdomainDto CreateEmpty() => new XSubdomainDto();

        public static XSubdomainDto Serialize(XSubdomain subdomain, IDofSerializer dofSerializer)
        {
            throw new NotImplementedException("What about integration strategies, dofs and enrichments?");

            var dto = new XSubdomainDto();
            dto.id = subdomain.ID;

            // Dofs


            // Enrichments


            // Nodes
            dto.nodes = new XNodeDto[subdomain.NumNodes];
            int n = 0;
            foreach (XNode node in subdomain.Nodes.Values) dto.nodes[n++] = new XNodeDto(node);

            // Materials
            // More than 1 elements may have the same material properties. First gather the unique ones.
            var uniqueMaterials = new Dictionary<int, IXMaterialField2D>();
            foreach (IXFiniteElement element in subdomain.Elements.Values)
            {
                // Each element is assumed to have the same material at all GPs.
                IXMaterialField2D elementMaterial = element.Material;
                uniqueMaterials[elementMaterial.ID] = elementMaterial;
            }
            dto.materials = new IXMaterialFieldDto[uniqueMaterials.Count];
            var materialSerializer = new XMaterialSerializer();
            int counter = 0;
            foreach (IXMaterialField2D material in uniqueMaterials.Values)
            {
                dto.materials[counter++] = materialSerializer.Serialize(material);
            }

            // Elements
            dto.elements = new IXElementDto[subdomain.NumElements];
            var elementSerializer = new XElementSerializer();
            int e = 0;
            foreach (IXFiniteElement element in subdomain.Elements.Values) dto.elements[e++] = elementSerializer.Serialize(element);

            // Displacements
            var displacements = new List<XNodalDisplacementDto>();
            foreach (XNode node in subdomain.Nodes.Values)
            {
                foreach (Constraint constraint in node.Constraints)
                {
                    displacements.Add(new XNodalDisplacementDto(node, constraint, dofSerializer));
                }
            }
            dto.nodalDisplacements = displacements.ToArray();

            // Nodal loads
            dto.nodalLoads = new XNodalLoadDto[subdomain.NodalLoads.Count];
            for (int i = 0; i < subdomain.NodalLoads.Count; ++i)
            {
                dto.nodalLoads[i] = new XNodalLoadDto(subdomain.NodalLoads[i], dofSerializer);
            }

            return dto;
        }

        public XSubdomain Deserialize(IDofSerializer dofSerializer)
        {
            throw new NotImplementedException("What about integration strategies, dofs and enrichments?");


            var subdomain = new XSubdomain(this.id);

            // Dofs
            

            // Enrichments
            
            
            // Nodes
            foreach (XNodeDto n in this.nodes)
            {
                XNode node = n.Deserialize();
                subdomain.Nodes[node.ID] = node;
            }

            // Materials
            var allMaterials = new Dictionary<int, IXMaterialField2D>();
            foreach (IXMaterialFieldDto m in this.materials) allMaterials[m.ID] = m.Deserialize();

            // Elements
            foreach (IXElementDto e in this.elements)
            {
                IXFiniteElement element = e.Deserialize(subdomain.Nodes, allMaterials);
                subdomain.Elements.Add(element.ID, element);
            }

            // Displacements
            foreach (XNodalDisplacementDto d in this.nodalDisplacements) d.Deserialize(subdomain.Nodes, dofSerializer);

            // Nodal loads
            foreach (XNodalLoadDto nl in this.nodalLoads) subdomain.NodalLoads.Add(nl.Deserialize(subdomain.Nodes, dofSerializer));

            return subdomain;
        }
    }
}
