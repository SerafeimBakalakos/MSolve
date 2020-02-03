using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.Discretization.Mesh;
using ISAAR.MSolve.FEM.Interpolation;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.XFEM.Multiphase.Entities;
using ISAAR.MSolve.XFEM.Multiphase.Geometry;
using ISAAR.MSolve.XFEM.Multiphase.Integration;

namespace ISAAR.MSolve.XFEM.Multiphase.Elements
{
    public class MockQuad4 : IXFiniteElement
    {
        public MockQuad4(int id, IReadOnlyList<XNode> nodes)
        {
            this.ID = id;
            this.CellType = CellType.Quad4;
            this.Nodes = nodes;
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

        public IReadOnlyList<XNode> Nodes { get; }

        public IIsoparametricInterpolation2D StandardInterpolation => InterpolationQuad4.UniqueInstance;

        public XSubdomain Subdomain { get; set; }

        public int ID { get; set; }

        public IElementType ElementType => throw new NotImplementedException();

        public CellType CellType { get; }

        public IElementDofEnumerator DofEnumerator { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public Dictionary<PhaseBoundary, CurveElementIntersection> PhaseIntersections { get; } 
            = new Dictionary<PhaseBoundary, CurveElementIntersection>();

        public HashSet<IPhase> Phases { get; } = new HashSet<IPhase>();

        public IIntegrationStrategy VolumeIntegration { get; set; }

        public IBoundaryIntegration BoundaryIntegration { get; set; }

        IReadOnlyList<INode> IElement.Nodes => Nodes;

        ISubdomain IElement.Subdomain => Subdomain;

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
