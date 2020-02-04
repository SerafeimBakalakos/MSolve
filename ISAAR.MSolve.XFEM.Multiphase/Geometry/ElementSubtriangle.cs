using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ISAAR.MSolve.FEM.Interpolation;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.XFEM.Multiphase.Elements;
using ISAAR.MSolve.XFEM.Multiphase.Entities;

//TODO: Perhaps it should also store it sign, store Gauss points and interpolate/extrapolate
namespace ISAAR.MSolve.XFEM.Multiphase.Geometry
{
    public class ElementSubtriangle
    {
        public ElementSubtriangle(IEnumerable<NaturalPoint> vertices)
        {
            this.VerticesNatural = vertices.ToArray();
            Debug.Assert(VerticesNatural.Length == 3);
        }

        public NaturalPoint[] VerticesNatural { get; }

        public double CalcAreaNatural()
        {
            NaturalPoint v0 = VerticesNatural[0];
            NaturalPoint v1 = VerticesNatural[1];
            NaturalPoint v2 = VerticesNatural[2];
            return 0.5 * Math.Abs(v0.Xi * (v1.Eta - v2.Eta) + v1.Xi * (v2.Eta - v0.Eta) + v2.Xi * (v0.Eta - v1.Eta));
        }

        public double CalcAreaCartesian(IXFiniteElement parentElement)
        {
            IIsoparametricInterpolation2D interpolation = parentElement.InterpolationStandard;
            IReadOnlyList<XNode> nodes = parentElement.Nodes;
            if (interpolation == InterpolationQuad4.UniqueInstance || interpolation == InterpolationTri3.UniqueInstance)
            {
                // The triangle edges will also be linear in Cartesian coordinate system, for Quad4 and Tri3 elements 
                CartesianPoint v0 = interpolation.TransformNaturalToCartesian(nodes, VerticesNatural[0]);
                CartesianPoint v1 = interpolation.TransformNaturalToCartesian(nodes, VerticesNatural[1]);
                CartesianPoint v2 = interpolation.TransformNaturalToCartesian(nodes, VerticesNatural[2]);
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
                xi += VerticesNatural[i].Xi;
                eta += VerticesNatural[i].Eta;
            }
            return new NaturalPoint(xi, eta);
        }

        public CartesianPoint[] GetVerticesCartesian(IXFiniteElement parentElement)
        {
            IIsoparametricInterpolation2D interpolation = parentElement.InterpolationStandard;
            IReadOnlyList<XNode> nodes = parentElement.Nodes;
            if (interpolation == InterpolationQuad4.UniqueInstance || interpolation == InterpolationTri3.UniqueInstance)
            {
                return VerticesNatural.Select(v => interpolation.TransformNaturalToCartesian(nodes, v)).ToArray();
            }
            else
            {
                //TODO: I should probably return a whole element in this case. E.g. Tri6.
                throw new NotImplementedException();
            }
        }
    }
}
