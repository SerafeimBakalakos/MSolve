using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Commons;
using ISAAR.MSolve.Geometry.Coordinates;

namespace MGroup.XFEM.Geometry
{
    public class Point3DComparer<TPoint> : IComparer<TPoint>
        where TPoint: IPoint
    {
        private readonly ValueComparer comparer;

        public Point3DComparer(double tolerance = 1E-4)
        {
            comparer = new ValueComparer(tolerance);
        }

        //public int Compare(TPoint x, TPoint y)
        //{
        //    if (x.X1 < y.X1) return -1;
        //    else if (x.X1 > y.X1) return +1;
        //    else
        //    {
        //        if (x.X2 < y.X2) return -1;
        //        else if (x.X2 > y.X2) return +1;
        //        else
        //        {
        //            if (x.X3 < y.X3) return -1;
        //            else if (x.X3 > y.X3) return +1;
        //            else return 0;
        //        }
        //    }
        //}

        public int Compare(TPoint x, TPoint y)
        {
            if (comparer.AreEqual(x.X1, y.X1) && comparer.AreEqual(x.X2, y.X2) && comparer.AreEqual(x.X3, y.X3)) return 0;
            else if (x.X1 < y.X1) return -1;
            else if (x.X1 > y.X1) return +1;
            else
            {
                if (x.X2 < y.X2) return -1;
                else if (x.X2 > y.X2) return +1;
                else
                {
                    if (x.X3 < y.X3) return -1;
                    else if (x.X3 > y.X3) return +1;
                    //else return 0;
                    else throw new Exception("Should not have happened");
                }
            }
        }
    }
}
