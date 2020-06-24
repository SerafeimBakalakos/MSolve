using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;

namespace MGroup.XFEM
{
    public static class OtherProjExtansions
    {
        public static double[] Coordinates(this INode node)
        {
            return new double[] { node.X, node.Y, node.Z };
        }
    }
}
