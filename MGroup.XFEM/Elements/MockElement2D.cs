﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Discretization.Integration;
using ISAAR.MSolve.Discretization.Integration.Quadratures;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.Discretization.Mesh;
using ISAAR.MSolve.FEM.Interpolation;
using ISAAR.MSolve.FEM.Interpolation.Jacobians;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.Geometry.Triangulation;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Entities;
using MGroup.XFEM.Geometry;
using MGroup.XFEM.Geometry.ConformingMesh;
using MGroup.XFEM.Geometry.Primitives;
using MGroup.XFEM.Integration;

//TODO: delete this class
namespace MGroup.XFEM.Elements
{
    public class MockElement2D : IXFiniteElement2D
    {
        private readonly ElementEdge[] edges;
        private readonly ElementFace[] faces;

        public MockElement2D(int id, CellType cellType, IReadOnlyList<XNode> nodes)
        {
            this.ID = id;
            this.CellType = cellType;
            this.Nodes = nodes;

            if (this.CellType == CellType.Tri3)
            {
                IReadOnlyList<NaturalPoint> nodesNatural = InterpolationTri3.UniqueInstance.NodalNaturalCoordinates;
                edges = new ElementEdge[3];
                edges[0] = new ElementEdge(Nodes, nodesNatural, 0, 1);
                edges[1] = new ElementEdge(Nodes, nodesNatural, 1, 2);
                edges[2] = new ElementEdge(Nodes, nodesNatural, 2, 0);
            }
            else if (this.CellType == CellType.Quad4)
            {
                IReadOnlyList<NaturalPoint> nodesNatural = InterpolationQuad4.UniqueInstance.NodalNaturalCoordinates;
                edges = new ElementEdge[4];
                edges[0] = new ElementEdge(Nodes, nodesNatural, 0, 1);
                edges[1] = new ElementEdge(Nodes, nodesNatural, 1, 2);
                edges[2] = new ElementEdge(Nodes, nodesNatural, 2, 3);
                edges[3] = new ElementEdge(Nodes, nodesNatural, 3, 0);
            }
        }

        public CellType CellType { get; }

        public IElementDofEnumerator DofEnumerator 
        { 
            get => throw new NotImplementedException(); 
            set => throw new NotImplementedException(); 
        }

        public IElementType ElementType => throw new NotImplementedException();

        public int ID { get; set; }

        public IIsoparametricInterpolation2D Interpolation
        {
            get
            {
                if (CellType == CellType.Quad4) return InterpolationQuad4.UniqueInstance;
                else throw new NotImplementedException();
            }
        }

        public IReadOnlyList<XNode> Nodes { get; }
        IReadOnlyList<INode> IElement.Nodes => Nodes;

        public HashSet<IPhase2D> Phases { get; } = new HashSet<IPhase2D>();

        public XSubdomain Subdomain { get; set; }
        ISubdomain IElement.Subdomain => Subdomain;

        public IReadOnlyList<ElementEdge> Edges => edges;

        public IReadOnlyList<ElementFace> Faces => faces;

        public IBulkIntegration IntegrationBulk { get; set; }
        public ElementSubtriangle2D[] ConformingSubtriangles { get; set; }

        public List<IElementCurveIntersection2D> Intersections { get; } = new List<IElementCurveIntersection2D>();

        public IMatrix DampingMatrix(IElement element)
        {
            throw new NotImplementedException();
        }

        public IReadOnlyList<IReadOnlyList<IDofType>> GetElementDofTypes(IElement element)
        {
            throw new NotImplementedException();
        }

        public IMatrix MassMatrix(IElement element)
        {
            throw new NotImplementedException();
        }

        public IMatrix StiffnessMatrix(IElement element)
        {
            throw new NotImplementedException();
        }

        public double CalcArea()
        {
            if (this.CellType == CellType.Tri3)
            {
                var triangle = new Geometry.Primitives.Triangle2D();
                triangle.Vertices[0] = Nodes[0].Coordinates;
                triangle.Vertices[1] = Nodes[1].Coordinates;
                triangle.Vertices[2] = Nodes[2].Coordinates;
                return triangle.CalcArea();
            }
            else if (this.CellType == CellType.Quad4)
            {
                return ISAAR.MSolve.Geometry.Shapes.ConvexPolygon2D.CreateUnsafe(Nodes).ComputeArea();
            }
            else throw new NotImplementedException();
        }
    }
}
