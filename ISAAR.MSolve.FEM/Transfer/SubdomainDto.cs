using ISAAR.MSolve.Discretization;
using ISAAR.MSolve.FEM.Entities;
using ISAAR.MSolve.FEM.Transfer.Elements;
using ISAAR.MSolve.Materials.Interfaces;
using ISAAR.MSolve.Materials.Transfer;
using System;
using System.Collections.Generic;
using System.Text;

namespace ISAAR.MSolve.FEM.Transfer
{
    public struct SubdomainDto
    {
        public int id;
        public NodeDto[] nodes;
        public IElementDto[] elements;
        public IMaterialDto[] materials;
        public NodalDisplacementDto[] nodalDisplacements;

        public SubdomainDto(Subdomain subdomain)
        {
            this.id = subdomain.ID;

            // Nodes
            IReadOnlyList<Node> actualNodes = subdomain.Nodes;
            this.nodes = new NodeDto[actualNodes.Count];
            for (int n = 0; n < actualNodes.Count; ++n) this.nodes[n] = new NodeDto(actualNodes[n]);

            // Elements
            IReadOnlyList<Element> actualElements = subdomain.Elements;
            this.elements = new IElementDto[actualElements.Count];
            var elementSerializer = new ElementSerializer();
            for (int e = 0; e < actualElements.Count; ++e) this.elements[e] = elementSerializer.Serialize(actualElements[e]);

            // Materials
            // More than 1 elements may have the same material properties. First gather the unique ones.
            var uniqueMaterials = new Dictionary<int, IFiniteElementMaterial>();
            foreach (Element element in actualElements)
            {
                // Each element is assumed to have the same material at all GPs.
                IFiniteElementMaterial elementMaterial = element.ElementType.Materials[0];
                uniqueMaterials[elementMaterial.ID] = elementMaterial;
            }
            this.materials = new IMaterialDto[uniqueMaterials.Count];
            var materialSerializer = new MaterialSerializer();
            int counter = 0;
            foreach (IFiniteElementMaterial material in uniqueMaterials.Values)
            {
                materials[counter++] = materialSerializer.Serialize(material);
            }

            // Displacements
            var displacements = new List<NodalDisplacementDto>();
            foreach (Node node in actualNodes)
            {
                foreach (Constraint constraint in node.Constraints)
                {
                    displacements.Add(new NodalDisplacementDto(node, constraint));
                }
            }
            this.nodalDisplacements = displacements.ToArray();
        }

        public Subdomain Deserialize()
        {
            var subdomain = new Subdomain(this.id);

            // Nodes
            var allNodes = new Dictionary<int, Node>();
            foreach (NodeDto n in this.nodes)
            {
                Node node = n.Deserialize();
                subdomain.Nodes.Add(node);
                allNodes[node.ID] = node;
            }

            // Materials
            var allMaterials = new Dictionary<int, IFiniteElementMaterial>();
            foreach (IMaterialDto m in this.materials) allMaterials[m.ID] = m.Deserialize();

            // Elements
            foreach (IElementDto e in this.elements) subdomain.Elements.Add(e.Deserialize(allNodes, allMaterials));

            // Displacements
            foreach (NodalDisplacementDto d in this.nodalDisplacements) d.Deserialize(allNodes);

            return subdomain;
        }
    }
}
