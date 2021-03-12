using System;
using System.Collections.Generic;
using System.Text;

namespace MGroup.XFEM.Geometry.HybridFries
{
    public interface ICrackFront3D
    {
        List<Vertex3D> Vertices { get; }

        //TODO: Perhaps this should be stored in the vertices.
        List<CrackFrontSystem3D> CoordinateSystems { get; }
    }
}
