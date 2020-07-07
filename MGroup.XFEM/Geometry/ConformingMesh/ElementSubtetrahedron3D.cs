﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ISAAR.MSolve.Discretization.Mesh;
using ISAAR.MSolve.Geometry.Coordinates;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Entities;
using MGroup.XFEM.Geometry.Primitives;
using MGroup.XFEM.Interpolation;

//TODO: Perhaps it should also store it sign, store Gauss points and interpolate/extrapolate
namespace MGroup.XFEM.Geometry.ConformingMesh
{
    public class ElementSubtetrahedron3D : IElementSubcell
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

        public CellType CellType => CellType.Tet4;

        public NaturalPoint[] VerticesNatural { get; }

        public NaturalPoint FindCentroidNatural() => Utilities.FindCentroidNatural(2, VerticesNatural);

        public (double[] centroid, double bulkSize) FindCentroidAndBulkSizeCartesian(IXFiniteElement parentElement)
        {
            IIsoparametricInterpolation interpolation = parentElement.Interpolation;
            if (interpolation == InterpolationTet4.UniqueInstance || interpolation == InterpolationHexa8.UniqueInstance)
            {
                // The tetrahedron edges will also be linear in Cartesian coordinate system, for Tetra4 and Hexa8 elements 
                IList<double[]> vertices = FindVerticesCartesian(parentElement);
                double[] centroid = Utilities.FindCentroid(vertices);
                double volume = Utilities.CalcTetrahedronVolume(vertices);
                return (centroid, volume);
            }
            else
            {
                //TODO: I need to write the equations. The Jacobian determinant comes into play, 
                //      but at how many points should it be calculated?
                throw new NotImplementedException();
            }
        }

        public IList<double[]> FindVerticesCartesian(IXFiniteElement parentElement)
        {
            IIsoparametricInterpolation interpolation = parentElement.Interpolation;
            IReadOnlyList<XNode> nodes = parentElement.Nodes;
            if (interpolation == InterpolationTet4.UniqueInstance || interpolation == InterpolationHexa8.UniqueInstance)
            {
                var verticesCartesian = new double[4][];
                for (int v = 0; v < 4; ++v)
                {
                    //double[] coordsNatural = tetraNatural.Vertices[v];
                    //var pointNatural = new NaturalPoint(coordsNatural[0], coordsNatural[1], coordsNatural[2]);
                    //verticesCartesian[v] = interpolation.TransformNaturalToCartesian(nodes, pointNatural);
                    verticesCartesian[v] = interpolation.TransformNaturalToCartesian(nodes, VerticesNatural[v].Coordinates);
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
