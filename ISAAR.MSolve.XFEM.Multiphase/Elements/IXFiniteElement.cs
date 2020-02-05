using System.Collections.Generic;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.Discretization.Mesh;
using ISAAR.MSolve.FEM.Interpolation;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.XFEM.Multiphase.Entities;
using ISAAR.MSolve.XFEM.Multiphase.Geometry;
using ISAAR.MSolve.XFEM.Multiphase.Integration;

namespace ISAAR.MSolve.XFEM.Multiphase.Elements
{
    public interface IXFiniteElement : IElement, IElementType, ICell<XNode>
    {
        IReadOnlyList<(XNode node1, XNode node2)> EdgeNodes { get; }
        IReadOnlyList<(NaturalPoint node1, NaturalPoint node2)> EdgesNodesNatural { get; }

        IBoundaryIntegration IntegrationBoundary { get; }
        IIntegrationStrategy IntegrationVolume { get; }

        IIsoparametricInterpolation2D InterpolationStandard { get; }

        IReadOnlyList<XNode> Nodes { get; }

        Dictionary<PhaseBoundary, CurveElementIntersection> PhaseIntersections { get; }

        HashSet<IPhase> Phases { get; }

        //TODO: Unify 2D and 3D interpolation classes and use that one.

        XSubdomain Subdomain { get; set; }

        void IdentifyDofs();
        void IdentifyIntegrationPointsAndMaterials();
    }
}