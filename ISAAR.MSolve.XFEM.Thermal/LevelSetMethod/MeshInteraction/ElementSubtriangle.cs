using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using ISAAR.MSolve.FEM.Interpolation;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.XFEM.Thermal.Elements;
using ISAAR.MSolve.XFEM.Thermal.Entities;

//TODO: Perhaps it should also store it sign, store Gauss points and interpolate/extrapolate
namespace ISAAR.MSolve.XFEM.Thermal.LevelSetMethod.MeshInteraction
{
    public class ElementSubtriangle
    {
        public ElementSubtriangle(IEnumerable<NaturalPoint> vertices)
        {
            this.Vertices = vertices.ToArray();
            Debug.Assert(Vertices.Length == 3);
        }

        public NaturalPoint[] Vertices { get; }

        public double CalcAreaNatural()
        {
            NaturalPoint v0 = Vertices[0];
            NaturalPoint v1 = Vertices[1];
            NaturalPoint v2 = Vertices[2];
            return 0.5 * Math.Abs(v0.Xi * (v1.Eta - v2.Eta) + v1.Xi * (v2.Eta - v0.Eta) + v2.Xi * (v0.Eta - v1.Eta));
        }

        public double CalcAreaCartesian(IXFiniteElement parentElement)
        {
            IIsoparametricInterpolation2D interpolation = parentElement.StandardInterpolation;
            IReadOnlyList<XNode> nodes = parentElement.Nodes;
            if (interpolation == InterpolationQuad4.UniqueInstance || interpolation == InterpolationTri3.UniqueInstance)
            {
                // The triangle edges will also be linear in Cartesian coordinate system, for Quad4 and Tri3 elements 
                CartesianPoint v0 = interpolation.TransformNaturalToCartesian(nodes, Vertices[0]);
                CartesianPoint v1 = interpolation.TransformNaturalToCartesian(nodes, Vertices[1]);
                CartesianPoint v2 = interpolation.TransformNaturalToCartesian(nodes, Vertices[2]);
                return 0.5 * Math.Abs(v0.X * (v1.Y - v2.Y) + v1.X * (v2.Y - v0.Y) + v2.X * (v0.Y - v1.Y));
            }
            else
            {
                //TODO: I need to write the equations. The Jacobian determinant comes into play, 
                //      but at how many points should it be calculated?
                throw new NotImplementedException();
            }
        }

        public NaturalPoint FindCentroid()
        {
            double xi = 0.0, eta = 0.0;
            for (int i = 0; i < 3; ++i)
            {
                xi += Vertices[i].Xi;
                eta += Vertices[i].Eta;
            }
            return new NaturalPoint(xi, eta);
        }
    }
}
