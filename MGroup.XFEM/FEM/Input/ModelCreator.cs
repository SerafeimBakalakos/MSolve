using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ISAAR.MSolve.FEM.Entities;
using ISAAR.MSolve.Materials;
using ISAAR.MSolve.Materials.Interfaces;
using MGroup.XFEM.FEM.Elements;
using MGroup.XFEM.FEM.Mesh;
using MGroup.XFEM.FEM.Mesh.GMSH;

namespace MGroup.XFEM.FEM.Input
{
    public class ModelCreator
    {
        public Model CreateModel3D(PreprocessingMesh mesh, Dictionary<int, IContinuumMaterial> materialsOfPhysicalGroups)
        {
            var model = new Model();
            int subdomainID = 0;
            model.SubdomainsDictionary[subdomainID] = new Subdomain(subdomainID);

            foreach (Vertex vertex in mesh.Vertices)
            {
                model.NodesDictionary[vertex.ID] = new Node(vertex.ID, vertex.Coords[0], vertex.Coords[1], vertex.Coords[2]);
            }

            var dynamicProperties = new DynamicMaterial(1, 1, 1);
            var elementFactory = new ContinuumElement3DFactory();
            foreach (Cell cell in mesh.Cells)
            {
                // Clone it to ensure input object can be reused.
                var material = (IContinuumMaterial)(materialsOfPhysicalGroups[cell.PhysicalGroup].Clone());
                Node[] nodes = cell.VertexIDs.Select(v => model.NodesDictionary[v]).ToArray();
                ContinuumElement3D elementType = elementFactory.CreateElement(cell.CellType, nodes, material, dynamicProperties);
                var elementWrapper = new Element() { ID = cell.ID, ElementType = elementType };
                elementWrapper.AddNodes(nodes);
                model.ElementsDictionary.Add(elementWrapper.ID, elementWrapper);
                model.SubdomainsDictionary[subdomainID].Elements.Add(elementWrapper);
            }

            return model;
        }
    }
}
