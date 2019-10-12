using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Integration;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.XFEM.Elements;
using ISAAR.MSolve.XFEM.Entities;

namespace ISAAR.MSolve.XFEM.MaterialInterface
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
