﻿using System;
using System.Collections.Generic;
using System.Text;
using MGroup.XFEM.Entities;
using MGroup.XFEM.Geometry;

namespace MGroup.XFEM.Elements
{
    public interface IXMultiphaseElement : IXFiniteElement
    {
        HashSet<IPhase> Phases { get; }

        Dictionary<PhaseBoundary, IElementGeometryIntersection> PhaseIntersections { get; }
    }
}