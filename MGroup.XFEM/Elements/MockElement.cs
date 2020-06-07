using System;
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
using MGroup.XFEM.Geometry.ConformingMesh;
using MGroup.XFEM.Geometry.Primitives;
using MGroup.XFEM.Integration;

//TODO: delete this class
namespace MGroup.XFEM.Elements
{
    public class MockElement : IXFiniteElement
    {
        private readonly ElementEdge[] edges;
        private readonly ElementFace[] faces;

        public MockElement(int id, CellType cellType, IReadOnlyList<XNode> nodes)
        {
            this.ID = id;
            this.CellType = cellType;
            this.Nodes = nodes;

            if (this.CellType == CellType.Quad4)
            {
            }
            if (this.CellType == CellType.Hexa8)
            {
                IReadOnlyList<NaturalPoint> nodesNatural = InterpolationHexa8.UniqueInstance.NodalNaturalCoordinates;
                edges = new ElementEdge[12];
                edges[0] = new ElementEdge(Nodes, nodesNatural, 0, 1);
                edges[1] = new ElementEdge(Nodes, nodesNatural, 1, 2);
                edges[2] = new ElementEdge(Nodes, nodesNatural, 2, 3);
                edges[3] = new ElementEdge(Nodes, nodesNatural, 3, 0);
                edges[4] = new ElementEdge(Nodes, nodesNatural, 4, 5);
                edges[5] = new ElementEdge(Nodes, nodesNatural, 5, 6);
                edges[6] = new ElementEdge(Nodes, nodesNatural, 6, 7);
                edges[7] = new ElementEdge(Nodes, nodesNatural, 7, 4);
                edges[8] = new ElementEdge(Nodes, nodesNatural, 0, 4);
                edges[9] = new ElementEdge(Nodes, nodesNatural, 1, 5);
                edges[10] = new ElementEdge(Nodes, nodesNatural, 2, 6);
                edges[11] = new ElementEdge(Nodes, nodesNatural, 3, 7);

                faces = new ElementFace[6];
                faces[0] = new ElementFace();
                faces[0].Nodes = new XNode[]
                {
                    Nodes[0], Nodes[1], Nodes[2], Nodes[3]
                };
                faces[0].NodesNatural = new NaturalPoint[]
                {
                    nodesNatural[0], nodesNatural[1], nodesNatural[2], nodesNatural[3]
                };
                faces[0].Edges = new ElementEdge[] { edges[0], edges[1], edges[2], edges[3] };

                faces[1] = new ElementFace();
                faces[1].Nodes = new XNode[]
                {
                    Nodes[7], Nodes[6], Nodes[5], Nodes[4]
                };
                faces[1].NodesNatural = new NaturalPoint[]
                {
                    nodesNatural[7], nodesNatural[6], nodesNatural[5], nodesNatural[4]
                };
                faces[1].Edges = new ElementEdge[] { edges[4], edges[5], edges[6], edges[7] };

                faces[2] = new ElementFace();
                faces[2].Nodes = new XNode[]
                {
                    Nodes[1], Nodes[0], Nodes[4], Nodes[5]
                };
                faces[2].NodesNatural = new NaturalPoint[]
                {
                    nodesNatural[1], nodesNatural[0], nodesNatural[4], nodesNatural[5]
                };
                faces[2].Edges = new ElementEdge[] { edges[0], edges[8], edges[4], edges[9] };

                faces[3] = new ElementFace();
                faces[3].Nodes = new XNode[]
                {
                    Nodes[3], Nodes[2], Nodes[6], Nodes[7]
                };
                faces[3].NodesNatural = new NaturalPoint[]
                {
                    nodesNatural[3], nodesNatural[2], nodesNatural[6], nodesNatural[7]
                };
                faces[3].Edges = new ElementEdge[] { edges[2], edges[10], edges[6], edges[11] };


                faces[4] = new ElementFace();
                faces[4].Nodes = new XNode[]
                {
                    Nodes[0], Nodes[3], Nodes[7], Nodes[4]
                };
                faces[4].NodesNatural = new NaturalPoint[]
                {
                    nodesNatural[0], nodesNatural[3], nodesNatural[7], nodesNatural[4]
                };
                faces[4].Edges = new ElementEdge[] { edges[3], edges[11], edges[7], edges[8] };

                faces[5] = new ElementFace();
                faces[5].Nodes = new XNode[]
                {
                    Nodes[2], Nodes[1], Nodes[5], Nodes[6]
                };
                faces[5].NodesNatural = new NaturalPoint[]
                {
                    nodesNatural[2], nodesNatural[1], nodesNatural[5], nodesNatural[6]
                };
                faces[5].Edges = new ElementEdge[] { edges[1], edges[9], edges[5], edges[10] };
            }
            
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

        public IIsoparametricInterpolation2D Interpolation2D
        {
            get
            {
                if (CellType == CellType.Quad4) return InterpolationQuad4.UniqueInstance;
                else throw new NotImplementedException();
            }
        }

        public IIsoparametricInterpolation3D Interpolation3D
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

        public XSubdomain Subdomain { get; set; }
        ISubdomain IElement.Subdomain => Subdomain;

        public IReadOnlyList<ElementEdge> Edges => edges;

        public IReadOnlyList<ElementFace> Faces => faces;

        public IBulkIntegration IntegrationBulk { get; set; }
        public ElementSubtriangle2D[] ConformingSubtriangles2D { get; set; }

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

        public double CalcAreaOrVolume()
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
            else if (this.CellType == CellType.Tet4)
            {
                var tetra = new Tetrahedron3D();
                tetra.Vertices[0] = Nodes[0].Coordinates;
                tetra.Vertices[1] = Nodes[1].Coordinates;
                tetra.Vertices[2] = Nodes[2].Coordinates;
                tetra.Vertices[3] = Nodes[3].Coordinates;
                return tetra.CalcVolume();
            }
            else if (this.CellType == CellType.Hexa8 || this.CellType == CellType.Tet4)
            {
                //TODO: Split it into tetrahedra and use the closed formula for their volume

                double volume = 0.0;
                GaussLegendre3D quadrature = GaussLegendre3D.GetQuadratureWithOrder(2, 2, 2);
                IReadOnlyList<Matrix> shapeGradientsNatural =
                    Interpolation3D.EvaluateNaturalGradientsAtGaussPoints(quadrature);
                for (int gp = 0; gp < quadrature.IntegrationPoints.Count; ++gp)
                {
                    var jacobian = new IsoparametricJacobian3D(Nodes, shapeGradientsNatural[gp]);
                    volume += jacobian.DirectDeterminant * quadrature.IntegrationPoints[gp].Weight;
                }
                return volume;
            }
            else throw new NotImplementedException();
        }
    }
}
