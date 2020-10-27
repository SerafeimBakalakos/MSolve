using System.Collections.Generic;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.Discretization.Mesh;
using ISAAR.MSolve.Geometry.Coordinates;
using MGroup.XFEM.Enrichment;
using MGroup.XFEM.Entities;
using MGroup.XFEM.Geometry;
using MGroup.XFEM.Geometry.ConformingMesh;
using MGroup.XFEM.Geometry.Primitives;
using MGroup.XFEM.Integration;
using MGroup.XFEM.Interpolation;
using MGroup.XFEM.Materials;

//TODO: LSM/element interactions should probably be stored in a GeometricModel class
namespace MGroup.XFEM.Elements
{
    public interface IXFiniteElement : IElement, IElementType, ICell<XNode>
    {
        /// <summary>
        /// Will be null for elements not intersected by any interfaces
        /// </summary>
        IElementSubcell[] ConformingSubcells { get; set; }

        ElementEdge[] Edges { get; }

        ElementFace[] Faces { get; }

        IBulkIntegration IntegrationBulk { get; }

        IIsoparametricInterpolation Interpolation { get; }

        IReadOnlyList<XNode> Nodes { get; }

        List<IElementGeometryIntersection> Intersections { get; }

        XSubdomain Subdomain { get; set; }

        double CalcBulkSizeCartesian();

        double CalcBulkSizeNatural();

        XPoint EvaluateFunctionsAt(double[] naturalPoint);

        double[] FindCentroidCartesian();

        void IdentifyDofs(Dictionary<IEnrichment, IDofType[]> enrichedDofs);

        void IdentifyIntegrationPointsAndMaterials();
    }
}