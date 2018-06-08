using System;
using System.Collections.Generic;
using System.Text;

namespace ISAAR.MSolve.Logging.VTK
{
    public class VtkPoint2D
    {
        public VtkPoint2D(int id, double x, double y)
        {
            this.ID = id;
            this.X = x;
            this.Y = y;
        }

        public int ID { get; }
        public double X { get; }
        public double Y { get; }
    }
}
