using System.Collections.Generic;
using ISAAR.MSolve.Discretization.Integration;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.Discretization.Mesh;
using ISAAR.MSolve.FEM.Interpolation;
using ISAAR.MSolve.Geometry.Coordinates;
using MGroup.XFEM.Entities;
using MGroup.XFEM.Geometry;
using MGroup.XFEM.Geometry.ConformingMesh;
using MGroup.XFEM.Integration;

//TODO: LSM/element interactions should probably be stored in a GeometricModel class
namespace MGroup.XFEM.Elements
{
    public interface IXFiniteElement : IElement, IElementType, ICell<XNode>
    {
        IReadOnlyList<ElementEdge> Edges { get; }

        IBulkIntegration IntegrationBulk { get; }

        IReadOnlyList<XNode> Nodes { get; }

        XSubdomain Subdomain { get; set; }

        //(IReadOnlyList<GaussPoint>, IReadOnlyList<ThermalMaterial>) GetMaterialsForBulkIntegration();

        //void IdentifyDofs();
        //void IdentifyIntegrationPointsAndMaterials();
    }
}