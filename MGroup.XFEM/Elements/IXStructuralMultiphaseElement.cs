using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using ISAAR.MSolve.Materials.Interfaces;
using MGroup.XFEM.Geometry.Primitives;
using MGroup.XFEM.Materials;
using MGroup.XFEM.Phases;

namespace MGroup.XFEM.Elements
{
    public interface IXStructuralMultiphaseElement : IXMultiphaseElement
    {
        IStructuralMaterialField MaterialField { get; }
    }
}
