using System;
using System.Collections.Generic;
using System.Text;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Entities;
using MGroup.XFEM.Geometry.Primitives;

//TODO: inherit from IXGeometryDescription
namespace MGroup.XFEM.Geometry.LSM
{
    public interface IClosedGeometry /*: IXGeometryDescription*/
    {
        int ID { get; }

        IElementGeometryIntersection Intersect(IXFiniteElement element);

        double SignedDistanceOf(XNode node);

        double SignedDistanceOf(XPoint point);


        void UnionWith(IClosedGeometry otherGeometry);
    }
}
