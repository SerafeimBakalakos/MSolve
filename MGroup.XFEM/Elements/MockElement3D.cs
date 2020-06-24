﻿using System;
using System.Collections.Generic;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Discretization.Integration;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.Discretization.Mesh;
using ISAAR.MSolve.FEM.Interpolation;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using MGroup.XFEM.Entities;
using MGroup.XFEM.Geometry;
using MGroup.XFEM.Geometry.ConformingMesh;
using MGroup.XFEM.Geometry.Primitives;
using MGroup.XFEM.Integration;

using MGroup.XFEM.Materials;

//TODO: delete this class
namespace MGroup.XFEM.Elements
{
    public class MockElement3D : IXFiniteElement3D
    {
        private readonly IElementGeometry3D elementGeometry;

        public MockElement3D(int id, CellType cellType, IReadOnlyList<XNode> nodes)
        {
            this.ID = id;
            this.CellType = cellType;
            this.Nodes = nodes;

            if (this.CellType == CellType.Tet4)
            {
                elementGeometry = new ElementTet4Geometry();
            }
            else if (this.CellType == CellType.Hexa8)
            {
                elementGeometry = new ElementHexa8Geometry();
            }

            (Edges, Faces) = elementGeometry.FindEdgesFaces(nodes);

        }

        public CellType CellType { get; }

        public IElementDofEnumerator DofEnumerator 
        { 
            get => throw new NotImplementedException(); 
            set => throw new NotImplementedException(); 
        }

        public IElementType ElementType => throw new NotImplementedException();

        public int ID { get; set; }

        public IIsoparametricInterpolation3D Interpolation
        {
            get
            {
                if (CellType == CellType.Hexa8) return InterpolationHexa8.UniqueInstance;
                else throw new NotImplementedException();
            }
        }

        public IReadOnlyList<XNode> Nodes { get; }
        IReadOnlyList<INode> IElement.Nodes => Nodes;

        public HashSet<IPhase> Phases { get; } = new HashSet<IPhase>();

        public Dictionary<PhaseBoundary, IElementGeometryIntersection> PhaseIntersections { get; } 
            = new Dictionary<PhaseBoundary, IElementGeometryIntersection>();

        public XSubdomain Subdomain { get; set; }
        ISubdomain IElement.Subdomain => Subdomain;

        public ElementEdge[] Edges { get; }

        public ElementFace[] Faces { get; }

        public IBulkIntegration IntegrationBulk { get; set; }

        public ElementSubtetrahedron3D[] ConformingSubtetrahedra { get; set; }

        public List<IElementSurfaceIntersection3D> Intersections { get; } = new List<IElementSurfaceIntersection3D>();

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

        public double CalcVolume() => elementGeometry.CalcVolume(Nodes);

        public void IdentifyDofs()
        {
        }

        public void IdentifyIntegrationPointsAndMaterials()
        {
        }

        public XPoint EvaluateFunctionsAt(NaturalPoint point)
        {
            throw new NotImplementedException();
        }

        public Dictionary<PhaseBoundary, (IReadOnlyList<GaussPoint>, IReadOnlyList<ThermalInterfaceMaterial>)> GetMaterialsForBoundaryIntegration()
        {
            throw new NotImplementedException();
        }

        public (IReadOnlyList<GaussPoint>, IReadOnlyList<ThermalMaterial>) GetMaterialsForBulkIntegration()
        {
            throw new NotImplementedException();
        }
    }
}
