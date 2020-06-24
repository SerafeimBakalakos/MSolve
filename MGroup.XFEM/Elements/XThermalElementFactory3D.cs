using System.Collections.Generic;
using ISAAR.MSolve.Discretization.Mesh;
using MGroup.XFEM.Entities;
using MGroup.XFEM.Integ.Quadratures;
using MGroup.XFEM.Integration;
using MGroup.XFEM.Interpolation;
using MGroup.XFEM.Interpolation.GaussPointExtrapolation;
using MGroup.XFEM.Materials;

namespace MGroup.XFEM.Elements
{
    public class XThermalElement3DFactory
    {
        private static readonly IReadOnlyDictionary<CellType, IElementGeometry3D> elementGeometries;
        private static readonly IReadOnlyDictionary<CellType, IGaussPointExtrapolation> extrapolations;
        private static readonly IReadOnlyDictionary<CellType, IQuadrature> standardIntegrationsForConductivity;
        //private static readonly IReadOnlyDictionary<CellType, IQuadrature3D> integrationsForMass;
        private static readonly IReadOnlyDictionary<CellType, IIsoparametricInterpolation> interpolations;

        private readonly int integrationBoundaryOrder;
        private readonly IBulkIntegration integrationbulk;
        private readonly IThermalMaterialField material;

        static XThermalElement3DFactory()
        {
            // Mass integrations require as many Gauss points as there are nodes, in order for the consistent mass matrix to be
            // of full rank (and symmetric positive definite)

            // Collections' declarations
            var interpolations = new Dictionary<CellType, IIsoparametricInterpolation>();
            var standardIntegrationsForConductivity = new Dictionary<CellType, IQuadrature>();
            //var integrationsForMass = new Dictionary<CellType, IQuadrature3D>();
            var extrapolations = new Dictionary<CellType, IGaussPointExtrapolation>();
            var elementGeometries = new Dictionary<CellType, IElementGeometry3D>();

            // Hexa8
            interpolations.Add(CellType.Hexa8, InterpolationHexa8.UniqueInstance);
            standardIntegrationsForConductivity.Add(CellType.Hexa8, GaussLegendre3D.GetQuadratureWithOrder(2, 2, 2));
            //integrationsForMass.Add(CellType.Hexa8, GaussLegendre3D.GetQuadratureWithOrder(2, 2, 2));
            extrapolations.Add(CellType.Hexa8, ExtrapolationGaussLegendre2x2x2.UniqueInstance);
            elementGeometries.Add(CellType.Hexa8, new ElementHexa8Geometry());

            // Tet4
            interpolations.Add(CellType.Tet4, InterpolationTet4.UniqueInstance);
            standardIntegrationsForConductivity.Add(CellType.Tet4, TetrahedronQuadrature.Order2Points4);
            //integrationsForMass.Add(CellType.Tet4, TetrahedronQuadrature.Order2Points4);
            extrapolations.Add(CellType.Tet4, null);
            elementGeometries.Add(CellType.Tet4, new ElementTet4Geometry());

            // Static field assignments
            XThermalElement3DFactory.interpolations = interpolations;
            XThermalElement3DFactory.extrapolations = extrapolations;
            XThermalElement3DFactory.standardIntegrationsForConductivity = standardIntegrationsForConductivity;
            //XContinuumElement3DFactory.integrationsForMass = integrationsForMass;
            XThermalElement3DFactory.elementGeometries = elementGeometries;

        }

        public XThermalElement3DFactory(IThermalMaterialField commonMaterial,
            IBulkIntegration bulkIntegration, int integrationBoundaryOrder)
        {
            this.material = commonMaterial;
            this.integrationbulk = bulkIntegration;
            this.integrationBoundaryOrder = integrationBoundaryOrder;
        }

        public XThermalElement3D CreateElement(int id, CellType cellType, IReadOnlyList<XNode> nodes)
        {
#if DEBUG
            interpolations[cellType].CheckElementNodes(nodes);
#endif
            return new XThermalElement3D(id, nodes, elementGeometries[cellType], material, interpolations[cellType],
                extrapolations[cellType], standardIntegrationsForConductivity[cellType], integrationbulk, 
                integrationBoundaryOrder);
        }
    }
}
