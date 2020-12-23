using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ISAAR.MSolve.Geometry.Coordinates;
using MGroup.XFEM.Entities;

namespace MGroup.XFEM.Elements
{
    public static class Extensions
    {
        public static HashSet<ElementFace> FindFacesOfEdge(this ElementEdge edge, IEnumerable<ElementFace> faces)
        {
            var facesOfEdge = new HashSet<ElementFace>();
            foreach (ElementFace face in faces)
            {
                if (face.Edges.Contains(edge)) facesOfEdge.Add(face);
            }
            return facesOfEdge;
        }

        public static HashSet<ElementFace_NEW> FindFacesOfEdge(this ElementEdge_NEW edge, IEnumerable<ElementFace_NEW> faces)
        {
            var facesOfEdge = new HashSet<ElementFace_NEW>();
            foreach (ElementFace_NEW face in faces)
            {
                if (face.Edges.Contains(edge)) facesOfEdge.Add(face);
            }
            return facesOfEdge;
        }

        public static HashSet<ElementFace> FindFacesOfNode(this XNode node, IEnumerable<ElementFace> faces)
        {
            var facesOfNode = new HashSet<ElementFace>();
            foreach (ElementFace face in faces)
            {
                if (face.Nodes.Contains(node)) facesOfNode.Add(face);
            }
            return facesOfNode;
        }

        //MODIFICATION NEEDED: Move this elsewhere. Shouldn't these methods be together?
        public static HashSet<ElementFace_NEW> FindFacesOfNode(int nodeID, IEnumerable<ElementFace_NEW> faces)
        {
            var facesOfNode = new HashSet<ElementFace_NEW>();
            foreach (ElementFace_NEW face in faces)
            {
                if (face.NodeIDs.Contains(nodeID)) facesOfNode.Add(face);
            }
            return facesOfNode;
        }
    }
}
