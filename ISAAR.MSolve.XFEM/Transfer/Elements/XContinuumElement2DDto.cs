using ISAAR.MSolve.Discretization.Mesh;
using ISAAR.MSolve.XFEM.Elements;
using ISAAR.MSolve.XFEM.Entities;
using ISAAR.MSolve.XFEM.Materials;
using ISAAR.MSolve.XFEM.Transfer.Elements;
using System;
using System.Collections.Generic;
using System.Text;

//TODO: In general I need a class to transfer continuum elements, with all posible combinations of interpolations, integrations
//      and different material per gauss point. Going further, I need a class to transfer all possible elements 
//      (continuum, structural, thermal,2 2D, 3D, etc).
namespace ISAAR.MSolve.XFEM.Transfer.Elements
{
    [Serializable]
    public class XContinuumElement2DDto : IXElementDto
    {
        public CellType cellType;
        public int id;
        public int integrationStrategy;
        public int jIntegrationStrategy;
        public int material;
        public int[] nodes;
        
        public XContinuumElement2DDto(XContinuumElement2D element)
        {
            this.id = element.ID;
            cellType = element.CellType;
            nodes = new int[element.Nodes.Count];
            for (int n = 0; n < element.Nodes.Count; ++n) nodes[n] = element.Nodes[n].ID;
            this.material = element.Material.ID;

            throw new NotImplementedException("What about integration strategies?");
        }

        public IXFiniteElement Deserialize(IReadOnlyDictionary<int, XNode> allNodes, 
            Dictionary<int, IXMaterialField2D> allMaterials)
        {
            throw new NotImplementedException("What about integration strategies?");
            var factory = new XContinuumElement2DFactory(null, null, allMaterials[material]);
            var elemNodes = new XNode[nodes.Length];
            for (int n = 0; n < nodes.Length; ++n) elemNodes[n] = allNodes[nodes[n]];
            return factory.CreateElement(id, cellType, elemNodes);
        }
    }
}
