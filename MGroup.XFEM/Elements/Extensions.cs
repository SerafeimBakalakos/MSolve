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
        public static NaturalPoint FindElementCentroid(IXFiniteElement element)
        {
            IReadOnlyList<double[]> nodes = element.Interpolation.NodalNaturalCoordinates;
            int dimension = nodes[0].Length;
            var centroid = new double[dimension];
            foreach (double[] node in nodes)
            {
                for (int i = 0; i < dimension; ++i)
                {
                    centroid[i] += node[i];
                }
            }
            for (int i = 0; i < dimension; ++i)
            {
                centroid[i] /= nodes.Count;
            }
            return new NaturalPoint(centroid);
        }

        public static double[] FindCentroidCartesian(this IElementGeometry elemGeom, int dimension, IReadOnlyList<XNode> nodes)
        {
            var centroid = new double[dimension];
            for (int n = 0; n < nodes.Count; ++n)
            {
                for (int j = 0; j < dimension; ++j)
                {
                    centroid[j] += nodes[n].Coordinates[j];
                }
            }
            for (int j = 0; j < dimension; ++j)
            {
                centroid[j] /= nodes.Count;
            }
            return centroid;
        }

        public static HashSet<ElementFace> FindFacesOfEdge(this ElementEdge edge, IEnumerable<ElementFace> faces)
        {
            var facesOfEdge = new HashSet<ElementFace>();
            foreach (ElementFace face in faces)
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
    }
}
