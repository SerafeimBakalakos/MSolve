using System.Collections.Generic;
using System.Diagnostics;
using ISAAR.MSolve.Discretization.Integration;
using ISAAR.MSolve.Discretization.Integration.Quadratures;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.Discretization.Mesh.Generation;
using ISAAR.MSolve.Discretization.Mesh.Generation.Custom;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.XFEM.Multiphase.Elements;
using ISAAR.MSolve.XFEM.Multiphase.Entities;

namespace ISAAR.MSolve.XFEM.Multiphase.Integration
{
    /// <summary>
    /// TODO: This rule is actually independent from the element and its elements can be cached, albeit not in a 
    /// static manner. Should I put it with the standard quadratures?
    /// TODO: Ensure this is not used for anything other than Quadrilaterals.
    /// </summary>
    public class SquareSubcellIntegration2D : IIntegrationStrategy
    {
        private readonly GaussLegendre2D quadratureInSubcells;
        private readonly int subcellsPerAxis;
        private readonly GaussLegendre2D standardQuadrature;

        public SquareSubcellIntegration2D(GaussLegendre2D standardQuadrature) : 
            this(standardQuadrature, 4, GaussLegendre2D.GetQuadratureWithOrder(2,2))
        {
        }

        public SquareSubcellIntegration2D(GaussLegendre2D standardQuadrature, 
            int subcellsPerAxis, GaussLegendre2D quadratureInSubcells)
        {
            this.standardQuadrature = standardQuadrature;
            this.subcellsPerAxis = subcellsPerAxis;
            this.quadratureInSubcells = quadratureInSubcells;
        }

        public IReadOnlyList<GaussPoint> GenerateIntegrationPoints(IXFiniteElement element)
        {
            // Standard elements
            Debug.Assert(element.Phases.Count > 0);
            if (element.Phases.Count == 1) return standardQuadrature.IntegrationPoints;

            // Enriched elements
            var points = new List<GaussPoint>();
            double length = 2.0 / subcellsPerAxis;
            for (int i = 0; i < subcellsPerAxis; ++i)
            {
                for (int j = 0; j < subcellsPerAxis; ++j)
                {
                    // The borders of the subrectangle
                    double xiMin = -1.0 + length * i;
                    double xiMax = -1.0 + length * (i+1);
                    double etaMin = -1.0 + length * j;
                    double etaMax = -1.0 + length * (j + 1);

                    foreach(var subgridPoint in quadratureInSubcells.IntegrationPoints)
                    {
                        // Transformation from the system of the subrectangle to the natural system of the element
                        double naturalXi = subgridPoint.Xi * (xiMax - xiMin) / 2.0 + (xiMin + xiMax) / 2.0;
                        double naturalEta = subgridPoint.Eta * (etaMax - etaMin) / 2.0 + (etaMin + etaMax) / 2.0;
                        double naturalWeight = subgridPoint.Weight * (xiMax - xiMin) / 2.0 * (etaMax - etaMin) / 2.0;
                        points.Add(new GaussPoint(naturalXi, naturalEta, naturalWeight));
                    }
                }
            }
            return points;
        }

        //TODO: Do not use XNode for this mesh
        public (IReadOnlyList<XNode> vertices, IReadOnlyList<CellConnectivity<XNode>> cells) GenerateIntegrationMesh(
            IXFiniteElement element)
        {
            // Standard elements
            if (element.Phases.Count == 1)
            {
                var cell = new CellConnectivity<XNode>(((IElementType)element).CellType, element.Nodes);
                return (element.Nodes, new CellConnectivity<XNode>[] { cell });
            }

            // Enriched elements
            var meshGen = new UniformMeshGenerator2D<XNode>(-1, -1, 1, 1, subcellsPerAxis, subcellsPerAxis);
            return meshGen.CreateMesh((id, x, y, z) =>
            {
                var natural = new NaturalPoint(x, y);
                CartesianPoint cartesian = element.StandardInterpolation.TransformNaturalToCartesian(element.Nodes, natural);
                return new XNode(int.MaxValue, cartesian.X, cartesian.Y);
            });
        }
    }
}
