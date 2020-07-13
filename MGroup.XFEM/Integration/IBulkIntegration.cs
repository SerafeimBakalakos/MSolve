﻿using System.Collections.Generic;
using MGroup.XFEM.Integration;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Integration;

namespace MGroup.XFEM.Integration
{
    /// <summary>
    /// Algorithms for complex integration rules for specific finite element types. These need the data from each 
    /// finite element to generate integration points for use only by that finite element. 
    /// They typically make use of the standard quadrature rules.
    /// </summary>
    public interface IBulkIntegration
    {
        IReadOnlyList<GaussPoint> GenerateIntegrationPoints(IXFiniteElement element);
    }
}