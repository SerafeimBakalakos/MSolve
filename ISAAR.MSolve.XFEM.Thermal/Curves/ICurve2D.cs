﻿using System.Collections.Generic;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.XFEM_OLD.Thermal.Elements;
using ISAAR.MSolve.XFEM_OLD.Thermal.Entities;
using ISAAR.MSolve.XFEM_OLD.Thermal.Curves.MeshInteraction;

//TODO: Curves should have IDs
//TODO: Duplication between this and Geometry.Shapes.ICurve2D
namespace ISAAR.MSolve.XFEM_OLD.Thermal.Curves
{
    public interface ICurve2D
    {
        double Thickness { get; } //TODO: Probably delete this

        ISet<NaturalPoint> FindConformingTriangleVertices(IXFiniteElement element, CurveElementIntersection intersection);

        CurveElementIntersection IntersectElement(IXFiniteElement element);

        double SignedDistanceOf(XNode node);

        double SignedDistanceOf(IXFiniteElement element, double[] shapeFunctionsAtNaturalPoint);
    }
}