using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;
using MGroup.XFEM.Entities;

namespace MGroup.XFEM
{
    public static class OtherProjExtansions
    {
        public static double[] Coordinates(this INode node)
        {
            if (node is XNode xNode) return xNode.Coordinates; 
            return new double[] { node.X, node.Y, node.Z };
        }
    }
}
