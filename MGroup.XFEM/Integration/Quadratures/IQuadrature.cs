﻿using System;
using System.Collections.Generic;
using System.Text;
using MGroup.XFEM.Integration;

namespace MGroup.XFEM.Integ.Quadratures
{
    /// <summary>
    /// Collection of integration points that are generated by a traditional 3D quadrature rule, independent of the 
	/// element type. All integration points are with respect to a natural (element local) coordinate system.
	/// These integration points are stored as static fields of an enum class, so that accessing them is fast 
	/// and there is only one copy for all elements.
    /// </summary>
    public interface IQuadrature
    {
        /// <summary>
        /// The integrations points are sorted in increasing xi order. This order is strictly defined for each quadrature and 
        /// cannot change.
        /// </summary>
        IReadOnlyList<GaussPoint> IntegrationPoints { get; }
    }
}
