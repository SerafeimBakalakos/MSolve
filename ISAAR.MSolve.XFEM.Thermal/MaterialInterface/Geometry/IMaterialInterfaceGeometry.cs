using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Integration;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.XFEM.Thermal.Elements;
using ISAAR.MSolve.XFEM.Thermal.Entities;

namespace ISAAR.MSolve.XFEM.Thermal.MaterialInterface.Geometry
{
    public interface IMaterialInterfaceGeometry
    {
        double Thickness { get; } //TODO: Probably delete this

        GaussPoint[] IntegrationPointsAlongInterface(IXFiniteElement element, int numIntegrationPoints);

        bool IsElementIntersected(IXFiniteElement element);

        double SignedDistanceOf(XNode node);

        double SignedDistanceOf(IXFiniteElement element, double[] shapeFunctionsAtNaturalPoint);
    }
}
