using System;
using System.Collections.Generic;
using ISAAR.MSolve.Discretization.Integration;
using ISAAR.MSolve.Discretization.Integration.Quadratures;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.XFEM_OLD.Multiphase.Entities;

//TODO: This is not such a good idea after all, since it is too simple. It would be better to have :
//      1) an interface that defines the intersected segment between a curve and an element. It would be responsible for 
//      generating Gauss points, but it would also be able to handle curved segments, kinks and segments that end inside the 
//      element.
//      2) Identifying the relative position between an element and a curve should probably be done separately from identifying 
//      the intersection (but perhaps they can be called/cached together for performance)
//      3) Corner cases, such as curves tangent to element's edges/nodes should be handled more carefully. A dedicated class 
//      other than the one used for intersections is probably needed.
//      4) Curve tangent to element edge does provide an intersection segment, but it is a) easier to calculate b) shared among 
//      elements. Thus the intersection segments should store which elements they refer to and divide the Gauss point weights 
//      over the multiplicity
//      5) Taking into account both the natural and cartesian system for all data is needed. For efficiency only 1 should be 
//      used at first and the other should be computed only when needed  
namespace ISAAR.MSolve.XFEM_OLD.Multiphase.Geometry
{
    public class CurveElementIntersection
    {
        public CurveElementIntersection(RelativePositionCurveElement relativePosition, NaturalPoint[] intersectionPoints)
        {
            this.RelativePosition = relativePosition;

            if (intersectionPoints.Length > 2)
            {
                throw new NotImplementedException("Intersection points must be 0, 1 or 2, but were " + intersectionPoints.Length);
            }
            this.IntersectionPoints = intersectionPoints;
        }


        public NaturalPoint[] IntersectionPoints { get; }

        public RelativePositionCurveElement RelativePosition { get; }
    }
}
