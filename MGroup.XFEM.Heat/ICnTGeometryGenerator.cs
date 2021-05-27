using System;
using System.Collections.Generic;
using System.Text;
using MGroup.XFEM.Geometry.Primitives;

namespace MGroup.XFEM.Heat
{
    public interface ICntGeometryGenerator
    {
        IEnumerable<ISurface3D> GenerateInclusions();
    }
}
