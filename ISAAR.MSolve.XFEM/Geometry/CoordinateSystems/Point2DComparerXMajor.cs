﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ISAAR.MSolve.LinearAlgebra.Testing.Utilities;
using ISAAR.MSolve.XFEM.Utilities;

namespace ISAAR.MSolve.XFEM.Geometry.CoordinateSystems
{
    class Point2DComparerXMajor: IComparer<ICartesianPoint2D>
    {
        private readonly ValueComparer valueComparer;

        public Point2DComparerXMajor(double tolerance = 1e-6)
        {
            this.valueComparer = new ValueComparer(tolerance);
        }

        public int Compare(ICartesianPoint2D point1, ICartesianPoint2D point2)
        {
            if (valueComparer.AreEqual(point1.X, point2.X))
            {
                if (valueComparer.AreEqual(point1.Y, point2.Y)) return 0;
                else if (point1.Y < point2.Y) return -1;
                else return 1;
            }
            else if (point1.X < point2.X) return -1;
            else return 1;
        }
    }
}