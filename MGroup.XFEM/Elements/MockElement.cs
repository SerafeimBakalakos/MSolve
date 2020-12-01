using System;
using System.Collections.Generic;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.Discretization.Mesh;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using MGroup.XFEM.Enrichment;
using MGroup.XFEM.Entities;
using MGroup.XFEM.Geometry;
using MGroup.XFEM.Geometry.ConformingMesh;
using MGroup.XFEM.Geometry.Primitives;
using MGroup.XFEM.Integration;
using MGroup.XFEM.Integration.Quadratures;
using MGroup.XFEM.Interpolation;
using MGroup.XFEM.Materials;

//TODO: delete this class
namespace MGroup.XFEM.Elements
{
    public class MockElement : IXThermalElement
    {
        private readonly IElementGeometry elementGeometry;
        private readonly int dimension;

        public MockElement(int id, CellType cellType, IReadOnlyList<XNode> nodes)
        {
            this.ID = id;
            this.CellType = cellType;
            this.Nodes = nodes;

            if (this.CellType == CellType.Tri3)
            {
                dimension = 2;
                elementGeometry = new ElementTri3Geometry();
            }
            else if (this.CellType == CellType.Quad4)
            {
                dimension = 2;
                elementGeometry = new ElementQuad4Geometry();
            }
            else if (this.CellType == CellType.Tet4)
            {
                dimension = 3;
                elementGeometry = new ElementTet4Geometry();
            }
            else if (this.CellType == CellType.Hexa8)
            {
                dimension = 3;
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

        public IIsoparametricInterpolation Interpolation
        {
            get
            {
                if (CellType == CellType.Quad4) return InterpolationQuad4.UniqueInstance;
                else if (CellType == CellType.Hexa8) return InterpolationHexa8.UniqueInstance;
                else throw new NotImplementedException();
            }
        }

        public IReadOnlyList<XNode> Nodes { get; }
        IReadOnlyList<INode> IElement.Nodes => Nodes;

        public HashSet<IPhase> Phases { get; } = new HashSet<IPhase>();

        public Dictionary<IPhaseBoundary, IElementDiscontinuityInteraction> PhaseIntersections { get; } 
            = new Dictionary<IPhaseBoundary, IElementDiscontinuityInteraction>();

        public Dictionary<int, IElementDiscontinuityInteraction> InteractingDiscontinuities { get; }
            = new Dictionary<int, IElementDiscontinuityInteraction>();

        public XSubdomain Subdomain { get; set; }
        ISubdomain IElement.Subdomain => Subdomain;

        public ElementEdge[] Edges { get; }

        public ElementFace[] Faces { get; }

        public IBulkIntegration IntegrationBulk { get; set; }

        public IElementSubcell[] ConformingSubcells { get; set; }

        public IQuadrature IntegrationStandard => throw new NotImplementedException();

        public IEnumerable<GaussPoint> BulkIntegrationPoints => new GaussPoint[0];

        public IEnumerable<GaussPoint> BoundaryIntegrationPoints => new GaussPoint[0];

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

        public double CalcBulkSizeCartesian() => elementGeometry.CalcBulkSizeCartesian(Nodes);

        public double CalcBulkSizeNatural() => elementGeometry.CalcBulkSizeNatural();

        public void IdentifyDofs()
        {
        }

        public void IdentifyIntegrationPointsAndMaterials()
        {
        }

        public XPoint EvaluateFunctionsAt(double[] naturalPoint)
        {
            throw new NotImplementedException();
        }

        public Dictionary<IPhaseBoundary, (IReadOnlyList<GaussPoint>, IReadOnlyList<ThermalInterfaceMaterial>)> 
            GetMaterialsForBoundaryIntegration()
        {
            throw new NotImplementedException();
        }

        public (IReadOnlyList<GaussPoint>, IReadOnlyList<ThermalMaterial>) GetMaterialsForBulkIntegration()
        {
            throw new NotImplementedException();
        }

        public double[] FindCentroidCartesian() => Utilities.FindCentroidCartesian(dimension, Nodes);

        public class Factory : IXElementFactory<MockElement>
        {
           public MockElement CreateElement(int id, CellType cellType, IReadOnlyList<XNode> nodes)
            {
                return new MockElement(id, cellType, nodes);
            }
        }
    }
}
