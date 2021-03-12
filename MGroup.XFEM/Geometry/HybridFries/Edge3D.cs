using System;
using System.Collections.Generic;
using System.Text;

//TODO: Perhaps tangent vector as property?
namespace MGroup.XFEM.Geometry.HybridFries
{
    public class Edge3D
    {
        public Edge3D(Vertex3D start, Vertex3D end)
        {
            Start = start;
            End = end;
        }

        public Vertex3D Start { get; }

        public Vertex3D End { get; }

        public bool HasVertices(Vertex3D vertex0, Vertex3D vertex1)
        {
            if ((vertex0 == Start) && (vertex1 == End)) return true;
            else if ((vertex1 == Start) && (vertex0 == End)) return true;
            else return false;
        }

        //public double SignedDistanceOf(double[] point)
        //{

        //}
    }
}
