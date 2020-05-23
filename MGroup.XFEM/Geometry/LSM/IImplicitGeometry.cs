using System;
using System.Collections.Generic;
using System.Text;
using MGroup.XFEM.Entities;
using MGroup.XFEM.Geometry.Primitives;

namespace MGroup.XFEM.Geometry.LSM
{
    public interface IImplicitGeometry
    {
        double SignedDistanceOf(XNode node);
        double SignedDistanceOf(XPoint point);

        //TODO: Also normal vector through point/node
    }
}
