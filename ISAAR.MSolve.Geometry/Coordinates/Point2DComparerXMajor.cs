﻿using System.Collections.Generic;
using ISAAR.MSolve.Geometry.Commons;

namespace ISAAR.MSolve.Geometry.Coordinates
{
    public class Point2DComparerXMajor<TPoint>: IComparer<TPoint> where TPoint : IPoint
    {
        private readonly ValueComparer valueComparer;

        public Point2DComparerXMajor(double tolerance = 1e-6)
        {
            this.valueComparer = new ValueComparer(tolerance);
        }

        public int Compare(TPoint point1, TPoint point2)
        {
            if (valueComparer.AreEqual(point1.X1, point2.X1))
            {
                if (valueComparer.AreEqual(point1.X2, point2.X2)) return 0;
                else if (point1.X2 < point2.X2) return -1;
                else return 1;
            }
            else if (point1.X1 < point2.X1) return -1;
            else return 1;
        }
    }
}
