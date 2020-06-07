using System.Collections.Generic;
using ISAAR.MSolve.Discretization.Integration;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.Discretization.Mesh;
using ISAAR.MSolve.FEM.Interpolation;
using ISAAR.MSolve.Geometry.Coordinates;
using MGroup.XFEM.Entities;
using MGroup.XFEM.Geometry.ConformingMesh;
using MGroup.XFEM.Integration;

namespace MGroup.XFEM.Elements
{
    public interface IXFiniteElement : IElement, IElementType, ICell<XNode>
    {
        /// <summary>
        /// Will be null for elements not intersected by any interfaces
        /// </summary>
        ElementSubtriangle2D[] ConformingSubtriangles2D { get; set; }

        /// <summary>
        /// Will be null for elements not intersected by any interfaces
        /// </summary>
        ElementSubtetrahedron3D[] ConformingSubtetrahedra3D { get; set; }

        IReadOnlyList<(XNode node1, XNode node2)> EdgeNodes { get; }
        IReadOnlyList<(NaturalPoint node1, NaturalPoint node2)> EdgesNodesNatural { get; }

        IReadOnlyList<ElementEdge> Edges { get; }
        IReadOnlyList<ElementFace> Faces { get; }

        //IBoundaryIntegration IntegrationBoundary { get; }
        IBulkIntegration IntegrationBulk { get; }

        //TODO: Unify 2D and 3D interpolation classes and use that one.
        IIsoparametricInterpolation2D Interpolation2D { get; }
        IIsoparametricInterpolation3D Interpolation3D { get; }

        IReadOnlyList<XNode> Nodes { get; }

        //Dictionary<PhaseBoundary, CurveElementIntersection> PhaseIntersections { get; }

        HashSet<IPhase> Phases { get; }

        XSubdomain Subdomain { get; set; }

        //Dictionary<PhaseBoundary, (IReadOnlyList<GaussPoint>, IReadOnlyList<ThermalInterfaceMaterial>)> 
        //    GetMaterialsForBoundaryIntegration();

        //(IReadOnlyList<GaussPoint>, IReadOnlyList<ThermalMaterial>) GetMaterialsForVolumeIntegration();

        //void IdentifyDofs();
        //void IdentifyIntegrationPointsAndMaterials();

        double CalcAreaOrVolume();
    }
}