using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.Geometry.Shapes;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Geometry.Primitives;
using MGroup.XFEM.Geometry.Tolerances;
using MIConvexHull;

//TODO: Allow the option to specify the minimum triangle area.
namespace MGroup.XFEM.Geometry.ConformingMesh
{
    public interface IConformingTriangulator
    {
        IElementSubcell[] FindConformingMesh(IXFiniteElement element,
            IEnumerable<IElementGeometryIntersection> intersections, IMeshTolerance meshTolerance);
    }
}
