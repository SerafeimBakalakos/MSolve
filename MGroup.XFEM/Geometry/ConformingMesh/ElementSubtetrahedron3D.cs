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
        //private readonly Tetrahedron3D tetraNatural;

        public ElementSubtetrahedron3D(Tetrahedron3D tetraNatural)
        {
            IList<double[]> vertices = tetraNatural.Vertices;
            VerticesNatural = new NaturalPoint[4];
            VerticesNatural[0] = new NaturalPoint(vertices[0][0], vertices[0][1], vertices[0][2]);
            VerticesNatural[1] = new NaturalPoint(vertices[1][0], vertices[1][1], vertices[1][2]);
            VerticesNatural[2] = new NaturalPoint(vertices[2][0], vertices[2][1], vertices[2][2]);
            VerticesNatural[3] = new NaturalPoint(vertices[3][0], vertices[3][1], vertices[3][2]);
            //this.tetraNatural = tetraNatural;
        }

        public NaturalPoint[] VerticesNatural { get; }

        public NaturalPoint FindCentroidNatural()
        {
            double centroidXi = 0.0, centroidEta = 0.0, centroidZeta = 0.0;
            for (int v = 0; v < 4; ++v)
            {
                centroidXi += VerticesNatural[v].Xi;
                centroidEta += VerticesNatural[v].Eta;
                centroidZeta += VerticesNatural[v].Zeta;
            }
            return new NaturalPoint(centroidXi / 4.0, centroidEta / 4.0, centroidZeta / 4.0);
        }

        public (CartesianPoint centroid, double volume) FindCentroidAndVolumeCartesian(IXFiniteElement3D parentElement)
        {
            IIsoparametricInterpolation3D interpolation = parentElement.Interpolation;
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

        public CartesianPoint[] GetVerticesCartesian(IXFiniteElement3D parentElement)
        {
            IIsoparametricInterpolation3D interpolation = parentElement.Interpolation;
            IReadOnlyList<XNode> nodes = parentElement.Nodes;
            if (interpolation == InterpolationTet4.UniqueInstance || interpolation == InterpolationHexa8.UniqueInstance)
            {
                var verticesCartesian = new CartesianPoint[4];
                for (int v = 0; v < 4; ++v)
                {
                    //double[] coordsNatural = tetraNatural.Vertices[v];
                    //var pointNatural = new NaturalPoint(coordsNatural[0], coordsNatural[1], coordsNatural[2]);
                    //verticesCartesian[v] = interpolation.TransformNaturalToCartesian(nodes, pointNatural);
                    verticesCartesian[v] = interpolation.TransformNaturalToCartesian(nodes, VerticesNatural[v]);
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
