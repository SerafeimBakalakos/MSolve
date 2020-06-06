using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ISAAR.MSolve.FEM.Interpolation;
using ISAAR.MSolve.Geometry.Coordinates;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Entities;
using MGroup.XFEM.Geometry.Primitives;

//TODO: Perhaps it should also store it sign, store Gauss points and interpolate/extrapolate
namespace MGroup.XFEM.Geometry.ConformingMesh
{
    public class ElementSubtetrahedron3D
    {
        private readonly Tetrahedron3D tetraNatural;

        public ElementSubtetrahedron3D(Tetrahedron3D tetraNatural)
        {
            this.tetraNatural = tetraNatural;
        }

        public (CartesianPoint centroid, double volume) FindCentroidAndVolumeCartesian(IXFiniteElement parentElement)
        {
            IIsoparametricInterpolation3D interpolation = parentElement.Interpolation3D;
            if (interpolation == InterpolationTet4.UniqueInstance || interpolation == InterpolationHexa8.UniqueInstance)
            {
                // The tetrahedron edges will also be linear in Cartesian coordinate system, for Tetra4 and Hexa8 elements 
                CartesianPoint[] vertices = GetVerticesCartesian(parentElement);

                var tetraCertesian = new Tetrahedron3D();
                for (int v = 0; v < 4; ++v)
                {
                    tetraCertesian.Vertices[v] = vertices[v].Coordinates;
                }
                double volume = tetraCertesian.CalcVolume();
                double[] centroid = tetraCertesian.FindCentroid();

                return (new CartesianPoint(centroid), volume);
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
            IIsoparametricInterpolation3D interpolation = parentElement.Interpolation3D;
            IReadOnlyList<XNode> nodes = parentElement.Nodes;
            if (interpolation == InterpolationTet4.UniqueInstance || interpolation == InterpolationHexa8.UniqueInstance)
            {
                var verticesCartesian = new CartesianPoint[4];
                for (int v = 0; v < 4; ++v)
                {
                    double[] coordsNatural = tetraNatural.Vertices[v];
                    var pointNatural = new NaturalPoint(coordsNatural[0], coordsNatural[1], coordsNatural[2]);
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
