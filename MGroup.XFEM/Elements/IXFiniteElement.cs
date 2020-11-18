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
using MGroup.XFEM.Integration.Quadratures;
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

        IQuadrature IntegrationStandard { get; }

        IIsoparametricInterpolation Interpolation { get; }

        IReadOnlyList<XNode> Nodes { get; }

        //TODO: Use a reference to the discontinuity itself instead of its ID
        Dictionary<int, IElementDiscontinuityInteraction> InteractingDiscontinuities { get; }

        XSubdomain Subdomain { get; set; }

        double CalcBulkSizeCartesian();

        double CalcBulkSizeNatural();

        XPoint EvaluateFunctionsAt(double[] naturalPoint);

        double[] FindCentroidCartesian();

        void IdentifyDofs();

        void IdentifyIntegrationPointsAndMaterials();
    }

    //TODO: These should be converted to default interface implementations
    public static class XFiniteElementExtensions
    {
        public static bool HasEnrichedNodes(this IXFiniteElement element)
        {
            foreach (XNode node in element.Nodes)
            {
                if (node.IsEnriched) return true;
            }
            return false;
        }

        public static double[] FindCentroidNatural(this IXFiniteElement element)
        {
            IReadOnlyList<double[]> nodesNatural = element.Interpolation.NodalNaturalCoordinates;
            return Utilities.FindCentroid(nodesNatural);
        }
    }
}