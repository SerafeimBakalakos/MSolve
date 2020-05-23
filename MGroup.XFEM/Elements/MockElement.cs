using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Discretization.Integration;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.Discretization.Mesh;
using ISAAR.MSolve.FEM.Interpolation;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Entities;

//TODO: delete this class
namespace MGroup.XFEM.Elements
{
    public class MockElement : IXFiniteElement
    {
        public MockElement(int id, CellType cellType, IReadOnlyList<XNode> nodes)
        {
            this.ID = id;
            this.CellType = cellType;
            this.Nodes = nodes;
        }

        public CellType CellType { get; }

        public IElementDofEnumerator DofEnumerator 
        { 
            get => throw new NotImplementedException(); 
            set => throw new NotImplementedException(); 
        }

        public IReadOnlyList<(XNode node1, XNode node2)> EdgeNodes
        {
            get
            {
                if (Nodes.Count > 4) throw new NotImplementedException();
                else
                {
                    var edges = new (XNode node1, XNode node2)[Nodes.Count];
                    for (int i = 0; i < Nodes.Count; ++i)
                    {
                        XNode node1 = Nodes[i];
                        XNode node2 = Nodes[(i + 1) % Nodes.Count];
                        edges[i] = (node1, node2);
                    }
                    return edges;
                }
            }
        }

        public IReadOnlyList<(NaturalPoint node1, NaturalPoint node2)> EdgesNodesNatural
        {
            get
            {
                var nodesNatural = new NaturalPoint[4];
                nodesNatural[0] = new NaturalPoint(-1.0, -1.0);
                nodesNatural[1] = new NaturalPoint(+1.0, -1.0);
                nodesNatural[2] = new NaturalPoint(+1.0, +1.0);
                nodesNatural[3] = new NaturalPoint(-1.0, +1.0);

                var edges = new (NaturalPoint node1, NaturalPoint node2)[4];
                for (int i = 0; i < Nodes.Count; ++i)
                {
                    NaturalPoint node1 = nodesNatural[i];
                    NaturalPoint node2 = nodesNatural[(i + 1) % Nodes.Count];
                    edges[i] = (node1, node2);
                }
                return edges;
            }
        }

        public IElementType ElementType => throw new NotImplementedException();

        public int ID { get; set; }

        public IIsoparametricInterpolation2D InterpolationStandard => InterpolationQuad4.UniqueInstance;

        public IReadOnlyList<XNode> Nodes { get; }
        IReadOnlyList<INode> IElement.Nodes => Nodes;

        public HashSet<IPhase> Phases { get; } = new HashSet<IPhase>();

        public XSubdomain Subdomain { get; set; }
        ISubdomain IElement.Subdomain => Subdomain;

        public IReadOnlyList<ElementEdge> Edges => throw new NotImplementedException();

        public IReadOnlyList<ElementFace> Faces => throw new NotImplementedException();

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
    }
}
