﻿using System.Collections.Generic;
using ISAAR.MSolve.Discretization.Integration;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.Discretization.Mesh;
using ISAAR.MSolve.Geometry.Coordinates;
using MGroup.XFEM.Entities;
using MGroup.XFEM.Geometry;
using MGroup.XFEM.Geometry.Primitives;
using MGroup.XFEM.Integration;
using MGroup.XFEM.Materials;

//TODO: LSM/element interactions should probably be stored in a GeometricModel class
namespace MGroup.XFEM.Elements
{
    public interface IXFiniteElement : IElement, IElementType, ICell<XNode>
    {
        ElementEdge[] Edges { get; }

        IBulkIntegration IntegrationBulk { get; }

        IReadOnlyList<XNode> Nodes { get; }

        HashSet<IPhase> Phases { get; }

        Dictionary<PhaseBoundary, IElementGeometryIntersection> PhaseIntersections { get; }

        XSubdomain Subdomain { get; set; }

        XPoint EvaluateFunctionsAt(NaturalPoint point);

        Dictionary<PhaseBoundary, (IReadOnlyList<GaussPoint>, IReadOnlyList<ThermalInterfaceMaterial>)>
            GetMaterialsForBoundaryIntegration();

        (IReadOnlyList<GaussPoint>, IReadOnlyList<ThermalMaterial>) GetMaterialsForBulkIntegration();

        void IdentifyDofs();

        void IdentifyIntegrationPointsAndMaterials();
    }
}