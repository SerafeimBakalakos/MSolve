using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ISAAR.MSolve.FEM.Interpolation;
using ISAAR.MSolve.Geometry.Coordinates;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Entities;

//TODO: Perhaps it should also store it sign, store Gauss points and interpolate/extrapolate
namespace MGroup.XFEM.Geometry.ConformingMesh
{
    public class ElementSubtriangle2D
    {
        private readonly TriangleCell2D triangleNatural;

        public ElementSubtriangle2D(TriangleCell2D triangleNatural)
        {
            this.triangleNatural = triangleNatural;
        }

        public (CartesianPoint centroid, double area) FindCentroidAndAreaCartesian(IXFiniteElement parentElement)
        {
            IIsoparametricInterpolation2D interpolation = parentElement.InterpolationStandard;
            if (interpolation == InterpolationQuad4.UniqueInstance || interpolation == InterpolationTri3.UniqueInstance)
            {
                // The triangle edges will also be linear in Cartesian coordinate system, for Quad4 and Tri3 elements 
                CartesianPoint[] vertices = GetVerticesCartesian(parentElement);

                CartesianPoint v0 = vertices[0];
                CartesianPoint v1 = vertices[1];
                CartesianPoint v2 = vertices[2];

                double x = (v0.X + v1.X + v2.X) / 3.0;
                double y = (v0.Y + v1.Y + v2.Y) / 3.0;
                double area = 0.5 * Math.Abs(v0.X * (v1.Y - v2.Y) + v1.X * (v2.Y - v0.Y) + v2.X * (v0.Y - v1.Y));

                return (new CartesianPoint(x, y), area);
            }
            else
            {
                //TODO: I need to write the equations. The Jacobian determinant comes into play, 
                //      but at how many points should it be calculated?
                throw new NotImplementedException();
            }
        }

        public CartesianPoint[] GetVerticesCartesian(IXFiniteElement parentElement)
        {
            IIsoparametricInterpolation2D interpolation = parentElement.InterpolationStandard;
            IReadOnlyList<XNode> nodes = parentElement.Nodes;
            if (interpolation == InterpolationQuad4.UniqueInstance || interpolation == InterpolationTri3.UniqueInstance)
            {
                var verticesCartesian = new CartesianPoint[3];
                for (int v = 0; v < 3; ++v)
                {
                    double[] coordsNatural = triangleNatural.Vertices[v];
                    var pointNatural = new NaturalPoint(coordsNatural[0], coordsNatural[1]);
                    verticesCartesian[v] = interpolation.TransformNaturalToCartesian(nodes, pointNatural);
                }
                return verticesCartesian;
            }
            else
            {
                //TODO: I should probably return a whole element in this case. E.g. Tri6.
                throw new NotImplementedException();
            }
        }
    }
}
