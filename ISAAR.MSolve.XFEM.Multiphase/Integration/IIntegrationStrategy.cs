using System.Collections.Generic;
using ISAAR.MSolve.Discretization.Integration;
using ISAAR.MSolve.XFEM_OLD.Multiphase.Elements;

namespace ISAAR.MSolve.XFEM_OLD.Multiphase.Integration
{
    /// <summary>
    /// Algorithms for complex integration rules for specific finite element types. These need the data from each 
    /// finite element to generate integration points for use only by that finite element. 
    /// They typically make use of the standard quadrature rules.
    /// </summary>
    public interface IIntegrationStrategy
    {
        IReadOnlyList<GaussPoint> GenerateIntegrationPoints(IXFiniteElement element);
    }
}
