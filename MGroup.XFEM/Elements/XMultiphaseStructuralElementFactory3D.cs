﻿using System.Collections.Generic;
using ISAAR.MSolve.Discretization.Mesh;
using MGroup.XFEM.Entities;
using MGroup.XFEM.Integration.Quadratures;
using MGroup.XFEM.Integration;
using MGroup.XFEM.Interpolation;
using MGroup.XFEM.Interpolation.GaussPointExtrapolation;
using MGroup.XFEM.Materials;
using MGroup.XFEM.ElementGeometry;

namespace MGroup.XFEM.Elements
{
    public class XMultiphaseStructuralElementFactory3D : IXElementFactory<IXStructuralMultiphaseElement>
    {
        private static readonly IReadOnlyDictionary<CellType, IElementGeometry> elementGeometries;
        private static readonly IReadOnlyDictionary<CellType, IGaussPointExtrapolation> extrapolations;
        private static readonly IReadOnlyDictionary<CellType, IQuadrature> stdIntegrationsForStiffness;
        //private static readonly IReadOnlyDictionary<CellType, IQuadrature> integrationsForMass;
        private static readonly IReadOnlyDictionary<CellType, IIsoparametricInterpolation> interpolations;

        private readonly int boundaryIntegrationOrder;
        private readonly IBulkIntegration bulkIntegration;
        private readonly bool cohesiveInterfaces;
        private readonly IStructuralMaterialField material;

        static XMultiphaseStructuralElementFactory3D()
        {
            // Mass integrations require as many Gauss points as there are nodes, in order for the consistent mass matrix to be
            // of full rank (and symmetric positive definite)

            // Collections' declarations
            var interpolations = new Dictionary<CellType, IIsoparametricInterpolation>();
            var standardIntegrationsForConductivity = new Dictionary<CellType, IQuadrature>();
            //var integrationsForMass = new Dictionary<CellType, IQuadrature2D>();
            var extrapolations = new Dictionary<CellType, IGaussPointExtrapolation>();
            var elementGeometries = new Dictionary<CellType, IElementGeometry>();

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
            XMultiphaseStructuralElementFactory3D.interpolations = interpolations;
            XMultiphaseStructuralElementFactory3D.extrapolations = extrapolations;
            XMultiphaseStructuralElementFactory3D.stdIntegrationsForStiffness = standardIntegrationsForConductivity;
            //XMultiphaseStructuralElementFactory3D.integrationsForMass = integrationsForMass;
            XMultiphaseStructuralElementFactory3D.elementGeometries = elementGeometries;

        }

        public XMultiphaseStructuralElementFactory3D(IStructuralMaterialField commonMaterial,
            IBulkIntegration bulkIntegration, int boundaryIntegrationOrder, bool cohesiveInterfaces)
        {
            this.material = commonMaterial;
            this.bulkIntegration = bulkIntegration;
            this.boundaryIntegrationOrder = boundaryIntegrationOrder;
            this.cohesiveInterfaces = cohesiveInterfaces;
        }

        public IXStructuralMultiphaseElement CreateElement(int id, CellType cellType, IReadOnlyList<XNode> nodes)
        {
#if DEBUG
            interpolations[cellType].CheckElementNodes(nodes);
#endif
            return new XMultiphaseStructuralElement3D(id, nodes, elementGeometries[cellType], material,
                interpolations[cellType], extrapolations[cellType], stdIntegrationsForStiffness[cellType],
                bulkIntegration, boundaryIntegrationOrder, cohesiveInterfaces);
        }
    }
}
